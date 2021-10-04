using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tkuri2010.Fsuty.Xmp
{
    class Program
    {
        static void Main(string[] args)
        {
			//Xmp1(args);
			//Xmp3().GetAwaiter().GetResult();
			Xmp4().GetAwaiter().GetResult();
			//Text.Std.LinesProcessorXmp1Grep.TryUseMemMapFileViewStream();
			//Try_Path_Combine();
        }

		static void Xmp1(string[] args)
		{
			var inputPath = (1 <= args.Length)
					? args[0]
					: Directory.GetCurrentDirectory();

			var filepath = Filepath.Parse(inputPath);
			Console.WriteLine($"input path = {filepath}");

			Console.WriteLine("=======================================================");
			Console.WriteLine($"     prefix : {filepath.Prefix}");
			Console.WriteLine($"is absolute?: {filepath.IsAbsolute}");

			for (var i = 0; i < filepath.Items.Count; i++)
			{
				Console.WriteLine($"     item {i} : {filepath.Items[i]}");
			}

			Console.WriteLine("=======================================================");
			Console.WriteLine($"  last item : {filepath.LastItem}");
			Console.WriteLine($"    has ext?: {filepath.HasExtension}");
			Console.WriteLine($"  last item without ext: {filepath.LastItemWithoutExtension}");
			Console.WriteLine($"  extension : {filepath.Extension}");
		}


		static async Task Xmp2()
		{
			// you can use CancellationToken so that the app user can request cancel on UI.
			CancellationToken ct = CancellationToken.None;

			await foreach (var e in Fsentry.EnumerateAsync("../.", ct))
			{
				if (e.Event == Fsevent.EnterDir)
				{
					Console.WriteLine($"ENTER: {e.FullPathString}");

					// you can do this:
					// if (ShouldSkip(e.RelativePath))
					// {
					//     Console.WriteLine($"skip visiting into the dir:{e.RelativePath}");
					//     e.Command = FentryCommand.SkipDirectory;
					//     continue;
					// }
				}
				else if (e.Event == Fsevent.File)
				{
					Console.WriteLine($"FILE : {e.FullPathString}");
				}
				else if (e.Event == Fsevent.LeaveDir)
				{
					Console.WriteLine($"LEAVE: {e.FullPathString}");
				}
				else if (e.Event == Fsevent.Error)
				{
					Console.WriteLine($"ERROR: {e.FullPathString}");
				}
			}
		}


		static async Task Xmp3()
		{
			var sw = new System.Diagnostics.Stopwatch();
			var all = new List<Filepath>();
			var ct = CancellationToken.None;

			sw.Start();

			await foreach (var e in Fsentry.EnumerateAsync(@"D:\somewhere", ct))
			{
				if (e.Event == Fsevent.EnterDir || e.Event == Fsevent.File)
				{
					Console.WriteLine(e.RelativePath);
					all.Add(e.RelativePath);
				}
			}

			sw.Stop();

			Console.WriteLine("=============================================================");
			Console.WriteLine("    GetTotalAllocatedBytes       : " + System.GC.GetTotalAllocatedBytes());
			Console.WriteLine("GetAllocatedBytesForCurrentThread: " + System.GC.GetAllocatedBytesForCurrentThread());
			Console.WriteLine("            Elapsed              : " + sw.Elapsed);
		}


		/// <summary>
		/// example usage of the Fsinfo class.
		/// </summary>
		static async Task Xmp4()
		{
			await foreach (var e in Fsinfo.EnumerateAsync(@"C:/Windows/Logs"))
			{
				if (e.WhenError(out var x, out var currentDirInfo))
				{
					Console.WriteLine($"! {x.Message} : {currentDirInfo.FullName}");
				}
				else if (e.WhenEnterDir(out var enterDirInfo))
				{
					Console.WriteLine($">> {enterDirInfo.FullName}");
				}
				else if (e.WhenLeaveDir(out var leaveDirInfo))
				{
					Console.WriteLine($"<< {leaveDirInfo.FullName}");
				}
				else if (e.WhenFile(out var fileInfo))
				{
					Console.WriteLine($"  {fileInfo.FullName}");
				}
			}
		}


#if false
		[DllImport("shlwapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool PathCanonicalize(
				[Out] StringBuilder lpszDest,
				string lpszSrc);

		static void Try_PathCanonicalize(string input)
		{
			var buf = new StringBuilder(256);
			PathCanonicalize(buf, input);
			Console.WriteLine($"|`{input}` | `{buf}` |");
		}

		static void Try_PathCanonicalize()
		{
			Try_PathCanonicalize( @"" );       //=> @"\"
			Try_PathCanonicalize( @"." );      //=> @"\"
			Try_PathCanonicalize( @".." );     //=> @"\"
			Try_PathCanonicalize( @".\" );     //=> @"\"
			Try_PathCanonicalize( @"c:." );    //=> @"c:\"

			Try_PathCanonicalize( @"a.." );    //=> @"a"
			Try_PathCanonicalize( @"..a" );    //=> @"..a"
			Try_PathCanonicalize( @"a..b" );   //=> @"a..b"
			Try_PathCanonicalize( @"./" );     //=> @"./"
			Try_PathCanonicalize( @"./.\." );  //=> @"./"
			Try_PathCanonicalize( @".a.\." );  //=> @".a"
			Try_PathCanonicalize( @".\./." );  //=> @"./"

			Try_PathCanonicalize( @".\..\dir\...\xxx\..\file.txt"); //=> @"dir\...\file.txt"
		}
#endif


#if false
		static void Try_Path_Combine(string path1, string path2)
		{
			var rv = System.IO.Path.Combine(path1, path2);
			Console.WriteLine($"| `{path1}` | `{path2}` | `{rv}` |");
		}

		static void Try_Path_Combine()
		{
			Try_Path_Combine(@"c:/base", @"dir"); // 普通に期待通り、ディレクトリのセパレータを補って連結
			Try_Path_Combine(@"c:/base/", @"dir"); // こちらも期待通り、ディレクトリのセパレータは補わずに連結
			Try_Path_Combine(@"c:\base", @"."); // ディレクトリセパレータは補うが、単純に文字列を連結するだけ
			Try_Path_Combine(@"c:\base", @"..\..\dir1");  // 同じく

			// 以下は path1 が完全になくなり、戻り値としては単純に path2 がそのまま返る
			Try_Path_Combine(@"c:\base", @"/."); // => /.
			Try_Path_Combine(@"c:\base", @"\."); // => \.
			Try_Path_Combine(@"c:\base", @"/dir1");
			Try_Path_Combine(@"c:\base", @"d:dir1");
			Try_Path_Combine(@"c:\base", @"\\?\server\share-name\dir1");
		}
#endif
    }
}
