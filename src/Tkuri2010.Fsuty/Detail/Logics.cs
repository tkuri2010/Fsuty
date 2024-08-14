using System;
using System.Collections.Generic;
using System.Linq;

namespace Tkuri2010.Fsuty.Detail
{
	public static class Logics
	{
		public static Stack<T> XPushRange<T>(this Stack<T> stack, IEnumerable<T> ts)
		{
			foreach (var item in ts)
			{
				stack.Push(item);
			}
			return stack;
		}


		public static Stack<T> XPopWhile<T>(this Stack<T> stack, Func<T, bool> condition)
		{
			while (stack.Any() && condition(stack.Peek()))
			{
				stack.Pop();
			}

			return stack;
		}


		/// <summary>
		/// (since .NET Standard 2.1)
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="stack"></param>
		/// <param name="outItem"></param>
		/// <returns></returns>
		public static bool XTryPop<T>(this Stack<T> stack, out T? outItem)
		{
			outItem = default;

			if (stack.Any())
			{
				outItem = stack.Pop();
				return true;
			}
			else
			{
				return false;
			}
		}
	}
}
