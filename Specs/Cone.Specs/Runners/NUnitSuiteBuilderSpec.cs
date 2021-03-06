﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Cone.Core;
using NUnit.Framework;
using System.IO;

//mimic the NUnit framework attributes, matches must be name based to avoid referenceing nunit.
namespace NUnit.Framework
{
	public class TestAttribute : Attribute { }
	public class TestFixtureAttribute : Attribute { }

	public class TestFixtureSetUpAttribute : Attribute { }

	public class SetUpAttribute : Attribute { }

	public class TearDownAttribute : Attribute { }

	public class TestFixtureTearDownAttribute : Attribute { }

	[AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
	public class CategoryAttribute : Attribute 
	{
		readonly string @name;

		public CategoryAttribute(string name) {
			this.@name = name;
		}

 		public string Name { get { return @name; } }
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class TestCaseAttribute : Attribute
	{
		readonly object[] arguments;

		public TestCaseAttribute(params object[] arguments) {
			this.arguments = arguments;
		}

		public object[] Arguments { get { return arguments; } }
		public object Result { get; set; }
		public string TestName { get; set; }
	}

	[AttributeUsage(AttributeTargets.Method, AllowMultiple = true)]
	public class TestCaseSourceAttribute : Attribute
	{
		private readonly string sourceName;

		public TestCaseSourceAttribute(string sourceName) {
			this.sourceName = sourceName;
		}

		public string SourceName { get { return sourceName; } }
		public Type SourceType { get; set; }
	}

	public class TestCaseData 
	{
		readonly object[] arguments;

		public TestCaseData(params object[] arguments) {
			this.arguments = arguments;
		}

		public object[] Arguments { get { return arguments; } }
		public string TestName { get; set; }
		public object Result { get; set; }
	}

	public class ExpectedExceptionAttribute : Attribute
	{
		public ExpectedExceptionAttribute(Type expectedException) {
			this.ExpectedException = expectedException;
		}

		public Type ExpectedException { get; private set; }
	}
}

namespace Cone.Runners
{
	[TestFixture]
	public class NUnitCompatibilityTests
	{
		[TestCase(1, 1, Result = 2.0)
		,TestCaseSource("AddTestCases")]
		public double add(float a, float b) {
			return a + b;
		}

		[TestCase(1, 1, Result = 2)]
		public decimal decimal_add(decimal a, decimal b) {
			return a + b;
		}

		[TestCase(new[]{ 1, 2, 3 }, Result = 6)]
		public int sum(int[] xs) {
			return xs.Sum();
		}

		IEnumerable<TestCaseData> AddTestCases() {
			yield return new TestCaseData(1, 1) {
				Result = new decimal(2)
			};
		}

		[Test, ExpectedException(typeof(ArgumentException))]
		public void handle_expected_exception() {
			throw new ArgumentException();
		}
	}

	[Describe(typeof(NUnitSuiteBuilder))]
	public class NUnitSuiteBuilderSpec
	{
		class DerivedCategoryAttribute : CategoryAttribute 
		{
			public DerivedCategoryAttribute() : base("DerivedCategory") { }
		}

		[TestFixture, Category("SomeCategory"), Category("Integration"), DerivedCategory]
		class MyNUnitFixture
		{ 
			public int Calls;
			public int FixtureSetUpCalled;
			public int SetUpCalled;
			public int TestCalled;
			public int TearDownCalled;
			public int FixtureTearDownCalled;

			[TestFixtureSetUp]
			public void FixtureSetUp() { FixtureSetUpCalled = ++Calls; }

			[SetUp]
			public void SetUp() { SetUpCalled = ++Calls; }

			[Test]
			public void a_test(){ TestCalled = ++Calls; }

			public void NotATest() { }


			[TearDown]
			public void TearDown() { TearDownCalled = ++Calls; }

			[TestFixtureTearDown]
			public void FixtureTearDown() { FixtureTearDownCalled = ++Calls; }
		}

		NUnitSuiteBuilder SuiteBuilder = new NUnitSuiteBuilder(new DefaultObjectProvider());

		public void supports_building_suites_for_types_with_NUnit_TestFixture_attribute() {
			Check.That(() => SuiteBuilder.SupportedType(typeof(MyNUnitFixture)));
		}

		[Context("given description of MyNunitFixture")]
		public class NUnitSuiteBuilderFixtureDescriptionSpec
		{
			IFixtureDescription Description; 

			[BeforeAll]
			public void Given_description_of_MyNUnitFixture()
			{
				var suiteBuilder = new NUnitSuiteBuilder(new DefaultObjectProvider());
				Description = suiteBuilder.DescriptionOf(typeof(MyNUnitFixture));
			}

			public void suite_type_is_TestFixture() {
				Check.That(() => Description.SuiteType == "TestFixture");
			}

			public void test_name_is_name_of_fixture() {
				Check.That(() => Description.TestName == typeof(MyNUnitFixture).Name);
			}

			public void suite_name_is_namespace_of_fixture() {
				Check.That(() => Description.SuiteName == typeof(MyNUnitFixture).Namespace);
			}

			public void categories_found_via_CategoryAttribute() {
				Check.That(() => Description.Categories.Contains("SomeCategory"));
				Check.That(() => Description.Categories.Contains("Integration"));
			}

			public void categories_from_attributes_deriving_CategoryAttribute_are_found() {
				Check.That(() => Description.Categories.Contains("DerivedCategory"));
			}
		}

		[Context("given a fixture instance")]
		public class NUnitSuiteBuilderFixtureInstanceSpec
		{
			private MyNUnitFixture NUnitFixture;
			private ConePadSuite NUnitSuite;

			[BeforeEach]
			public void GivenFixtureInstance() {
				NUnitSuite = new NUnitSuiteBuilder(new LambdaObjectProvider(t => NUnitFixture = new MyNUnitFixture())).BuildSuite(typeof(MyNUnitFixture)); 
                new TestSession(new NullLogger()).RunSession(collectResult => collectResult(NUnitSuite));
			}

			public void FixtureSetUp_is_called_to_initialize_fixture() {
				Check.That(() => NUnitFixture.FixtureSetUpCalled == 1);
			}

			public void SetUp_is_called_as_BeforeEach() {
				Check.That(() => NUnitFixture.SetUpCalled == NUnitFixture.TestCalled - 1);
			}
			
			public void TearDown_is_called_as_AfterEach() {
				Check.That(() => NUnitFixture.TearDownCalled == NUnitFixture.TestCalled + 1);
			}

			public void TestFixtureTearDown_is_used_to_release_fixture() {
				Check.That(() => NUnitFixture.FixtureTearDownCalled == NUnitFixture.Calls);
			}
		}

		[Context("given fixture with TestCase's")]
		public class NUnitSuiteBuilderTestCaseSpec
		{
			class FixtureWithTestCases
			{
				[TestCase(1, 2, Result = 3, TestName = "1 + 2 = 3")]
				[TestCase(1, 1, Result = 3, TestName = "1 + 1 = 3")]
				public int add(int a, int b) { return a + b; }
			}

			ConePadSuite NUnitSuite;

			[BeforeEach]
			public void GivenFixtureWithTestCases() {
				NUnitSuite = new NUnitSuiteBuilder(new DefaultObjectProvider()).BuildSuite(typeof(FixtureWithTestCases)); 
			}

			public void theres_one_test_per_case() {
				Check.That(() => NUnitSuite.TestCount == 2);
			}
		}

		[Context("given fixture with TestCaseSoruce")]
		public class NUnitSuiteBuilderTestCaseSourceSpec
		{
			class FixtureWithTestCaseSource
			{
				[TestCaseSource("AddTestCaseSource")
				,TestCaseSource("PrivateTestCaseSource")
				,TestCaseSource("StaticTestCaseSource")
				,TestCaseSource("PropertyTestCaseSource")]
				public int add(int a, int b) { return a + b; }

				public IEnumerable<TestCaseData> AddTestCaseSource() {
					yield return new TestCaseData(1, 1) {
						TestName = "one + one = two",
						Result = 2,
					};
				}

				private IEnumerable<TestCaseData> PrivateTestCaseSource() {
					yield return new TestCaseData(1, 1);
				}

				private static IEnumerable StaticTestCaseSource() {
					yield return new TestCaseData(1, 1);
				}

				private static IEnumerable PropertyTestCaseSource {
					get { yield return new TestCaseData(1, 1); }
				}
			}

			ConePadSuite NUnitSuite;

			[BeforeEach]
			public void GivenFixtureWithTestCases() {
				NUnitSuite = new NUnitSuiteBuilder(new DefaultObjectProvider()).BuildSuite(typeof(FixtureWithTestCaseSource)); 
			}

			public void locates_TestCaseData_source_method_in_same_class() {
				Check.That(() => NUnitSuite.TestCount == 4);
			}

		}
	}
}
