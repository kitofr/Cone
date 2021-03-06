﻿using Cone.Core;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace Cone.Runners
{
    public class TestSession
    {
        class TestResult : ITestResult
        {
            readonly IConeTest test;
			readonly ITestLogger log;

            public TestResult(IConeTest test, ITestLogger log) {
                this.test = test;
				this.log = log;
            }

			public ITestName TestName { get { return test.TestName; } }
			public TestStatus Status { get; private set; }

            void ITestResult.Success() { 
				Status = TestStatus.Success;
				log.Success();
			}

            void ITestResult.Pending(string reason) { 
				Status = TestStatus.Pending;
				log.Pending(reason);
			}
            
            void ITestResult.BeforeFailure(Exception ex) {
				Fail(TestStatus.SetupFailure, FailureType.Setup, ex);
            }

            void ITestResult.TestFailure(Exception ex) {
				Fail(TestStatus.TestFailure, FailureType.Test, ex);
            }
            
            void ITestResult.AfterFailure(Exception ex) {
				Fail(TestStatus.TeardownFailure, FailureType.Teardown, ex);
            }

			void Fail(TestStatus status, FailureType failureType, Exception ex) {
				Status = status;
				var fixtureFailure = ex as FixtureException;
				if(fixtureFailure != null)
					for(var i = 0; i != fixtureFailure.Count; ++i)
						log.Failure(new ConeTestFailure(test.TestName, fixtureFailure[i], failureType));
				else
					log.Failure(new ConeTestFailure(test.TestName, ex, failureType));
			}
        }

        class TestSessionReport : ISessionLogger, ISuiteLogger, ITestLogger
        {
            int Passed;
            int Failed { get { return failures.Count; } }
            int Excluded;
            int Total { get { return Passed + Failed + Excluded; } }
            Stopwatch timeTaken;
            readonly List<ConeTestFailure> failures = new List<ConeTestFailure>();

            public void BeginSession() {
                timeTaken = Stopwatch.StartNew();
            }

            public void EndSession() {
                timeTaken.Stop();
            }

            public ISuiteLogger BeginSuite(IConeSuite suite) {
                return this;
            }

            public void EndSuite() { }

            public ITestLogger BeginTest(IConeTest test) {
                return this;
            }

            public void WriteInfo(Action<TextWriter> output) { }

            public void Success() { Interlocked.Increment(ref Passed); }

            public void Failure(ConeTestFailure failure) { lock(failures) failures.Add(failure); }

            public void Pending(string reason) { }

            public void Skipped() { Interlocked.Increment(ref Excluded); }

			public void EndTest() { }

            public void WriteReport(TextWriter output) {
                output.WriteLine();
                output.WriteLine("{0} tests found. {1} Passed. {2} Failed. ({3} Skipped)", Total, Passed, Failed, Excluded);

                if (failures.Count > 0) {
                    output.WriteLine("Failures:");
                    failures.ForEach((n, failure) => output.WriteLine("{0}) {1}", 1 + n, failure));
                }
                output.WriteLine();
                output.WriteLine("Done in {0}.", timeTaken.Elapsed);
            }
        }

		readonly ISessionLogger sessionLog;
        readonly TestSessionReport report = new TestSessionReport();

        public TestSession(ISessionLogger sessionLog) {
            this.sessionLog = new MulticastSessionLogger(sessionLog, report);
        }

		public Predicate<IConeTest> ShouldSkipTest = _ => false; 
		public Predicate<IConeSuite> IncludeSuite = _ => true;
		public Func<IConeFixture, ITestExecutor> GetTestExecutor = x => new TestExecutor(x);

        public void RunSession(Action<Action<ConePadSuite>> @do) {
            sessionLog.BeginSession();
            @do(CollectSuite);
            sessionLog.EndSession();
        }

        void CollectSuite(ConePadSuite suite) {
            var log = sessionLog.BeginSuite(suite);
            suite.Run((tests, fixture) => CollectResults(tests, fixture, log));
            log.EndSuite();
        }

        void CollectResults(IEnumerable<IConeTest> tests, IConeFixture fixture, ISuiteLogger suiteLog) {
			var testExecutor = GetTestExecutor(fixture);
			var beforeFailure = new Lazy<Exception>(() => Initiaize(testExecutor));
			tests.ForEach(test =>
				suiteLog.WithTestLog(test, log => {
					if(ShouldSkipTest(test))
						log.Skipped();
					else {
						ITestResult result = new TestResult(test, log);
						if(beforeFailure.Value != null)
							result.BeforeFailure(beforeFailure.Value);
						else
							testExecutor.Run(test, result);
					}
				}));

			try {
				testExecutor.Relase();
			} catch { }
        }

		static Exception Initiaize(ITestExecutor executor) {
			try {
				executor.Initialize();
				return null;
			} catch(Exception ex) {
				return ex;
			}
		}

	    public void Report() {
            sessionLog.WriteInfo(output => report.WriteReport(output));
        }
    }
}
