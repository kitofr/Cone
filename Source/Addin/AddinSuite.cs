﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cone.Core;
using NUnit.Core;

namespace Cone.Addin
{
    public class AddinSuite : TestSuite, IConeSuite, IFixtureHolder
    {
        readonly TestExecutor testExecutor;
        readonly ConeTestNamer testNamer;
        readonly RowSuiteLookup<ConeRowSuite> rowSuites;
        readonly string suiteType;
        MethodInfo[] afterEachWithResult;
        readonly ConeFixture fixture;

        internal AddinSuite(Type type, IFixtureDescription description, ConeTestNamer testNamer) : base(description.SuiteName, description.TestName) {
            this.suiteType = description.SuiteType;
            this.testNamer = testNamer;
            this.fixture = new ConeFixture(type, this);
            this.fixture.Before += (s, e) => Verify.Context = ((ConeFixture)s).FixtureType;          
            this.testExecutor = new TestExecutor(this.fixture);
            this.rowSuites = new RowSuiteLookup<ConeRowSuite>((method, suiteSame) => {
                var suite = new ConeRowSuite(new ConeMethodThunk(method, testNamer), this, testExecutor, suiteSame);
                AddWithAttributes(method, suite);
                return suite;
            });

            var pending = type.FirstOrDefault((IPendingAttribute x) => x.IsPending);
            if(pending != null) {
                RunState = RunState.Ignored;
                IgnoreReason = pending.Reason;
            }
        }

        public string Name { get { return TestName.FullName; } }

        MethodInfo[] IFixtureHolder.FixtureSetupMethods { get { return fixtureSetUpMethods; } }
        MethodInfo[] IFixtureHolder.FixtureTeardownMethods { get  { return fixtureTearDownMethods; } }
        MethodInfo[] IFixtureHolder.SetupMethods { get { return setUpMethods; } }
        MethodInfo[] IFixtureHolder.TeardownMethods { get { return tearDownMethods; } }
        MethodInfo[] IFixtureHolder.AfterEachWithResult { get { return afterEachWithResult; } }

        public override Type FixtureType { get { return fixture.FixtureType; } }
        public override string TestType { get { return suiteType; } }

        public override TestResult Run(EventListener listener, ITestFilter filter) {
            listener.SuiteStarted(TestName);
            var result = new TestResult(this);
            try { fixture.Initialize(); } 
            catch { }
            foreach(Test test in Tests)
                if(filter.Pass(test))
                    result.AddResult(test.Run(listener, filter));
            try { fixture.Teardown(); } 
            catch { }
            listener.SuiteFinished(result);
            return result;
        }

        public void BindTo(ConeFixtureMethods setup) {
            fixtureSetUpMethods = setup.BeforeAll;
            setUpMethods = setup.BeforeEach;
            tearDownMethods = setup.AfterEach;
            afterEachWithResult = setup.AfterEachWithResult;
            fixtureTearDownMethods = setup.AfterAll;
        }

        string NameFor(MethodInfo method) {
            return testNamer.NameFor(method);
        }

        public void AddCategories(IEnumerable<string> categories) {
            foreach(var item in categories)
                Categories.Add(item);
        }

        void AddTestMethod(ConeMethodThunk thunk) { 
            AddWithAttributes(thunk, new ConeTestMethod(thunk, this, testExecutor, thunk.NameFor(null))); 
        }
        
        void AddRowTest(MethodInfo method, IEnumerable<IRowData> rows) {
            rowSuites.GetSuite(method, testNamer.NameFor(method)).Add(rows);
        }

        void IConeSuite.AddSubsuite(IConeSuite suite) {
            AddWithAttributes(FixtureType, (Test)suite);
        }

        void AddWithAttributes(ICustomAttributeProvider method, Test test) {
            test.ProcessExplicitAttributes(method);
            Add(test);
        }

        void IConeTestMethodSink.Test(MethodInfo method) {
            AddTestMethod(new ConeMethodThunk(method, testNamer));
        }

        void IConeTestMethodSink.RowTest(MethodInfo method, IEnumerable<IRowData> rows) {
            AddRowTest(method, rows);
        }

        void IConeTestMethodSink.RowSource(MethodInfo method) {
            var rows = ((IEnumerable<IRowTestData>)fixture.Invoke(method))
                .GroupBy(x => x.Method, x => x as IRowData);
            foreach(var item in rows)
                AddRowTest(item.Key, item);
        }

        IConeSuite AsSuite() { return (IConeSuite)this; }
    }
}
 