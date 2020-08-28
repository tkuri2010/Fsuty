using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Tkuri2010.Fsuty.Text.Std
{
	/// <summary>
	/// example of class `LargeFileLineProcessor` (namespace `Tkuri2010.Fsuty.Text.Std`)
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


		public static async Task Exec(string[] args)
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

				if (pattern.Match(str).Success)
					return info.Ok(str);
				else
					return info.No();
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
	}
}
