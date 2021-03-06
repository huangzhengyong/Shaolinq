﻿// Copyright (c) 2007-2017 Thong Nguyen (tumtumtum@gmail.com)

using System;
using System.IO;
using NUnit.Framework;
using Shaolinq.Persistence.Computed;

namespace Shaolinq.Tests
{
	[TestFixture]
	public class ComputedExpressionParserTests
	{
		public static int Bar()
		{
			return 3;
		}

		public class TestObject
		{
			public int A { get; set; }
			public int B { get; set; }

			public TestObject C;

			public long Foo()
			{
				return 101;
			}

			public static string Make<T>(object x, T value)
			{
				return value.ToString();
			}
		}

		[Test]
		public void TestParse()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("C.Foo()"), property, null, property.PropertyType).Compile();

			var obj = new TestObject
			{
				B = 10,
				C = new TestObject {B = 20}
			};

			Console.WriteLine(((Func<TestObject, int>)func)(obj));
		}

		[Test]
		public void TestParse2()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("A = value + 1000"), property, null, property.PropertyType).Compile();

			var result = func.DynamicInvoke(new TestObject());
		}

		[Test]
		public void TestParse3()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func =
				ComputedExpressionParser.Parse
					(new StringReader("A = Shaolinq.Tests.ComputedExpressionParserTests.Bar() + 1"), property, new[] { typeof(TestObject) }, property.PropertyType).Compile();

			Assert.AreEqual(4, func.DynamicInvoke(new TestObject()));
		}

		[Test]
		public void TestParse4()
		{
			var property = typeof(TestObject).GetProperty("A");
			var func = ComputedExpressionParser.Parse(new StringReader("A = TestObject.Make(this, 1).GetHashCode()"), property, null, property.PropertyType).Compile();

			Assert.AreEqual("1".GetHashCode(), func.DynamicInvoke(new TestObject()));
		}
	}
}
