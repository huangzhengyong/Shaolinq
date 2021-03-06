// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace Shaolinq.Persistence
{
	public abstract class SqlDataType
	{
		protected readonly ConstraintDefaultsConfiguration constraintDefaultsConfiguration;
		protected static readonly MethodInfo IsDbNullMethod = DataRecordMethods.IsNullMethod;

		public Type SupportedType { get; }
		public Type UnderlyingType { get; }
		public bool IsUserDefinedType { get; }

		/// <summary>
		/// Converts the given value for serializing to SQL.  The default
		/// implementation performs no conversion.
		/// </summary>
		/// <param name="value">The value</param>
		/// <returns>The converted value</returns>
		public virtual TypedValue ConvertForSql(object value)
		{
			if (this.UnderlyingType != null)
			{
				return new TypedValue(this.UnderlyingType, value);
			}
			else
			{
				return new TypedValue(this.SupportedType, value);
			}
		}

		protected SqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type supportedType)
			: this(constraintDefaultsConfiguration, supportedType, false)
		{	
		}

		protected SqlDataType(ConstraintDefaultsConfiguration constraintDefaultsConfiguration, Type supportedType, bool isUserDefinedType)
		{
			this.constraintDefaultsConfiguration = constraintDefaultsConfiguration;
			this.SupportedType = supportedType;
			this.IsUserDefinedType = isUserDefinedType;
			this.UnderlyingType = Nullable.GetUnderlyingType(supportedType);
		}

		/// <summary>
		/// Gets the SQL type name for the given property.
		/// </summary>
		/// <param name="propertyDescriptor">The property whose return type is to be serialized</param>
		/// <returns>The SQL type name</returns>
		public string GetSqlName(PropertyDescriptor propertyDescriptor)
		{
			return this.GetSqlName(propertyDescriptor, null);
		}

		/// <summary>
		/// Gets the SQL type name for the given property.
		/// </summary>
		/// <returns>The SQL type name</returns>
		public abstract string GetSqlName(PropertyDescriptor propertyDescriptor, ConstraintDefaultsConfiguration constraintDefaults);

		/// <summary>
		/// Gets an expression to perform reading of a column.
		/// </summary>
		/// <param name="dataReader">The parameter that references the <see cref="IDataReader"/></param>
		/// <param name="ordinal">The parameter that contains the ordinal of the column to read</param>
		/// <returns>An expression for reading the column into a value</returns>
		public abstract Expression GetReadExpression(Expression dataReader, int ordinal);

		public virtual Expression IsNullExpression(Expression dataReader, int ordinal)
		{
			return Expression.Call(dataReader, IsDbNullMethod, Expression.Constant(ordinal));
		}
	}
}
