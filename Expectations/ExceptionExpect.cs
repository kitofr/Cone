﻿using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Cone.Expectations
{
    public class ExceptionExpect : Expect
    {
        public static ExceptionExpect From(Expression<Action> expression, Type expected) {
            return new ExceptionExpect(expression.Body, ExceptionOrNull(expression), expected);
        }
       
        public static ExceptionExpect From<T>(Expression<Func<T>> expression, Type expected) {
            return new ExceptionExpect(expression.Body, ExceptionOrNull(expression.Body), expected);
        }
       
        ExceptionExpect(Expression body, object result, Type expected): base(body, result, expected) { }

        Type ExpectedExceptionType { get { return (Type)Expected; } }

        protected override bool CheckCore() {
            return actual != null && ExpectedExceptionType.IsAssignableFrom(actual.GetType());
        }

        static object ExceptionOrNull(Expression expression) {
            try {
                ExpressionEvaluator.EvaluateAs<object>(expression);
            } catch(Exception e) {
                return e;
            }
            return null;
        }

        public override string FormatExpression(IFormatter<Expression> formatter) {
            if(actual == null)
                return string.Format(ExpectMessages.MissingExceptionFormat, formatter.Format(body));
            return string.Format(ExpectMessages.UnexpectedExceptionFormat,
                formatter.Format(body), Expected, actual.GetType());
        }
    }
}
