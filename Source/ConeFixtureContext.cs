﻿using System;
using System.Reflection;

namespace Cone
{
    class ConeFixtureContext : ITestContext
    {
        readonly IConeFixture fixture;

        public ConeFixtureContext(IConeFixture fixture) {
            this.fixture = fixture;
        }

        public Action<ITestResult> Establish(Action<ITestResult> next) {
            return result => {
                Maybe(fixture.Before, () => {
                        Maybe(() => next(result), 
                            result.Success, 
                            result.TestFailure);
                    }, result.BeforeFailure);
                try {
                    fixture.After(result);
                } catch(Exception ex) {
                    result.AfterFailure(ex);
                }
            };
        }

        static void Maybe(Action action, Action then, Action<Exception> fail) {
            try {
                action();
                then();
            } catch (TargetInvocationException ex) {
                fail(ex.InnerException);
            } catch (Exception ex) {
                fail(ex);
            }
        }
    }
}
