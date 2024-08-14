using System;
using System.IO;

namespace Tkuri2010.Fsuty.Xmp
{
	public class FilepathExapmle
	{
		public static void Example1(string[] args)
		{
			var inputPath = (1 <= args.Length)
					? args[0]
					: Directory.GetCurrentDirectory();

			var filepath = Filepath.Parse(inputPath);
			Console.WriteLine($"input path = {filepath}");

			Console.WriteLine("=======================================================");
			Console.WriteLine($"      prefix : {filepath.Prefix}");
			Console.WriteLine($"is from root?: {filepath.IsFromRoot}");

			for (var i = 0; i < filepath.Items.Count; i++)
			{
				Console.WriteLine($"    item {i} : {filepath.Items[i]}");
			}

			Console.WriteLine("=======================================================");
			Console.WriteLine($"   last item : {filepath.LastItem}");
			Console.WriteLine($"     has ext?: {filepath.HasExtension}");
			Console.WriteLine($"last item without ext: {filepath.LastItemWithoutExtension}");
			Console.WriteLine($"   extension : {filepath.Extension}");
		}
	}
}
