using System;
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
			Xmp1(args);
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
			Console.WriteLine($"is absolute?: {filepath.Absolute}");

			for (var i = 0; i < filepath.Items.Length; i++)
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

			await foreach (var e in Fsentry.VisitAsync("../.", ct))
			{
				if (e.Event == FsentryEvent.EnterDir)
				{
					Console.WriteLine($"ENTER: {e.Path}");

					// you can do this:
					// if (ShouldSkip(e.Path))
					// {
					//     Console.WriteLine($"skip visiting into the dir:{e.RelativePath}");
					//     e.Command = FentryCommand.SkipDirectory;
					//     continue;
					// }
				}
				else if (e.Event == FsentryEvent.File)
				{
					Console.WriteLine($"FILE : {e.Path}");
				}
				else if (e.Event == FsentryEvent.LeaveDir)
				{
					Console.WriteLine($"LEAVE: {e.Path}");
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
			Console.WriteLine(buf);
		}

		static void Try_PathCanonicalize()
		{
			//Try_PathCanonicalize( @".\..\dir\...\xxx\..\file.txt"); //=> @"dir\...\file.txt"
			//Try_PathCanonicalize( @"" );       //=> @"\"
			//Try_PathCanonicalize( @"." );      //=> @"\"
			//Try_PathCanonicalize( @".." );     //=> @"\"
			//Try_PathCanonicalize( @"./.\." );  //=> @"./"
			  Try_PathCanonicalize( @".a.\." );  //=> @".a"
			//Try_PathCanonicalize( @".\./." );  //=> @"./"
			//Try_PathCanonicalize( @"./" );     //=> @"./"
			//Try_PathCanonicalize( @".\" );     //=> @"\"
			//Try_PathCanonicalize( @"c:." );    //=> @"c:\"
			//Try_PathCanonicalize( @"a.." );    //=> @"a"
			//Try_PathCanonicalize( @"..a" );    //=> @"..a"
			//Try_PathCanonicalize( @"a..b" );   //=> @"a..b"
		}
#endif

    }
}
