using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tkuri2010.Fsuty.Text.Std
{
	/// <summary>
	/// Poor man's grep / An example of class `LargeFileLineProcessor` (namespace `Tkuri2010.Fsuty.Text.Std`)
	/// </summary>
	public class LinesProcessorXmp1Grep
	{
		static readonly string[] Usage =
		{
			"<< poor man's grep >>",
			"usage:",
			"    C:\\here> dotnet run [file-name] [regex-pattern]",
			"example:",
			"    C:\\here> dotnet run  C:/logs/large-text.log  [Aa]bcde"
		};


		public static async Task ExecAsync(string[] args)
		{
			if (args.Length < 2)
			{
				ShowUsage();
				return;
			}

			var file = args[0];
			var pattern = args[1];

			var watch = new System.Diagnostics.Stopwatch();
			watch.Start();

#if false
			ProcessingFunc<string> findingFunction = (info) =>  // info ... LineInfo<string>
			{
				var str =  info.LineBytes.ToString(Encoding.UTF8);

				return Regex.IsMatch(str, pattern)
						? str     // or explicitly `info.Ok(str)`.
						: info.No();
			};

			await foreach (var result in LargeFileLinesProcessor.ProcessAsync(file, findingFunction))
			{
				Put($"Line {result.LineNumber}: " + result.Value.TrimEnd());
			}
#else
			foreach (var line in System.IO.File.ReadAllLines(file))
			{
				if (Regex.IsMatch(line, pattern))
				{
					Put(line);
				}
			}
#endif

			watch.Stop();
			Put("elap: " + watch.Elapsed.TotalMilliseconds);
			Put("line enum elap: " + Lfdetail.BasicLineEnumerator.elaps.TotalMilliseconds);
		}


		static void Put(string str)
		{
			Console.WriteLine(str);
		}


		static void ShowUsage()
		{
			foreach (var l in Usage)
			{
				Put(l);
			}
		}


		#region my laob.

		public static async Task MakeLargeFileAsync()
		{
			using var file = new System.IO.FileStream("r:/large_file.txt", System.IO.FileMode.Create);
			var manyXs = "xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx";
			for (var i = 1; i <= 500000; i++)
			{
				var line = $"xxxxxxxxx {i} {manyXs}{manyXs}{manyXs} ABCDEF abcdef {manyXs}{manyXs}{manyXs}\r\n";
				var bytes = Encoding.UTF8.GetBytes(line);
				await file.WriteAsync(bytes);
			}
		}

		public static void ExecSimpl()
		{
			foreach (var line in System.IO.File.ReadAllLines("r:/large_file.txt"))
			{
				if (Regex.IsMatch(line, "173"))
				{
					Console.WriteLine(line);
				}
			}
		}


		public static void TryUseMemMapFileViewStream()
		{
			using var mmf = System.IO.MemoryMappedFiles.MemoryMappedFile.CreateFromFile("r:/15.txt");
			using var strm1 = mmf.CreateViewStream(0, 3);
			using var strm2 = mmf.CreateViewStream(3, 3);

			int c = 0;

			strm2.Position = 1;
			c = strm2.ReadByte();
			Console.WriteLine($"char = {Char.ConvertFromUtf32(c)}");

			c = strm2.ReadByte();
			Console.WriteLine($"char = {Char.ConvertFromUtf32(c)}");

			strm2.Position = 0;
			c = strm2.ReadByte();
			Console.WriteLine($"char = {Char.ConvertFromUtf32(c)}");
		}

		#endregion
	}
}
