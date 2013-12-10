// Copyright (c) 2007-2013 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq.Expressions;
using System.Text;
using Shaolinq.Persistence.Sql.Linq.Expressions;
using Shaolinq.Persistence.Sql.Linq.Optimizer;
using Platform;
using Shaolinq.TypeBuilding;

namespace Shaolinq.Persistence.Sql.Linq
{
	public class Sql92QueryFormatter
		: SqlQueryFormatter
	{
		public struct FunctionResolveResult
		{
			public static Pair<Type, object>[] MakeArguments(params object[] args)
			{
				var retval = new Pair<Type, object>[args.Length];

				for (var i = 0; i < args.Length; i++)
				{
					retval[i] = new Pair<Type, object>(args[i].GetType(), args[i]);
				}

				return retval;
			}

			public string functionName;
			public bool treatAsOperator;
			public string functionPrefix;
			public string functionSuffix;
			public Pair<Type, object>[] argsAfter;
			public Pair<Type, object>[] argsBefore;
			public ReadOnlyCollection<Expression> arguments;

			public FunctionResolveResult(string functionName, bool treatAsOperator, ReadOnlyCollection<Expression> arguments)
				: this(functionName, treatAsOperator, null, null, arguments)
			{
			}

			public FunctionResolveResult(string functionName, bool treatAsOperator, Pair<Type, object>[] argsBefore, Pair<Type, object>[] argsAfter, ReadOnlyCollection<Expression> arguments)
			{
				this.functionPrefix = null;
				this.functionSuffix = null;
				this.functionName = functionName;
				this.treatAsOperator = treatAsOperator;
				this.argsBefore = argsBefore;
				this.argsAfter = argsAfter;
				this.arguments = arguments;
			}
		}

		protected enum Indentation
		{
			Same,
			Inner,
			Outer
		}

		public Expression Expression { get; private set; }

		protected virtual char ParameterIndicatorChar
		{
			get
			{
				return '@';
			}
		}

		private int depth;
		protected StringBuilder commandText;
		protected List<Pair<Type, object>> parameterValues;
		private readonly SqlQueryFormatterOptions options;
		protected readonly SqlDataTypeProvider sqlDataTypeProvider;
		protected readonly SqlDialect sqlDialect;
		internal int IndentationWidth { get; private set; }

		public class IndentationContext
			: IDisposable
		{
			private readonly Sql92QueryFormatter parent;

			public IndentationContext(Sql92QueryFormatter parent)
			{
				this.parent = parent;
				this.parent.depth++;
				this.parent.WriteLine();
			}

			public void Dispose()
			{
				this.parent.depth--;
			}
		}

		public IndentationContext AcquireIndentationContext()
		{
			return new IndentationContext(this);
		}

		public virtual void WriteLine()
		{
			this.commandText.AppendLine();
			this.commandText.Append(' ', depth * this.IndentationWidth);
		}

		public virtual void WriteLine(object line)
		{
			this.commandText.Append(line);
			this.commandText.AppendLine();
			this.commandText.Append(' ', depth * this.IndentationWidth);
		}
		
		public virtual void Write(object value)
		{
			this.commandText.Append(value);
		}

		public virtual void WriteFormat(string format, params object[] args)
		{
			this.commandText.AppendFormat(format, args);
		}

		public Sql92QueryFormatter(Expression expression)
			: this(expression, SqlQueryFormatterOptions.Default, null, null)
		{
		}

		public Sql92QueryFormatter(Expression expression, SqlQueryFormatterOptions options)
			: this(expression, options, null, null)
		{
		}

		public Sql92QueryFormatter(Expression expression, SqlQueryFormatterOptions options, SqlDataTypeProvider sqlDataTypeProvider, SqlDialect sqlDialect)
		{
			this.options = options;

			if (sqlDataTypeProvider == null)
			{
				this.sqlDataTypeProvider = DefaultSqlDataTypeProvider.Instance;
			}
			else
			{
				this.sqlDataTypeProvider = sqlDataTypeProvider;
			}

			if (sqlDialect == null)
			{
				this.sqlDialect = SqlDialect.Default;
			}
			else
			{
				this.sqlDialect = sqlDialect;
			}

			this.IndentationWidth = 2;
			this.Expression = expression;
		}

		public override SqlQueryFormatResult Format()
		{
			if (this.commandText == null)
			{
				commandText = new StringBuilder(512);
				parameterValues = new List<Pair<Type, object>>();

				Visit(this.Expression);
			}

			return new SqlQueryFormatResult(commandText.ToString(), parameterValues);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			return Visit(projection.Select);
		}

		protected override Expression VisitMethodCall(MethodCallExpression methodCallExpression)
		{
			if (methodCallExpression.Method == MethodInfoFastRef.ObjectToStringMethod)
			{
				if (methodCallExpression.Object.Type.IsEnum)
				{
					Visit(methodCallExpression.Object);

					return methodCallExpression;
				}
				else
				{
					Visit(methodCallExpression.Object);

					return methodCallExpression;
				}
			}
			else if (methodCallExpression.Method.DeclaringType.IsGenericType
			         && methodCallExpression.Method.DeclaringType.GetGenericTypeDefinition() == typeof(Nullable<>)
			         && methodCallExpression.Method.Name == "GetValueOrDefault")
			{
				Visit(methodCallExpression.Object);

				return methodCallExpression;
			}

			throw new NotSupportedException(String.Format("The method '{0}' is not supported", methodCallExpression.Method.Name));
		}

		private static bool IsLikeCallExpression(Expression expression)
		{
			var methodCallExpression = expression as MethodCallExpression;

			if (methodCallExpression == null)
			{
				return false;
			}

			return methodCallExpression.Method.DeclaringType == typeof(ShaolinqStringExtensions)
			       && methodCallExpression.Method.Name == "IsLike";
		}

		private static bool IsNumeric(Type type)
		{
			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Byte:
				case TypeCode.Char:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Decimal:
					return true;
			}

			return false;
		}

		protected override Expression VisitUnary(UnaryExpression unaryExpression)
		{
			switch (unaryExpression.NodeType)
			{
				case ExpressionType.Convert:

					var unaryType = Nullable.GetUnderlyingType(unaryExpression.Type) ?? unaryExpression.Type;
					var operandType = Nullable.GetUnderlyingType(unaryExpression.Operand.Type) ?? unaryExpression.Operand.Type;

					if (unaryType == operandType
					    || (IsNumeric(unaryType) && IsNumeric(operandType))
					    || unaryExpression.Operand.Type.IsDataAccessObjectType())
					{
						Visit(unaryExpression.Operand);
					}
					else
					{
						throw new NotSupportedException(String.Format("The unary operator '{0}' is not supported", unaryExpression.NodeType));
					}
					break;
				case ExpressionType.Not:
					this.Write("NOT (");
					Visit(unaryExpression.Operand);
					this.Write(")");
					break;
				default:
					throw new NotSupportedException(String.Format("The unary operator '{0}' is not supported", unaryExpression.NodeType));
			}

			return unaryExpression;
		}

		protected virtual FunctionResolveResult ResolveSqlFunction(SqlFunction function, ReadOnlyCollection<Expression> arguments)
		{
			switch (function)
			{
				case SqlFunction.IsNull:
					return new FunctionResolveResult("", true, arguments)
					{
						functionSuffix = "IS NULL"
					};
				case SqlFunction.IsNotNull:
					return new FunctionResolveResult("", true, arguments)
					{
						functionSuffix = "IS NOT NULL"
					};
				case SqlFunction.In:
					return new FunctionResolveResult("IN", true, arguments);
				case SqlFunction.Like:
					return new FunctionResolveResult(this.sqlDialect.LikeString, true, arguments);
				case SqlFunction.CompareObject:
					var expressionType = (ExpressionType)((ConstantExpression)arguments[0]).Value;
					var args = new Expression[2];

					args[0] = arguments[1];
					args[1] = arguments[2];

					switch (expressionType)
					{
						case ExpressionType.LessThan:
							return new FunctionResolveResult("<", true, new ReadOnlyCollection<Expression>(args));
						case ExpressionType.LessThanOrEqual:
							return new FunctionResolveResult("<=", true, new ReadOnlyCollection<Expression>(args));
						case ExpressionType.GreaterThan:
							return new FunctionResolveResult(">", true, new ReadOnlyCollection<Expression>(args));
						case ExpressionType.GreaterThanOrEqual:
							return new FunctionResolveResult(">=", true, new ReadOnlyCollection<Expression>(args));
					}
					throw new InvalidOperationException();
				case SqlFunction.NotLike:
					return new FunctionResolveResult("NOT " + this.sqlDialect.LikeString, true, arguments);
				case SqlFunction.ServerDateTime:
					return new FunctionResolveResult("NOW", false, arguments);
				case SqlFunction.StartsWith:
				{
					Expression newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, arguments[1], Expression.Constant("%"));
					newArgument = RedundantFunctionCallRemover.Remove(newArgument);

					var list = new List<Expression>
					{
						arguments[0],
						newArgument
					};

					return new FunctionResolveResult(this.sqlDialect.LikeString, true, new ReadOnlyCollection<Expression>(list));
				}
				case SqlFunction.ContainsString:
				{
					Expression newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, arguments[1], Expression.Constant("%"));
					newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, Expression.Constant("%"), newArgument);
					newArgument = RedundantFunctionCallRemover.Remove(newArgument);

					var list = new List<Expression>
					{
						arguments[0],
						newArgument
					};

					return new FunctionResolveResult(this.sqlDialect.LikeString, true, new ReadOnlyCollection<Expression>(list));
				}
				case SqlFunction.EndsWith:
				{
					Expression newArgument = new SqlFunctionCallExpression(typeof(string), SqlFunction.Concat, Expression.Constant("%"), arguments[1]);
					newArgument = RedundantFunctionCallRemover.Remove(newArgument);

					var list = new List<Expression>
					{
						arguments[0],
						newArgument
					};

					return new FunctionResolveResult(this.sqlDialect.LikeString, true, new ReadOnlyCollection<Expression>(list));
				}
				default:
					return new FunctionResolveResult(function.ToString().ToUpper(), false, arguments);
			}
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			var result = ResolveSqlFunction(functionCallExpression.Function, functionCallExpression.Arguments);

			if (result.treatAsOperator)
			{
				this.Write("(");

				if (result.functionPrefix != null)
				{
					this.Write(result.functionPrefix);
					this.Write(' ');
				}
				
				for (int i = 0, n = result.arguments.Count - 1; i <= n; i++)
				{
					var requiresGrouping = result.arguments[i] is SqlSelectExpression;

					if (requiresGrouping)
					{
						this.Write("(");
					}

					Visit(result.arguments[i]);

					if (requiresGrouping)
					{
						this.Write(")");
					}

					if (i != n)
					{
						this.Write(' ');
						this.Write(result.functionName);
						this.Write(' ');
					}
				}

				if (result.functionSuffix != null)
				{
					this.Write(' ');
					this.Write(result.functionSuffix);
				}

				this.Write(")");
			}
			else
			{
				this.Write(result.functionName);
				this.Write("(");

				if (result.functionPrefix != null)
				{
					this.Write(result.functionPrefix);
					this.Write(' ');
				}

				if (result.argsBefore != null && result.argsBefore.Length > 0)
				{
					for (int i = 0, n = result.argsBefore.Length - 1; i <= n; i++)
					{
						this.Write(this.ParameterIndicatorChar);
						this.Write("param");
						this.Write(parameterValues.Count);
						parameterValues.Add(new Pair<Type, object>(result.argsBefore[i].Left, result.argsBefore[i].Right));

						if (i != n || (functionCallExpression.Arguments.Count > 0))
						{
							this.Write(", ");
						}
					}
				}

				for (int i = 0, n = result.arguments.Count - 1; i <= n; i++)
				{
					Visit(result.arguments[i]);

					if (i != n || (result.argsAfter != null && result.argsAfter.Length > 0))
					{
						this.Write(", ");
					}
				}

				if (result.argsAfter != null && result.argsAfter.Length > 0)
				{
					for (int i = 0, n = result.argsAfter.Length - 1; i <= n; i++)
					{
						Write(this.ParameterIndicatorChar);
						Write("param");
						Write(parameterValues.Count);
						parameterValues.Add(new Pair<Type, object>(result.argsAfter[i].Left, result.argsAfter[i].Right));

						if (i != n)
						{
							this.Write(", ");
						}
					}
				}

				if (result.functionSuffix != null)
				{
					this.Write(' ');
					this.Write(result.functionSuffix);
				}

				this.Write(")");
			}

			return functionCallExpression;
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			Write("(");

			Visit(binaryExpression.Left);

			switch (binaryExpression.NodeType)
			{
				case ExpressionType.And:
				case ExpressionType.AndAlso:
					Write(" AND ");
					break;
				case ExpressionType.Or:
				case ExpressionType.OrElse:
					Write(" OR ");
					break;
				case ExpressionType.Equal:
					Write(" = ");
					break;
				case ExpressionType.NotEqual:
					Write(" <> ");
					break;
				case ExpressionType.LessThan:
					Write(" < ");
					break;
				case ExpressionType.LessThanOrEqual:
					Write(" <= ");
					break;
				case ExpressionType.GreaterThan:
					Write(" > ");
					break;
				case ExpressionType.GreaterThanOrEqual:
					Write(" >= ");
					break;
				case ExpressionType.Add:
					Write(" + ");
					break;
				case ExpressionType.Subtract:
					Write(" - ");
					break;
				case ExpressionType.Multiply:
					Write(" * ");
					break;
				default:
					throw new NotSupportedException(String.Format("The binary operator '{0}' is not supported", binaryExpression.NodeType));
			}

			Visit(binaryExpression.Right);

			Write(")");

			return binaryExpression;
		}

		protected virtual void VisitCollection(IEnumerable collection)
		{
			var i = 0;

			this.Write("(");

			foreach (var obj in collection)
			{
				VisitConstant(Expression.Constant(obj));

				this.Write(", ");
				i++;
			}

			if (i > 0)
			{
				commandText.Length -= 2;
			}

			this.Write(")");
		}

		protected override Expression VisitConstantPlaceholder(SqlConstantPlaceholderExpression constantPlaceholderExpression)
		{
			if (this.options.EvaluateConstantPlaceholders)
			{
				return base.VisitConstantPlaceholder(constantPlaceholderExpression);
			}
			else
			{
				this.WriteFormat("$${0}", constantPlaceholderExpression.Index);

				return constantPlaceholderExpression;
			}
		}

		protected override Expression VisitConstant(ConstantExpression constantExpression)
		{
			if (constantExpression.Value == null)
			{
				this.Write("NULL");
			}
			else
			{
				var type = constantExpression.Value.GetType();

				switch (Type.GetTypeCode(type))
				{
					case TypeCode.Boolean:
						this.Write(this.ParameterIndicatorChar);
						this.Write("param");
						this.Write(parameterValues.Count);
						parameterValues.Add(new Pair<Type, object>(typeof(bool), Convert.ToBoolean(constantExpression.Value)));
						break;
					case TypeCode.Object:
						if (type.IsArray || typeof(IEnumerable).IsAssignableFrom(type))
						{
							VisitCollection((IEnumerable)constantExpression.Value);
						}
						else if (type == typeof(Guid))
						{
							this.Write(this.ParameterIndicatorChar);
							this.Write("param");
							this.Write(parameterValues.Count);

							var value = constantExpression.Value as Guid?;

							if (this.sqlDataTypeProvider != null)
							{
								parameterValues.Add(this.sqlDataTypeProvider.GetSqlDataType(constantExpression.Type).ConvertForSql(value));
							}
							else
							{
								parameterValues.Add(new Pair<Type, object>(constantExpression.Type, value));
							}
						}
						else
						{
							this.Write("obj: " + constantExpression.Value);
						}
						break;
					default:
						if (constantExpression.Type.IsEnum)
						{
							this.Write(this.ParameterIndicatorChar);
							this.Write("param");
							this.Write(parameterValues.Count);

							parameterValues.Add(new Pair<Type, object>(typeof(string), Enum.GetName(constantExpression.Type, constantExpression.Value)));
						}
						else
						{
							this.Write(this.ParameterIndicatorChar);
							this.Write("param");
							this.Write(parameterValues.Count);

							parameterValues.Add(new Pair<Type, object>(constantExpression.Type, constantExpression.Value));
						}
						break;
				}
			}

			return constantExpression;
		}

		private static string GetAggregateName(SqlAggregateType aggregateType)
		{
			switch (aggregateType)
			{
				case SqlAggregateType.Count:
					return "COUNT";
				case SqlAggregateType.Min:
					return "MIN";
				case SqlAggregateType.Max:
					return "MAX";
				case SqlAggregateType.Sum:
					return "SUM";
				case SqlAggregateType.Average:
					return "AVG";
				default:
					throw new NotSupportedException(String.Concat("Unknown aggregate type: ", aggregateType));
			}
		}

		protected virtual bool RequiresAsteriskWhenNoArgument(SqlAggregateType aggregateType)
		{
			return aggregateType == SqlAggregateType.Count;
		}

		protected override Expression VisitAggregate(SqlAggregateExpression sqlAggregate)
		{
			this.Write(GetAggregateName(sqlAggregate.AggregateType));

			this.Write("(");

			if (sqlAggregate.IsDistinct)
			{
				Write("DISTINCT ");
			}

			if (sqlAggregate.Argument != null)
			{
				this.Visit(sqlAggregate.Argument);
			}
			else if (RequiresAsteriskWhenNoArgument(sqlAggregate.AggregateType))
			{
				this.Write("*");
			}

			this.Write(")");

			return sqlAggregate;
		}

		protected void Indent(Indentation style)
		{
			if (style == Indentation.Inner)
			{
				this.depth++;
			}
			else if (style == Indentation.Outer)
			{
				this.depth--;
			}
		}

		protected override Expression VisitSubquery(SqlSubqueryExpression subquery)
		{
			this.Write("(");

			using (AcquireIndentationContext())
			{
				this.Visit(subquery.Select);
				this.WriteLine();
			}

			this.Write(")");

			return subquery;
		}

		protected override Expression VisitColumn(SqlColumnExpression columnExpression)
		{
			if (!String.IsNullOrEmpty(columnExpression.SelectAlias))
			{
				if (ignoreAlias == columnExpression.SelectAlias)
				{
					this.Write(this.sqlDialect.NameQuoteChar);
					this.Write(replaceAlias);
					this.Write(this.sqlDialect.NameQuoteChar);
				}
				else
				{
					this.Write(this.sqlDialect.NameQuoteChar);
					this.Write(columnExpression.SelectAlias);
					this.Write(this.sqlDialect.NameQuoteChar);
				}

				this.Write(".");
			}

			this.Write(this.sqlDialect.NameQuoteChar);
			this.Write(columnExpression.Name);
			this.Write(this.sqlDialect.NameQuoteChar);

			return columnExpression;
		}

		protected virtual void VisitColumn(SqlSelectExpression selectExpression, SqlColumnDeclaration column)
		{
			var c = Visit(column.Expression) as SqlColumnExpression;

			if ((c == null || c.Name != column.Name) && !String.IsNullOrEmpty(column.Name))
			{
				this.Write(" AS ");
				this.Write(this.sqlDialect.NameQuoteChar);
				this.Write(column.Name);
				this.Write(this.sqlDialect.NameQuoteChar);
			}
		}

		protected override Expression VisitConditional(ConditionalExpression expression)
		{
			this.Write("CASE WHEN (");
			this.Visit(expression.Test);
			this.Write(")");
			this.Write(" THEN (");
			this.Visit(expression.IfTrue);
			this.Write(") ELSE (");
			this.Visit(expression.IfFalse);
			this.Write(") END");

			return expression;
		}

		private int selectNest;

		protected override Expression VisitSelect(SqlSelectExpression selectExpression)
		{
			var selectNested = selectNest > 0;

			if (selectNested)
			{
				this.Write("(");
			}

			try
			{
				selectNest++;

				this.Write("SELECT ");

				if (selectExpression.Distinct)
				{
					this.Write("DISTINCT ");
				}

				if (selectExpression.Columns.Count == 0)
				{
					this.Write("* ");
				}

				for (int i = 0, n = selectExpression.Columns.Count; i < n; i++)
				{
					var column = selectExpression.Columns[i];

					if (i > 0)
					{
						this.Write(", ");
					}

					VisitColumn(selectExpression, column);
				}

				if (selectExpression.From != null)
				{
					this.WriteLine();
					this.Write("FROM ");
					VisitSource(selectExpression.From);
				}

				if (selectExpression.Where != null)
				{
					this.WriteLine();
					this.Write("WHERE ");
					Visit(selectExpression.Where);
				}

				if (selectExpression.OrderBy != null && selectExpression.OrderBy.Count > 0)
				{
					this.WriteLine();
					this.Write("ORDER BY ");

					for (int i = 0; i < selectExpression.OrderBy.Count; i++)
					{
						var orderExpression = selectExpression.OrderBy[i];

						if (i > 0)
						{
							this.Write(", ");
						}

						this.Visit(orderExpression.Expression);

						if (orderExpression.OrderType == OrderType.Descending)
						{
							this.Write(" DESC");
						}
					}
				}

				if (selectExpression.GroupBy != null && selectExpression.GroupBy.Count > 0)
				{
					this.WriteLine();
					this.Write("GROUP BY ");

					for (var i = 0; i < selectExpression.GroupBy.Count; i++)
					{
						if (i > 0)
						{
							this.Write(", ");
						}

						this.Visit(selectExpression.GroupBy[i]);
					}
				}

				AppendLimit(selectExpression);

				if (selectExpression.ForUpdate && this.sqlDialect.SupportsFeature(SqlFeature.Constraints))
				{
					this.Write(" FOR UPDATE");
				}

				if (selectNested)
				{
					this.Write(")");
				}
			}
			finally
			{
				selectNest--;
			}

			return selectExpression;
		}

		protected virtual void AppendLimit(SqlSelectExpression selectExpression)
		{
			if (selectExpression.Skip != null || selectExpression.Take != null)
			{
				this.Write(" LIMIT ");

				if (selectExpression.Skip == null)
				{
					this.Write("0");
				}
				else
				{
					Visit(selectExpression.Skip);
				}

				if (selectExpression.Take != null)
				{
					this.Write(", ");

					Visit(selectExpression.Take);
				}
				else if (selectExpression.Skip != null)
				{
					this.Write(", ");
					this.Write(Int64.MaxValue);
				}
			}
		}

		protected override Expression VisitJoin(SqlJoinExpression join)
		{
			this.VisitSource(join.Left);

			this.WriteLine();

			switch (join.JoinType)
			{
				case SqlJoinType.CrossJoin:
					this.Write(" CROSS JOIN ");
					break;
				case SqlJoinType.InnerJoin:
					this.Write(" INNER JOIN ");
					break;
				case SqlJoinType.LeftJoin:
					this.Write(" LEFT JOIN ");
					break;
				case SqlJoinType.RightJoin:
					this.Write(" RIGHT JOIN ");
					break;
				case SqlJoinType.OuterJoin:
					this.Write(" FULL OUTER JOIN ");
					break;
			}

			this.VisitSource(join.Right);

			if (join.Condition != null)
			{
				using (AcquireIndentationContext())
				{
					this.Write("ON ");

					this.Visit(join.Condition);
				}
			}

			return join;
		}

		protected override Expression VisitSource(Expression source)
		{
			switch ((SqlExpressionType)source.NodeType)
			{
				case SqlExpressionType.Table:
					var table = (SqlTableExpression)source;

					this.Write(this.sqlDialect.NameQuoteChar);
					this.Write(table.Name);
					this.Write(this.sqlDialect.NameQuoteChar);
					this.Write(" AS ");
					this.Write(this.sqlDialect.NameQuoteChar);
					this.Write(table.Alias);
					this.Write(this.sqlDialect.NameQuoteChar);

					break;
				case SqlExpressionType.Select:
					var select = (SqlSelectExpression)source;
					this.WriteLine();
					this.Write("(");

					using (AcquireIndentationContext())
					{
						Visit(select);
						this.WriteLine();
					}
					
					this.Write(")");
					this.Write(" AS ");
					this.Write(this.sqlDialect.NameQuoteChar);
					this.Write(select.Alias);
					this.Write(this.sqlDialect.NameQuoteChar);

					break;
				case SqlExpressionType.Join:
					this.VisitJoin((SqlJoinExpression)source);
					break;
				default:
					throw new InvalidOperationException(String.Format("Select source ({0}) is not valid type", source.NodeType));
			}

			return source;
		}

		protected string ignoreAlias;
		protected string replaceAlias;

		protected override Expression VisitDelete(SqlDeleteExpression deleteExpression)
		{
			this.Write("DELETE ");
			this.Write("FROM ");
			this.Write(this.sqlDialect.NameQuoteChar);
			this.Write(deleteExpression.TableName);
			this.Write(this.sqlDialect.NameQuoteChar);
			this.WriteLine();
			this.Write(" WHERE ");
			this.WriteLine();

			ignoreAlias = deleteExpression.Alias;
			replaceAlias = deleteExpression.TableName;

			Visit(deleteExpression.Where);

			ignoreAlias = "";

			return deleteExpression;
		}

		protected override Expression VisitMemberAccess(MemberExpression memberExpression)
		{
			this.Visit(memberExpression.Expression);
			this.Write(".");
			this.Write("Prop(");
			this.Write(memberExpression.Member.Name);
			this.Write(")");

			return memberExpression;
		}

		protected override Expression VisitObjectOperand(SqlObjectOperand objectOperand)
		{
			this.Write("Obj(");
			this.Write(objectOperand.Type.Name);
			this.Write(")");

			return objectOperand;
		}

		protected override Expression VisitTuple(SqlTupleExpression tupleExpression)
		{
			this.Write('(');

			var i = 0;

			foreach (var expression in tupleExpression.SubExpressions)
			{
				this.Visit(expression);

				if (i != tupleExpression.SubExpressions.Count - 1)
				{
					this.Write(" ,");
				}
				i++;
			}

			this.Write(')');

			return tupleExpression;
		}

		protected override Expression VisitCreateTable(SqlCreateTableExpression createTableExpression)
		{
			this.Write("CREATE TABLE ");
			this.Write(this.sqlDialect.NameQuoteChar);
			this.Write(createTableExpression.TableName);
			this.Write(this.sqlDialect.NameQuoteChar);
			this.WriteLine();
			this.Write("(");
			
			using (AcquireIndentationContext())
			{
				var i = 0;

				foreach (var expression in createTableExpression.ColumnDefinitionExpressions)
				{
					this.Visit(expression);

					if (i != createTableExpression.ColumnDefinitionExpressions.Count - 1
						|| createTableExpression.TableConstraints.Count > 0)
					{
						this.Write(",");
						this.WriteLine();
					}

					i++;
				}

				i = 0;

				foreach (var expression in createTableExpression.TableConstraints)
				{
					this.Visit(expression);

					if (i != createTableExpression.TableConstraints.Count - 1)
					{
						this.WriteLine(",");
					}

					i++;
				}
			}

			this.WriteLine();
			;
			this.WriteLine(");");
			this.WriteLine();

			return createTableExpression;
		}

		protected override Expression VisitSimpleConstraint(SqlSimpleConstraintExpression simpleConstraintExpression)
		{
			switch (simpleConstraintExpression.Constrant)
			{
				case SqlSimpleConstraint.DefaultValue:
					if (simpleConstraintExpression.Value != null)
					{
						this.Write(" DEFAULT ");
						this.Write(simpleConstraintExpression.Value);
					}
					break;
				case SqlSimpleConstraint.NotNull:
					this.Write("NOT NULL ");
					break;
				case SqlSimpleConstraint.PrimaryKey:
					this.Write("PRIMARY KEY ");
					if (simpleConstraintExpression.ColumnNames != null)
					{
						var i = 0;
						foreach (var s in simpleConstraintExpression.ColumnNames)
						{
							this.Write(this.sqlDialect.NameQuoteChar);
							this.Write(s);
							this.Write(this.sqlDialect.NameQuoteChar);

							if (i != simpleConstraintExpression.ColumnNames.Length - 1)
							{
								this.Write(", ");
							}
						}
					}
					break;
				case SqlSimpleConstraint.Unique:
					this.Write("UNIQUE(");
					if (simpleConstraintExpression.ColumnNames != null)
					{
						var i = 0;
						foreach (var s in simpleConstraintExpression.ColumnNames)
						{
							this.Write(this.sqlDialect.NameQuoteChar);
							this.Write(s);
							this.Write(this.sqlDialect.NameQuoteChar);

							if (i != simpleConstraintExpression.ColumnNames.Length - 1)
							{
								this.Write(", ");
							}
						}
					}
					this.Write(")");
					break;
				default:
					break;
			}

			return simpleConstraintExpression;
		}

		protected override Expression VisitColumnDefinition(SqlColumnDefinitionExpression columnDefinitionExpression)
		{
			this.Write(this.sqlDialect.NameQuoteChar);
			this.Write(columnDefinitionExpression.ColumnName);
			this.Write(this.sqlDialect.NameQuoteChar);
			this.Write(' ');
			this.Write(columnDefinitionExpression.ColumnTypeName);

			var i = 0;

			foreach (var constraint in columnDefinitionExpression.ConstraintExpressions)
			{
				if (i == 0)
				{
					this.Write(' ');
				}

				this.Visit(constraint);
				this.Write(' ');

				if (i != columnDefinitionExpression.ConstraintExpressions.Count - 1)
				{
					this.Write(',');
				}

				i++;
			}

			return columnDefinitionExpression;
		}
	}
}
