// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Shaolinq.Persistence.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Optimizers
{
	public class SqlObjectOperandComparisonExpander
		: SqlExpressionVisitor
	{
		private SqlObjectOperandComparisonExpander()
		{
		}

		public static Expression Expand(Expression expression)
		{
			var expander = new SqlObjectOperandComparisonExpander();

			return expander.Visit(expression);
		}

		protected override Expression VisitProjection(SqlProjectionExpression projection)
		{
			var source = (SqlSelectExpression)this.Visit(projection.Select);
			var projector = this.Visit(projection.Projector);
			var aggregator = (LambdaExpression) this.Visit(projection.Aggregator);

			if (source != projection.Select || projector != projection.Projector || aggregator != projection.Aggregator)
			{
				return new SqlProjectionExpression(source, projector, aggregator, projection.IsElementTableProjection);
			}

			return projection;
		}

		internal static IEnumerable<Expression> GetPrimaryKeyElementalExpressions(Expression expression)
		{

			if (expression is MemberInitExpression initExpression)
			{
				var memberInitExpression = initExpression;

				foreach (var value in memberInitExpression
					.Bindings
					.OfType<MemberAssignment>()
					.Where(c => c.Member is PropertyInfo)
					.Where(binding => PropertyDescriptor.IsPropertyPrimaryKey((PropertyInfo)binding.Member))
					.Select(c => c.Expression))
				{
					if (value is MemberInitExpression || value is SqlObjectReferenceExpression)
					{
						foreach (var inner in GetPrimaryKeyElementalExpressions(value))
						{
							yield return inner;
						}
					}
					else
					{
						yield return value;
					}
				}

				yield break;
			}

			if (!(expression is SqlObjectReferenceExpression referenceExpression))
			{
				yield break;
			}

			var operand = referenceExpression;

			foreach (var value in operand
				.GetBindingsFlattened()
				.OfType<MemberAssignment>()
				.Select(c => c.Expression))
			{
				if (value is MemberInitExpression || value is SqlObjectReferenceExpression)
				{
					foreach (var inner in GetPrimaryKeyElementalExpressions(value))
					{
						yield return inner;
					}
				}
				else
				{
					yield return value;
				}
			}
		}

		protected override Expression VisitFunctionCall(SqlFunctionCallExpression functionCallExpression)
		{
			if (functionCallExpression.Arguments.Count == 1
				&& (functionCallExpression.Function == SqlFunction.IsNotNull || functionCallExpression.Function == SqlFunction.IsNull)
				&& (functionCallExpression.Arguments[0].NodeType == ExpressionType.MemberInit || functionCallExpression.Arguments[0].NodeType == (ExpressionType)SqlExpressionType.ObjectReference))
			{
				Expression retval = null;
				
				foreach (var value in GetPrimaryKeyElementalExpressions(functionCallExpression.Arguments[0]))
				{
					var current = new SqlFunctionCallExpression(functionCallExpression.Type, functionCallExpression.Function, value);

					if (retval == null)
					{
						retval = current;
					}
					else
					{
						retval = Expression.And(retval, current);
					}
				}

				return retval;
			}

			return base.VisitFunctionCall(functionCallExpression);
		}

		protected override Expression VisitBinary(BinaryExpression binaryExpression)
		{
			if ((binaryExpression.Left.NodeType == (ExpressionType)SqlExpressionType.ObjectReference || binaryExpression.Left.NodeType == ExpressionType.MemberInit)
				&& (binaryExpression.Right.NodeType == (ExpressionType)SqlExpressionType.ObjectReference || binaryExpression.Right.NodeType == ExpressionType.MemberInit)
				&& (binaryExpression.Left.Type.IsAssignableFrom(binaryExpression.Right.Type) || binaryExpression.Right.Type.IsAssignableFrom(binaryExpression.Left.Type)))
			{
				Expression retval = null;
				var leftOperand = binaryExpression.Left;
				var rightOperand = binaryExpression.Right;

				foreach (var value in GetPrimaryKeyElementalExpressions(this.Visit(leftOperand))
					.Zip(GetPrimaryKeyElementalExpressions(this.Visit(rightOperand)), (left, right) => new { Left = left, Right = right }))
				{
					Expression current;
					var left = value.Left;
					var right = value.Right;
					
					switch (binaryExpression.NodeType)
					{
						case ExpressionType.Equal:
							current = Expression.Equal(left, right);
							break;
						case ExpressionType.NotEqual:
							current = Expression.NotEqual(left, right);
							break;
						default:
							throw new NotSupportedException($"Operation on DataAccessObject with {binaryExpression.NodeType.ToString()} not supported");
					}
					
					if (retval == null)
					{
						retval = current;
					}
					else
					{
						retval = Expression.And(retval, current);
					}
				}

				return retval;
			}

			return base.VisitBinary(binaryExpression);
		}
	}
}
