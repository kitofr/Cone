﻿using System;
using System.Reflection;

namespace Cone.Core
{
    public class ConeFixture : IConeFixture
    {
        readonly IFixtureHolder fixtureHolder;
        public ConeFixture(IFixtureHolder fixtureHolder) {
            this.fixtureHolder = fixtureHolder;
        }

        public event EventHandler Before;

        void ITestInterceptor.Before() { 
            InvokeAll(SetupMethods, null);
            Before.Raise(this, EventArgs.Empty);
        }

        void ITestInterceptor.After(ITestResult testResult) {
            InvokeAll(AfterEachWithResult, new[] { testResult });
            InvokeAll(TearDownMethods, null);
        }

        public object Invoke(MethodInfo method, params object[] parameters) {
            return method.Invoke(Fixture, parameters);
        }

        object NewFixture() { 
            var ctor = FixtureType.GetConstructor(Type.EmptyTypes);
            if(ctor == null)
                throw new NotSupportedException("No compatible constructor found for " + FixtureType.FullName);
            return ctor.Invoke(null);
        }

        void InvokeAll(MethodInfo[] methods, object[] parameters) {
            for (int i = 0; i != methods.Length; ++i)
                methods[i].Invoke(Fixture, parameters);
        }

        public Type FixtureType { get { return fixtureHolder.FixtureType; } }

        public object Fixture { 
            get { return fixtureHolder.Fixture ?? (Fixture = NewFixture()); }
            set { fixtureHolder.Fixture = value; }
        }

        MethodInfo[] SetupMethods { get { return fixtureHolder.SetupMethods; } }
        MethodInfo[] TearDownMethods { get { return fixtureHolder.TeardownMethods; } }
        MethodInfo[] AfterEachWithResult { get { return fixtureHolder.AfterEachWithResult; } }

    }
}
