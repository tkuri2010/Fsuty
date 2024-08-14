using System;
using System.Collections.Generic;
using System.Text;


namespace Tkuri2010.Fsuty.Xmp
{
	public class FsentryExample
	{
		/// <summary>
		/// Example - basic usage
		/// </summary>
		public static void Example1()
		{
			Console.OutputEncoding = Encoding.UTF8;

			string indent = "";

			// By default, the order of items enumerated by the Enumerate() method
			// is undefined.
			foreach (var it in Fsentry.Enumerate(@"C:\Windows\Temp"))
			{
				if (it is Fsentry.EnterDir enterDir)
				{
					Console.WriteLine(indent + "📁 " + enterDir.RelativePath.LastItem);
					indent = indent + "  ";
				}
				else if (it is Fsentry.LeaveDir)
				{
					indent = indent.Substring(2);
				}
				else if (it is Fsentry.File file)
				{
					Console.WriteLine(indent + "📄 " + file.RelativePath.LastItem);
				}
				else if (it is Fsentry.Error error)
				{
					Console.WriteLine("🚫[ERROR!] " +( error.Exception?.Message ?? ""));
				}
			}
		}


		/// <summary>
		/// Example - enumeration order.
		/// </summary>
		public static void ExampleOfEnumerationOrder()
		{
			// enumerates files first, followed by directories.
			foreach (var it in Fsentry.Enumerate(@"C:\Windows\Temp", enumMode: Fsentry.FilesThenDirs()))
			{
				// ...
			}
		}


		/// <summary>
		/// Example - search pattern.
		/// </summary>
		public static void ExampleOfSearchPattern()
		{
			// enumerates files first, followed by directories.
			foreach (var it in Fsentry.Enumerate(@"C:\Windows\Temp", enumMode: Fsentry.DirsThenFiles(dirPattern: "*", filePattern: "*.log")))
			{
				if (it is Fsentry.File file)
				{
					Console.WriteLine(file.RelativePath.LastItem);
				}
			}
		}


		/// <summary>
		/// Example - you can skip entering sub directories.
		/// </summary>
		public static void ExampleOfSkipEnteringDirectory()
		{
			Console.OutputEncoding = Encoding.UTF8;

			foreach (var it in Fsentry.Enumerate(@"C:\Windows\Temp"))
			{
				if (it is Fsentry.EnterDir enterDir)
				{
					Console.WriteLine("📁 " + enterDir.RelativePath.LastItem);

					enterDir.Skip();  // yay! ★
				}
				else if (it is Fsentry.File file)
				{
					Console.WriteLine("📄 " + file.RelativePath.LastItem);
				}
			}
		}


		/// <summary>
		/// Benchmarking :-)
		/// </summary>
		public static void Benchmark()
		{
			var sw = new System.Diagnostics.Stopwatch();
			var enterDirs = new List<Filepath>();
			var files = new List<Filepath>();

			sw.Start();

			var path = @"D:\somewhere";

			foreach (var e in Fsentry.Enumerate(path))
			{
				if (e is Fsentry.EnterDir enterDir)
				{
					Console.WriteLine(enterDir.RelativePath);
					enterDirs.Add(enterDir.RelativePath);
				}
				else if (e is Fsentry.File file)
				{
					Console.WriteLine(file.RelativePath);
					files.Add(file.RelativePath);
				}
				else if (e is Fsentry.Error error)
				{
					Console.WriteLine("error! " + error.Exception?.Message);
					break;
				}
			}
			Console.WriteLine("new");

			sw.Stop();

			Console.WriteLine( "=============================================================");
			Console.WriteLine( "    GetTotalAllocatedBytes       : " + GC.GetTotalAllocatedBytes());
			Console.WriteLine( "GetAllocatedBytesForCurrentThread: " + GC.GetAllocatedBytesForCurrentThread());
			Console.WriteLine( "            Elapsed              : " + sw.Elapsed);
			Console.WriteLine($"Dirs:{enterDirs.Count}, files:{files.Count}");
		}
	}
}