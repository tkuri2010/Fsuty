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

			Func<LineInfo, Result<string>> findingFunction = info =>
			{
				var str =  Encoding.UTF8.GetString(info.LineBytes.Body, 0, info.LineBytes.Count);

				if (pattern.Match(str).Success)
					return Result<string>.Ok(str);
				else
					return Result<string>.No();
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