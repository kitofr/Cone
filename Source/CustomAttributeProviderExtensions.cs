﻿using System;
using System.Reflection;

namespace Cone
{
    public static class CustomAttributeProviderExtensions
    {
        public static bool Has<T>(this ICustomAttributeProvider self) {
            return self.IsDefined(typeof(T), true);
        }

        public static bool Has<T>(this ICustomAttributeProvider self, Action<T[]> with) {
            var values = self.GetCustomAttributes(typeof(T), true);
            if(values.Length == 0)
                return false;
            with(Array.ConvertAll(values, x => (T)x));
            return true;
        }
    }
}
