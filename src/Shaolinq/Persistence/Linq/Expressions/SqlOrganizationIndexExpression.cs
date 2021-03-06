﻿using System.Collections.Generic;
using System.Linq.Expressions;

namespace Shaolinq.Persistence.Linq.Expressions
{
	public class SqlOrganizationIndexExpression
		: SqlIndexExpressionBase
	{
		public override ExpressionType NodeType => (ExpressionType)SqlExpressionType.OrganizationIndex;

		/// <summary>
		/// Creates a new <c>SqlOrganizationIndexExpression</c>
		/// </summary>
		/// <param name="indexName">The name of the index</param>
		/// <param name="columns">The columns in the index or null to remove an explicitly defined organization index</param>
		/// <param name="includedColumns">Columns to include int he organization index (default depends on underlying RDBMS)</param>
		public SqlOrganizationIndexExpression(string indexName, IReadOnlyList<SqlIndexedColumnExpression> columns, IReadOnlyList<SqlIndexedColumnExpression> includedColumns)
			: base(indexName, columns, includedColumns)
		{
		}

		public SqlOrganizationIndexExpression ChangeColumns(IReadOnlyList<SqlIndexedColumnExpression> columns)
		{
			return new SqlOrganizationIndexExpression(this.IndexName, columns, this.IncludedColumns);
		}

		public SqlOrganizationIndexExpression ChangeColumns(IReadOnlyList<SqlIndexedColumnExpression> columns, IReadOnlyList<SqlIndexedColumnExpression> includedColumns)
		{
			return new SqlOrganizationIndexExpression(this.IndexName, columns, includedColumns);
		}
	}
}