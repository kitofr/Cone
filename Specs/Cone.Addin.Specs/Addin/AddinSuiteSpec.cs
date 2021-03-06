﻿using System;
using NUnit.Core;
using NUnit.Framework;

namespace Cone.Addin
{
    public class SampleFixture 
    {
        public static int AfterEachCalled = 0;

        [AfterEach]
        public void AfterEach(ITestResult result) {
            ++AfterEachCalled;
        }
    }

    [Describe(typeof(AddinSuite))]
    public partial class AddinSuiteSpec
    {
        static TestSuite BuildSuite(Type type){ return new AddinSuiteBuilder().BuildSuite(type); }

        [Context("Before")]
        public class Before
        {
            static int Magic = 0;
            int LocalMagic;

            [BeforeAll]
            public void InitializeMagic() {
                Magic = 21;
            }

            [BeforeEach]
            public void DoubleLocalMagic() {
                LocalMagic = Magic * 2;
            }

            public void the_magic_is_ready() {
                Check.That(() => Magic == 21);
                Check.That(() => LocalMagic == 42);
            }

            public void the_magic_can_be_reused() {
                Check.That(() => LocalMagic == 42);
            }
        }

        [Context("After")]
        public class After
        {
            internal static bool AfterAllExecuted = false;
            int Shared;

            [AfterAll]
            public void AfterAll() {
                AfterAllExecuted = true;
            }

            [AfterEach]
            public void ResetFixture() {
                Shared = 0;
            }

            public void AfterEach_called_between_tests() {
                Check.That(() => Shared == 0);
                Shared = 1;
            }

            public void AfterEach_called_between_tests_2() {
                Check.That(() => Shared == 0);
                Shared = 1;
            }

            [Context("each with result")]
            public class AfterEachWithResult
            {
                static int Product = 1;
                [AfterAll]
                public void VerifyProduct() {
                    Check.That(() => Product == 21);
                    Product = 1;
                }

                [AfterEach]
                public void Silly(ITestResult result) {
                   Check.That(() => result.Status == TestStatus.Success);
                    Product *= int.Parse(((NUnitTestResultAdapter)result).TestName.Name);
                }

                public void _3() { }
                public void _7() { }
            }
        }

        [Context("Nesting")]
        public class Nesting
        {
            [Context("Contexts")]
            public class Contexts : SampleFixture
            {
                public const int  TestCount = 1;
                public void works_as_expected() { }
            }

            public void zzz_inherited_after_each_in_context() {
                Check.That(() => SampleFixture.AfterEachCalled == Contexts.TestCount);
            }
        }

        [Context("Static")]
        public static class StaticFixture
        {
            public static bool StaticMethowWasCalled = false;
            public static void static_method() { StaticMethowWasCalled = true; }
        }

        [AfterAll]
        public void StaticFixture_was_run() {
            Check.That(() => StaticFixture.StaticMethowWasCalled);
        }

        [Describe(typeof(EmptySpec), Category = "Empty")]
        class EmptySpec
        {
            [Context("with multiple categories", Category = "Empty, Context")]
            public class EmptyContext { }
        }

        public void attaches_Category_to_described_suite() {
            var suite = BuildSuite(typeof(EmptySpec));
            Check.That(() => suite.Categories.Contains("Empty"));
        }

        public void multiple_categories_can_be_comma_seperated() {
            var suite = BuildSuite(typeof(EmptySpec)).Tests[0] as AddinSuite;
            Check.That(() => suite.Categories.Contains("Empty"));
            Check.That(() => suite.Categories.Contains("Context"));
        }

        public void zzz_rely_on_sorting_to_check_that_AfterAll_is_triggered() {
            //Check.That(() => After.AfterAllExecuted == true);
            Assert.That(After.AfterAllExecuted, Is.True);
        }
    }
}
