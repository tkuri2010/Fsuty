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
			var pattern = new Regex(args[1]);

			ProcessingFunc<string> findingFunction = (info) =>  // info ... LineInfo<string>
			{
				var str =  info.LineBytes.ToString(Encoding.UTF8);

				return Regex.IsMatch(str, args[1])
						? str
						: info.No();
			};

			using var processor = new LargeFileLinesProcessor<string>(file, findingFunction);
			await foreach (var result in processor.ProcessAsync())
			{
				Put($"Line {result.LineNumber}: " + result.Value.TrimEnd());
			}
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
			for (var i = 1; i <= 100000; i++)
			{
				var line = $"xxxxxxxxx {i} xxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxxx\r\n";
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

		#endregion
	}
}
