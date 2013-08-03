﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shaolinq
{
	[AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
	public class DependsOnPropertyAttribute
		: Attribute
	{
		public string PropertyName { get; set; }

		public DependsOnPropertyAttribute(string propertyName)
		{
			this.PropertyName = propertyName;
		}
	}
}
