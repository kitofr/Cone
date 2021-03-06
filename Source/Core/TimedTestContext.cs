﻿using System;

namespace Cone.Core
{
    public class TimedTestContext : ITestExecutionContext
    {
	    readonly Action<IConeTest> before;
        readonly Action<IConeTest, TimeSpan> after;
	
	    public TimedTestContext(Action<IConeTest> before, Action<IConeTest, TimeSpan> after) {
            this.before = before;
            this.after = after;
	    }

	    public TestContextStep Establish(IFixtureContext context, TestContextStep next) {
		    return (test, r) => {
                before(test);
                test.Timed(_ => next(test, r), after);
		    };
	    }
    }
}
