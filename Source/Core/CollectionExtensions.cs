﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Cone.Core
{
	public delegate bool TryConvert<TInput, TOutput>(TInput value, out TOutput result);

    public static class CollectionExtensions
    {
		public static bool Any<T>(this T[] self, Predicate<T> predicate) {
			for(var i = 0; i != self.Length; ++i)
				if(predicate(self[i]))
					return true;
			return false;
		}

		public static T Last<T>(this T[] self) {
			return self[self.Length - 1];
		}

        public static TOutput[] ConvertAll<TInput,TOutput>(this IList<TInput> self, Converter<TInput, TOutput> converter) {
            var result = new TOutput[self.Count];
            for(var i = 0; i != result.Length; ++i)
                result[i] = converter(self[i]);
            return result;
        }

        public static TOutput[] ConvertAll<TInput, TOutput>(this TInput[] self, Converter<TInput, TOutput> converter) {
            return Array.ConvertAll(self, converter);
        }

		public static bool IsEmpty<T>(this ICollection<T> self) {
			return self.Count == 0;
		}

        public static void EachWhere<T>(this T[] self, Func<T, bool> predicate, Action<T> @do) {
            for(var i = 0; i != self.Length; ++i) {
                var x = self[i];
                if(predicate(x))
                    @do(x);
            }   
        }

        public static void EachWhere<T>(this IEnumerable<T> self, Func<T, bool> predicate, Action<T> @do) {
            foreach (var item in self)
                if (predicate(item))
                    @do(item);
        }

        public static void BackwardsEach<T>(this IList<T> self, Action<T> @do) {
            for(var i = self.Count; --i != -1 ;)
                @do(self[i]);
        }

        public static void Each<T>(this T[] self, Action<T> @do) {
            for (var i = 0; i != self.Length; ++i)
                @do(self[i]);
        }

        public static void Each<T>(this IEnumerable<T> self, Action<T> @do) {
            foreach(var item in self)
                @do(item);
        }

        public static void Each<T>(this IEnumerable<T> self, Action<int, T> @do) {
            using(var items = self.GetEnumerator())
                for(var i = 0; items.MoveNext(); ++i)
                    @do(i, items.Current);
        }

        public static int IndexOf<T>(this IEnumerable<T> self, T value) {
            var index = 0;
            foreach(var item in self) {
                if(item.Equals(value))
                    return index; 
                ++index;
            }
            return -1;
        }

		public static IEnumerable<TOutput> Choose<TInput, TOutput>(this IEnumerable<TInput> self, TryConvert<TInput, TOutput> transform) {
			TOutput value;
			foreach(var item in self)
				if(transform(item, out value))
					yield return value;			
		}

		public static IEnumerable<T> Flatten<T>(this IEnumerable<T> self, Func<T, IEnumerable<T>> children) {
			return self.SelectMany(x => Flatten(x, children));
		}

		static IEnumerable<T> Flatten<T>(T parent, Func<T, IEnumerable<T>> children) {
			yield return parent;
			foreach(var item in children(parent).Flatten(children))
				yield return item;
		}
    }
}
