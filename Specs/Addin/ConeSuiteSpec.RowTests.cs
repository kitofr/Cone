﻿using System.Linq;
using NUnit.Core;
using NUnit.Framework;
using System.Collections.Generic;
using System.Linq.Expressions;
using System;
using System.Reflection;

namespace Cone.Addin
{
    [Describe(typeof(RowTestFixture))]
    public class RowTestFixture
    {
        [Row(1, 1, 2)]
        [Row(4, 2, 42, IsPending = true)]
        [Row(1, 1, 2, Name = "One + One is Two")]
        public void Add(int a, int b, int r) { Verify.That(() => a + b == r); }
    }

    class RowBuilder<T> : IEnumerable<IRowTestData>
    {
        readonly ConeTestNamer testNamer = new ConeTestNamer();
        readonly List<IRowTestData> rows = new List<IRowTestData>();

        class RowTestData : IRowTestData
        {
            readonly MethodInfo method;
            readonly object[] parameters;
            string name;
            bool isPending;

            public RowTestData(MethodInfo method, object[] parameters) {
                this.method = method;
                this.parameters = parameters;
            }

            public string Name  { get { return name ?? method.Name; } }

            public MethodInfo Method { get { return method; } }

            public object[] Parameters { get { return parameters;; } }

            public bool IsPending { get { return isPending; } }

            public RowTestData SetName(string name) {
                this.name = name;
                return this;
            }

            public RowTestData SetPending(bool isPending) {
                this.isPending = isPending;
                return this;
            }
        }

        public RowBuilder<T> Add(Expression<Action<T>> testCase) {
            var row = CreateRow((MethodCallExpression)testCase.Body); 
            rows.Add(row.SetName(testNamer.NameFor(row.Method, row.Parameters)));
            return this;
        }

        public RowBuilder<T> Add(string name, Expression<Action<T>> testCase) { 
            rows.Add(CreateRow((MethodCallExpression)testCase.Body).SetName(name));
            return this;
        }

        public RowBuilder<T> Pending(Expression<Action<T>> pendingTest) {
            var row = CreateRow((MethodCallExpression)pendingTest.Body).SetPending(true); 
            rows.Add(row.SetName(testNamer.NameFor(row.Method, row.Parameters)));
            return this;
        }

        RowTestData CreateRow(MethodCallExpression call) {
            var arguments = call.Arguments;
            var parameters = new object[arguments.Count];
            for(var i = 0; i != arguments.Count; ++i)
                parameters[i] = Collect(arguments[i]);
            return new RowTestData(call.Method, parameters);
        }

        object Collect(Expression expression) {
            return ((ConstantExpression)expression).Value;
        }

        IEnumerator<IRowTestData> IEnumerable<IRowTestData>.GetEnumerator() { return rows.GetEnumerator(); }

        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
            return rows.GetEnumerator(); 
        }
    }

    [Describe(typeof(DynamicRowTestFixture))]
    public class DynamicRowTestFixture
    {
        public void Add(int a, int b, int r) { Verify.That(() => a + b == r); }

        public IEnumerable<IRowTestData> Tests() {
            return new RowBuilder<DynamicRowTestFixture>()
                .Add(x => x.Add(1, 1, 2))
                .Pending(x => x.Add(4, 2, 42))
                .Add("One + One is Two", x => x.Add(1, 1, 2));
        }
    }

    public partial class ConeSuiteSpec
    {
        [Context("row based tests")]
        public class RowTests
        {
            public class RowTestFixtureSpec<T>
            {
                protected TestSuite Suite { get { return ConeSuite.For(typeof(T)); } }

                public void create_test_per_input_row() { Verify.That(() => Suite.TestCount == 3); }            

                public void rows_named_by_their_arguments() {
                    var testNames = Suite.Tests.Cast<ITest>().SelectMany(x => x.Tests.Cast<ITest>()).Select(x => x.TestName.Name);

                    Verify.That(() => testNames.Contains("Add(1, 1, 2)"));
                    Verify.That(() => testNames.Contains("Add(4, 2, 42)"));
                }

                public void can_use_custom_name() {
                    var testNames = Suite.Tests.Cast<ITest>().SelectMany(x => x.Tests.Cast<ITest>()).Select(x => x.TestName.Name);

                    Verify.That(() => testNames.Contains("One + One is Two"));                
                }
            }

            [Context("static row fixture")]
            public class StaticRowFixture : RowTestFixtureSpec<RowTestFixture> {}

            [Context("dynamic row fixture")]
            public class DynamicRowFixture : RowTestFixtureSpec<DynamicRowTestFixture>{}

            [Context("Before and After are triggered")]
            public class BeforeAndAfterRows
            {
                internal static int Magic;
                internal static int Passed;
                internal static int Pending;
                int Mojo;

                [BeforeAll]
                public void Initialize() {
                    Magic = 1;
                    Passed = Pending = 0;
                }

                [BeforeEach]
                public void BeforeEach() {
                    Verify.That(() => Magic == 1);
                    Mojo = Magic + Magic;
                }

                [AfterEach]
                public void AfterEach() { Mojo = 0; }

                [AfterEach]
                public void Tally(ITestResult testResult) {
                    switch (testResult.Status) {
                        case TestStatus.Success: ++Passed; break;
                        case TestStatus.Pending: ++Pending; break;
                    }
                }

                [AfterAll]
                public void AfterAll() { Magic = 0; }

                [Row(2, 0), Row(0, 2), Row(1, 1, IsPending = true)]
                public void calculate_magic(int a, int b) {
                    Verify.That(() => a + b == Mojo);
                }
            }

            public void zzz_put_me_last_to_check_that_AfterAll_for_rows_was_executed() {
                Verify.That(() => BeforeAndAfterRows.Magic == 0);
                Verify.That(() => BeforeAndAfterRows.Passed == 2);
                Verify.That(() => BeforeAndAfterRows.Pending == 0);//Should not execute for pending tests
            }
        }
    }
}
