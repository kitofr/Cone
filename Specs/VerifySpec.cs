﻿using System;
using System.Linq.Expressions;
using NUnit.Framework;

namespace Cone
{
    class Counter
    {
        int next;

        public int Next() { return next++; }
        public int Next(int step) {
            var n = next;
            next += step;
            return n;
        }

        public bool ReturnsFalse() { return false; }
    }

    [Describe(typeof(Verify))]
    public class VerifySpec
    {
        static int TheAnswer = 42;

        public void should_evaluate_only_once() {
            var counter = new Counter();
            try {
                Verify.That(() => counter.Next() != 0);
            } catch { }
            Verify.That(() => counter.Next() == 1);
        }

        public void support_constant_expressions() {
            Verify.That(() => 1 + 2 == 3);
        }

        public void supports_null_values_as_actual() {
            Counter x = null;
            Verify.That(() => x == null);
        }

        public void support_identity_checking() {
            var obj = new Counter();
            Verify.That(() => object.ReferenceEquals(obj, obj) == true);
        }

        public void support_static_fields() {
            Verify.That(() => TheAnswer == 42);
        }

        public void negated_expression() {
            Verify.That(() => !(TheAnswer == 7));
        }

        public class PossiblyGreen
        {
            public bool IsGreen { get { return true; } }
        }

        public void bool_member_access() {
            var obj = new PossiblyGreen();
            Verify.That(() => obj.IsGreen);
        }

        [Context("binary expressions")]
        public class BinaryExpressions
        {
            int a = 1, b = 2;

            public void equal() {
                Verify.That(() => a == a);
            }

            public void not_equal() {
                Verify.That(() => a != b);
            }

            public void less() {
                Verify.That(() => a < b);
            }

            public void less_or_equal() {
                Verify.That(() => a <= a);
            }

            public void greater() {
                Verify.That(() => b > a);
            }

            public void greater_or_equal() {
                Verify.That(() => b >= a);
            }
        }

        [Context("Exceptions")]
        public class Exceptions
        {
            public void raises_expectation_failed_when_wrong_type_of_excpetion_raised() {
                try {
                    Verify.Exception<NotSupportedException>(() => NotImplemented());
                    throw new NotSupportedException();
                } catch (Exception e) {
                    Verify.That(() => e.GetType() == ExpectedExcpetionType());
                }
            }

            public void passes_when_Exception_types_match() {
                Verify.Exception<NotImplementedException>(() => NotImplemented());
            }

            public void rasises_expectation_failed_when_exception_missing() {
                try {
                    Verify.Exception<NotSupportedException>(() => Nothing());
                    throw new NotSupportedException();
                } catch (Exception e) {
                    Verify.That(() => e.GetType() == ExpectedExcpetionType());
                }
            }

            public void verify_Exception_message() {
                var e = Verify.Exception<NotImplementedException>(() => NotImplemented());
                Verify.That(() => e.GetType() == typeof(NotImplementedException));
                Verify.That(() => e.Message == new NotImplementedException().Message);
            }

            void Nothing() { }
            void NotImplemented() { throw new NotImplementedException(); }

            Type ExpectedExcpetionType() {
                try {
                    Verify.ExpectationFailed(string.Empty);
                } catch (Exception e) { 
                    return e.GetType(); 
                }
                return null;
            }
        }
    }
}
