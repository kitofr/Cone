﻿using System.Diagnostics;
using System.Threading;
using NUnit.Core;

namespace Cone.Addin
{
    public abstract class ConeTest : Test, IConeTest
    {
        internal readonly TestExecutor testExecutor;

        static int id = 0;
        static TestName BuildTestName(Test suite, string name) {
            var testName = new TestName();
            testName.FullName = suite.TestName.FullName + "." + name;
            testName.Name = name;
            var testId = Interlocked.Increment(ref id);
            testName.TestID = new TestID(testId);
            return testName;
        }

        protected ConeTest(Test suite, TestExecutor testExecutor, string name): base(BuildTestName(suite, name)) {
            Parent = suite;
            this.testExecutor = testExecutor; 
        }

        public override object Fixture {
            get { return Parent.Fixture; }
            set { Parent.Fixture = value;  }
        }

        public override TestResult Run(EventListener listener, ITestFilter filter) {
            var nunitTestResult = new TestResult(this);
            ITestResult testResult = new NUnitTestResultAdapter(nunitTestResult);
            var time = Stopwatch.StartNew();
            
            listener.TestStarted(TestName);
            switch(RunState){               
                case RunState.Runnable: testExecutor.Run(this, testResult); break;
                case RunState.Ignored: testResult.Pending(IgnoreReason); break;
                case RunState.Explicit: goto case RunState.Runnable;
            }
            
            time.Stop();
            nunitTestResult.Time = time.Elapsed.TotalSeconds;
            listener.TestFinished(nunitTestResult);
            return nunitTestResult;
        }

        public override string TestType { get { return GetType().Name; } }

        public virtual void Run(ITestResult testResult){}
    }
}