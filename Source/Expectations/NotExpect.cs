﻿using System.Linq.Expressions;
using Cone.Core;

namespace Cone.Expectations
{
    class NotExpect : IExpect
    {
        readonly IExpect inner;

        public NotExpect(IExpect inner) { this.inner = inner; }

        public ExpectResult Check() {
            var innerResult = inner.Check();
            innerResult.Success = !innerResult.Success;
            return innerResult;
        }

        public string FormatExpression(IFormatter<Expression> formatter) {
            return string.Format("!({0})", inner.FormatExpression(formatter));
        }

        string IExpect.FormatExpected(IFormatter<object> formatter) {
            return inner.FormatExpected(formatter);
        }

        public string FormatMessage(IFormatter<object> formatter) {
            return string.Empty;
        }
    }
}