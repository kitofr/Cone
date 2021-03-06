﻿using System;

namespace Cone.Helpers
{
	[Describe(typeof(EventSpy<>))]
	public class EventSpySpec
	{
		class TestTarget
		{
			public event EventHandler<EventArgs> OnFoo;

			public void Foo() {
				var handler = OnFoo;
				if(handler != null)
					handler(this, EventArgs.Empty);
			}
		}

		public void records_number_of_invocations() {
			var target = new TestTarget();
			var fooSpy = new EventSpy<EventArgs>();

			target.OnFoo += fooSpy;
			target.Foo();
			Check.That(() => fooSpy.HasBeenCalled);
			fooSpy.Then((s, e) =>
				Check.That(
					() => Object.ReferenceEquals(s, target),
					() => e == EventArgs.Empty));
		}
	}
}
