﻿using System;
using System.Linq.Expressions;
using Cone.Core;
using Cone.Expectations;
using Moq;

namespace Cone
{
    [Describe(typeof(EqualExpect))]
    public class BinaryExpectSpec
    {
        public void formats_actual_and_expected_values() {
            object actual = 42, expected = 7;
            var formatter = new Mock<IFormatter<object>>();
            formatter.Setup(x => x.Format(actual)).Returns("<actual>");
            formatter.Setup(x => x.Format(expected)).Returns("<expected>");

            var expect = new EqualExpect(Expression.MakeBinary(ExpressionType.Equal, Expression.Constant(actual), Expression.Constant(expected)), actual, expected);

            var expectedMessage = string.Format(ExpectMessages.EqualFormat, "<actual>", "<expected>");
            Verify.That(() => expect.FormatMessage(formatter.Object) == expectedMessage);
        }

        class MyClass
        {
            public object Value;

            public static bool operator==(MyClass left, MyClass right) {
                return left.Value.Equals(right.Value);
            }

            public static bool operator!=(MyClass left, MyClass right) {
                return !(left == right);
            }
        }

        public void operator_overloading_supported() {
            var actual = new MyClass { Value = 42 };
            var expected = new MyClass { Value = 42 };
            Expression<Func<bool>> expression = () => actual == expected;
            var expect = new BinaryExpect((BinaryExpression)expression.Body, actual, expected);

            Verify.That(() => (actual == expected) == true);
            Verify.That(() => expect.Check() == new ExpectResult { Actual = actual, Success = true });
        }
    }
}
