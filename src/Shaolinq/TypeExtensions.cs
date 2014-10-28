// Copyright (c) 2007-2014 Thong Nguyen (tumtumtum@gmail.com)

﻿using System;
﻿using System.Text;

namespace Shaolinq
{
	internal static class TypeExtensions
	{
		public static bool IsNullableType(this Type type)
		{
			return Nullable.GetUnderlyingType(type) != null;
		}

		public static bool IsDataAccessObjectType(this Type type)
		{
			return typeof(IDataAccessObjectAdvanced).IsAssignableFrom(type);
		}

		public static string ToHumanReadableName(this Type type)
		{
			var builder = new StringBuilder();

			type.AppendHumanReadableName(builder);

			return builder.ToString();
		}

		private static void AppendHumanReadableName(this Type type, StringBuilder builder)
		{
			if (type.IsGenericType)
			{
				builder.Append(type.Name.Remove(type.Name.LastIndexOf('`')));

				builder.Append("<");

				var i = 0;
				var genericArgs = type.GetGenericArguments();

				foreach (var innerType in genericArgs)
				{
					innerType.AppendHumanReadableName(builder);

					if (i != genericArgs.Length - 1)
					{
						builder.Append(", ");
					}
					i++;
				}

				builder.Append(">");
			}
			else
			{
				builder.Append(type.Name);
			}
		}
	}
}
