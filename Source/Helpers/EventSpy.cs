﻿using System;

namespace Cone.Helpers
{
    public class EventSpy<T> : MethodSpy where T : EventArgs
    {
        public EventSpy() : base(new EventHandler<T>((s, e) => { })) { }

        public static implicit operator EventHandler<T>(EventSpy<T> self) {
            return self.HandleEvent;
        }

		void HandleEvent(object sender, T args){ 
			Called(sender, args);
		}

        public void Then(EventHandler<T> then) {
            base.Then(then);
        }
    }
}
