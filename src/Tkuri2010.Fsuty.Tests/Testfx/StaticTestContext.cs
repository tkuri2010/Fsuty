using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tkuri2010.Fsuty.Tests.Testfx
{
	public class StaticTestContext : IDisposable
	{
		private static object m_locker = new object();


		public static TestContext? TestContext { get; private set; } = null;


		public static void IfAvailable(Action<TestContext> action)
		{
			var tc = TestContext;
			if (tc is not null)
			{
				action(tc);
			}
		}


		public static void WriteLine(string line)
		{
			IfAvailable(tc => tc.WriteLine(line));
			System.Diagnostics.Debug.WriteLine(line);
		}


		public static void WriteLine(string format, params object?[] args)
		{
			IfAvailable(tc => tc.WriteLine(format, args));
			System.Diagnostics.Debug.WriteLine(format, args);
		}


		public StaticTestContext(TestContext? testContext)
		{
			lock (m_locker)
			{
				if (TestContext is not null)
				{
					throw new Exception("TestContext: already initialized.");
				}

				TestContext = testContext;
			}
		}


		public void Dispose()
		{
			lock (m_locker)
			{
				TestContext = null;
			}
		}
	}
}