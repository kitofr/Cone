﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Cone.Core;
using NUnit.Core;

namespace Cone.Addin
{
    class ConeRowSuite : ConeTest, IConeRowTestBuilder<Test>
    {
        readonly ConeMethodThunk thunk;
        readonly List<Test> tests = new List<Test>();

        class ConeRowTest : ConeTest
        {
            readonly object[] parameters;

            public ConeRowTest(object[] parameters, ConeRowSuite parent, string name) : base(parent, parent.testExecutor, name) {
                this.parameters = parameters;
            }

		    public override ICustomAttributeProvider Attributes { get { return Suite.Attributes; } }

            public override void Run(ITestResult testResult) { Thunk.Invoke(Fixture, parameters); }

            ICallable Thunk { get { return Suite.thunk; } }

            ConeRowSuite Suite { get { return ((ConeRowSuite)Parent); } }
        }

        public ConeRowSuite(ConeMethodThunk thunk, Test suite, TestExecutor testExecutor, string name)
            : base(suite, testExecutor, name) {
        
            this.thunk = thunk;
        }

        public void Add(IEnumerable<IRowData> rows) {
            tests.AddRange(ConeRowTestBuilder.BuildFrom(this, rows));
        }

        public override TestResult Run(EventListener listener, ITestFilter filter) {
            listener.SuiteStarted(TestName);
            return new TestResult(this).Timed(
                x => {
                    foreach(var item in tests.Where(filter.Pass))
                        x.AddResult(item.Run(listener, filter));
                }, 
                listener.SuiteFinished);
        }

        public override bool IsSuite { get { return true; } }

        public override int TestCount { get { return tests.Count; } }

        public override IList Tests { get { return tests; } }

		public override ICustomAttributeProvider Attributes { get { return thunk; } }

        public string NameFor(object[] parameters) {
            return thunk.NameFor(parameters);
        }

        public Test NewRow(string name, object[] parameters, TestStatus status) {
            var row = new ConeRowTest(parameters, this, name);
            if(status == TestStatus.Pending)
                row.RunState = RunState.Ignored;
            return row;
        }
    }
}
