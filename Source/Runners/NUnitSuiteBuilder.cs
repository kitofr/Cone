using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cone.Core;

namespace Cone.Runners
{
	public class NUnitSuiteBuilder : ConePadSuiteBuilder
	{
		class NUnitFixtureDescription : IFixtureDescription 
		{
			private readonly Type type;

			public NUnitFixtureDescription(Type type) {
				this.type = type;
			}

			public IEnumerable<string> Categories
			{
				get { 
					return type.GetCustomAttributes(true)
						.Select(x => new { Type = x.GetType(), Item = x })
						.Where(x => x.Type.FullName == "NUnit.Framework.CategoryAttribute")
						.Select(x => x.Type.GetProperty("Name").GetValue(x.Item, null).ToString());
				}
			}

			public string SuiteName
			{
				get { return type.Namespace; }
			}

			public string SuiteType
			{
				get { return "TestFixture"; }
			}

			public string TestName
			{
				get { return type.Name; }
			}
		}

		class NUnitSuite : ConePadSuite 
		{
			class NUnitMethodClassifier : MethodClassifier
			{
				public NUnitMethodClassifier(IConeFixtureMethodSink fixtureSink, IConeTestMethodSink testSink) : base(fixtureSink, testSink)
				{ }

				protected override void ClassifyCore(MethodInfo method) {
					var attributes = method.GetCustomAttributes(true);
					if(method.GetParameters().Length > 0) {
						ClassifyParameterized(method, attributes);
						return;
					}
					var attributeNames = attributes.Select(x => x.GetType().FullName).ToArray();
					var sunk = false;
					if(attributeNames.Any(x => x == "NUnit.Framework.SetUpAttribute")) {
						BeforeEach(method);
						sunk = true;
					}
					if(attributeNames.Any(x => x == "NUnit.Framework.TearDownAttribute")) {
						AfterEach(method);
						sunk = true;
					}
					if(attributeNames.Any(x => x == "NUnit.Framework.TestFixtureSetUpAttribute")) {
						BeforeAll(method);
						sunk = true;
					}
					if(attributeNames.Any(x => x == "NUnit.Framework.TestFixtureTearDownAttribute")) {
						AfterAll(method);
						sunk = true;
					}

					if(!sunk)
						Test(method);					
				}

				void ClassifyParameterized(MethodInfo method, object[] attributes) {
					var testCases = attributes.Where(x => x.GetType().FullName == "NUnit.Framework.TestCaseAttribute").ToList();
					if(testCases.Count == 0) {
						Unintresting(method);
						return;
					}
					RowTest(method, testCases.Select(x => (IRowData)new NUnitRowDataAdapter(x)));
				}

				class NUnitRowDataAdapter : IRowData 
				{
					readonly Type testCaseType;
					readonly object testCase;

					public NUnitRowDataAdapter(object testCase) {
						this.testCase = testCase;
						this.testCaseType = testCase.GetType();
					}

					public bool IsPending {
						get { return false; }
					}

					public string DisplayAs {
						get { return (string)GetPropertyValue("TestName"); }
					}

					public object[] Parameters {
						get { return (object[])GetPropertyValue("Arguments"); }
					}

					public object Result {
						get { return GetPropertyValue("Result"); }
					}

					object GetPropertyValue(string name) {
						return testCaseType.GetProperty(name).GetValue(testCase, null);
					}
				}

			}

			public NUnitSuite(ConeFixture fixture) : base(fixture) 
			{}

			protected override IMethodClassifier GetMethodClassifier(IConeFixtureMethodSink fixtureSink, IConeTestMethodSink testSink)
			{
				return new NUnitMethodClassifier(fixtureSink, testSink);
			}
		}

		public override bool SupportedType(Type type)
		{
			return type.GetCustomAttributes(true)
				.Any(x => x.GetType().FullName == "NUnit.Framework.TestFixtureAttribute"); 
		}

		public override IFixtureDescription DescriptionOf(Type fixtureType)
		{
			return new NUnitFixtureDescription(fixtureType);
		}

		protected override ConePadSuite NewSuite(Type type, IFixtureDescription description) {
            return new NUnitSuite(new ConeFixture(type, description.Categories)) { 
				Name = description.SuiteName + "." + description.TestName
			};
		}
	}
}