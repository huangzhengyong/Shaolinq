// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using System.Text;
using Platform;

namespace Shaolinq.Persistence.Linq
{
	public abstract class SqlQueryFormatter
		: SqlExpressionVisitor
	{
		public const char DefaultParameterIndicatorChar = '@';
		protected internal static readonly string ParamNamePrefix = "SHAOLINQPARAM";

		protected enum Indentation
		{
			Same,
			Inner,
			Outer
		}

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

		public static string PrefixedTableName(string tableNamePrefix, string tableName)
		{
			if (!string.IsNullOrEmpty(tableNamePrefix))
			{
				return tableNamePrefix + tableName;
			}

			return tableName;
		}

		private int depth;
		protected TextWriter writer;
		protected List<TypedValue> parameterValues;
		internal int IndentationWidth { get; }
		protected bool canReuse = true;
		protected readonly SqlDataTypeProvider sqlDataTypeProvider;
		public string ParameterIndicatorPrefix { get; protected set; }
		protected List<Pair<int, int>> parameterIndexToPlaceholderIndexes;
		
		protected readonly SqlDialect sqlDialect;
		private readonly string stringQuote;
		private readonly string stringEscape;

		public virtual SqlQueryFormatResult Format(Expression expression)
		{
			this.depth = 0;
			this.canReuse = true;
			this.writer = new StringWriter(new StringBuilder(1024));
			this.parameterValues = new List<TypedValue>();
			this.parameterIndexToPlaceholderIndexes = new List<Pair<int, int>>();

			this.Visit(this.PreProcess(expression));

			return new SqlQueryFormatResult(this, this.writer.ToString(), this.parameterValues, this.canReuse ? this.parameterIndexToPlaceholderIndexes : null);
		}

		public virtual SqlQueryFormatResult Format(Expression expression, TextWriter writer)
		{
			this.depth = 0;
			this.canReuse = true;
			this.writer = writer;
			this.parameterValues = new List<TypedValue>();
			this.parameterIndexToPlaceholderIndexes = new List<Pair<int, int>>();

			this.Visit(this.PreProcess(expression));

			return new SqlQueryFormatResult(this,null, this.parameterValues, this.canReuse ? this.parameterIndexToPlaceholderIndexes : null);
		}

		protected SqlQueryFormatter(SqlDialect sqlDialect, TextWriter writer, SqlDataTypeProvider sqlDataTypeProvider)
		{
			this.sqlDialect = sqlDialect ?? new SqlDialect();
			this.writer = writer;
			
			this.sqlDataTypeProvider = sqlDataTypeProvider ?? new DefaultSqlDataTypeProvider(new ConstraintDefaultsConfiguration());
			this.stringEscape = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.StringEscape);
			this.stringQuote = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.StringQuote);
			this.ParameterIndicatorPrefix = this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.ParameterPrefix);
			this.IndentationWidth = 2;
		}

		public virtual string FormatConstant(object value)
		{
			if (value == null || value == DBNull.Value)
			{
				return this.sqlDialect.GetSyntaxSymbolString(SqlSyntaxSymbol.Null);
			}

			var dataTypeProvider = this.sqlDataTypeProvider.GetSqlDataType(value.GetType());

			var sqlDaType = dataTypeProvider.ConvertForSql(value);

			value = sqlDaType.Value;
			var type = sqlDaType.Type;
			

			type = Nullable.GetUnderlyingType(type) ?? type;

			if (type == typeof(string) || type.IsEnum)
			{
				var str = value.ToString();

				if (str.Contains(this.stringQuote))
				{
					return this.stringQuote + str.Replace(this.stringQuote, this.stringEscape + this.stringQuote) + this.stringQuote;
				}

				return this.stringQuote + str + this.stringQuote;
			}

			if (type == typeof(Guid))
			{
				var guidValue = (Guid)value;

				return this.stringQuote + guidValue.ToString("D") + this.stringQuote;
			}

			if (type == typeof(TimeSpan))
			{
				var timespanValue = (TimeSpan)value;

				return this.stringQuote + timespanValue + this.stringQuote;
			}

			if (type == typeof(DateTime))
			{
				var dateTime = ((DateTime)value).ToUniversalTime();

				return this.stringQuote + dateTime.ToString("yyyy-MM-dd HH:mm:ss.fffff") + this.stringQuote;
			}

			return Convert.ToString(value);
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

		public virtual void WriteLine()
		{
			this.writer.WriteLine();

			for (var i = 0; i < this.depth * this.IndentationWidth; i++)
			{
				this.writer.Write(' ');
			}
		}

		public virtual void WriteLine(object line)
		{
			this.writer.Write(line);
			this.writer.WriteLine();

			for (var i = 0; i < this.depth * this.IndentationWidth; i++)
			{
				this.writer.Write(' ');
			}
		}

		public virtual void Write(object value)
		{
			this.writer.Write(value);
		}

		public virtual void WriteFormat(string format, params object[] args)
		{
			this.writer.Write(format, args);
		}

		protected virtual Expression PreProcess(Expression expression)
		{
			return expression;
		}

		protected void WriteDeliminatedListOfItems(IEnumerable listOfItems, Action<object> action, string deliminator = ", ")
		{
			var i = 0;

			foreach (var item in listOfItems)
			{
				if (i++ > 0)
				{
					this.Write(deliminator);
				}

				action(item);
			}
		}

		protected void WriteDeliminatedListOfItems<T>(IEnumerable<T> listOfItems, Action<T> action, string deliminator = ", ")
		{
			var i = 0;

			foreach (var item in listOfItems)
			{
				if (i++ > 0)
				{
					this.Write(deliminator);
				}

				action(item);
			}
		}
		
		protected void WriteDeliminatedListOfItems<T>(IEnumerable<T> listOfItems, Action<T> action, Action deliminationAction)
		{
			var i = 0;

			foreach (var item in listOfItems)
			{
				if (i++ > 0)
				{
					deliminationAction();
				}

				action(item);
			}
		}
	}
}

