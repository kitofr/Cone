﻿using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Cone.Core;

namespace Cone.Expectations
{
    public class StringMethodsProvider : IMethodExpectProvider
    {
        readonly Dictionary<MethodInfo, Func<object[], string>> methodDisplay;

        public StringMethodsProvider() {
            var s = typeof(string);
            var t = new[]{ s };
            methodDisplay = new Dictionary<MethodInfo,Func<object[], string>> {
                { s.GetMethod("Contains"), _ => "a string containing {0}" },
                { s.GetMethod("EndsWith", t), _ => "a string ending with {0}" },
                { s.GetMethod("EndsWith", new []{ typeof(string), typeof(StringComparison) }), x => string.Format("a string ending with {{0}} using '{0}'", x[1]) },
                { s.GetMethod("StartsWith", t), _ => "a string starting with {0}" }
            };
        }

        IEnumerable<MethodInfo> IMethodExpectProvider.GetSupportedMethods() {
            return methodDisplay.Keys;
        }

        IExpect IMethodExpectProvider.GetExpectation(Expression body, MethodInfo method, object target, object[] args) {
            return new StringMethodExpect(methodDisplay[method], body, method, target, args);
        }
    }

    public class StringMethodExpect : MemberMethodExpect
    {
        readonly Func<object[], string> methodDisplay;

        public StringMethodExpect(Func<object[], string> methodDisplay, Expression body, MethodInfo method, object target, object[] arguments):
            base(body, method, target, arguments) {
            this.methodDisplay = methodDisplay;
        }

        public override string FormatExpected(IFormatter<object> formatter) {
            return string.Format(methodDisplay(arguments), formatter.Format(arguments[0]));
        }
    }
}
