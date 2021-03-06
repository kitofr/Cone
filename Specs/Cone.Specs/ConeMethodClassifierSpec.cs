﻿using System;
using System.Linq.Expressions;
using System.Reflection;
using Cone.Runners;
using Moq;

namespace Cone.Core
{
    [Describe(typeof(ConeMethodClassifier))]
    public class ConeMethodClassifierSpec
    {
        public interface IMultiSink : IConeFixtureMethodSink, IConeTestMethodSink { }

        Mock<IMultiSink> Classify(MethodInfo method) {
            var sink = new Mock<IMultiSink >();
            new ConeMethodClassifier(sink.Object, sink.Object).Classify(method);
            return sink;
        }

        MethodInfo Method(Expression<Action<SampleFixture>> x) {
            return ((MethodCallExpression)x.Body).Method;
        }

        public void Object_methods_are_unintresting() {
            var method = Method(x => x.ToString());
            Classify(method).Verify(x => x.Unintresting(method));
        }

        public void public_niladic_methods_are_tests() {
            var method = Method(x => x.Test());
            Classify(method).Verify(x => x.Test(method, ExpectedTestResult.None));
        }

        public void public_niladic_methods_with_returnvalues_are_not_tests() {
            var method = Method(x => x.Test());
            Classify(method).Verify(x => x.Unintresting(method));
        }

		public void public_with_RowAttribute_are_row_tests() {
            var method = Method(x => x.RowTest(42));
            Classify(method).Verify(x => x.RowTest(method, It.IsAny<IRowData[]>()));
        }

        public void public_niladic_returning_row_test_data_sequence_is_row_source() {
            var method = Method(x => x.RowSource());
            Classify(method).Verify(x => x.RowSource(method));
        }

        public void public_with_BeforeAll_are_before_all() {
            var method = Method(x => x.BeforeAll());
            Classify(method).Verify(x => x.BeforeAll(method));
        }

        public void public_with_BeforeEach_are_before_each() {
            var method = Method(x => x.BeforeEach());
            Classify(method).Verify(x => x.BeforeEach(method));
        }

        public void public_with_AfterEach_are_after_each() {
            var method = Method(x => x.AfterEach());
            Classify(method).Verify(x => x.AfterEach(method));
        }

        public void public_with_AfterEach_and_test_result_paramaters_is_after_each_with_result() {
            var method = Method(x => x.AfterEachWithResult(null));
            Classify(method).Verify(x => x.AfterEachWithResult(method));
        }

        public void public_with_AfterAll_are_after_all() {
            var method = Method(x => x.AfterAll());
            Classify(method).Verify(x => x.AfterAll(method));
        }
    }
}
