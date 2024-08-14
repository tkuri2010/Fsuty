using System;

namespace Tkuri2010.Fsuty.Xmp
{
	public static class FsinfoExample
	{
		public static void Example1()
		{
			string indent = "";
			foreach (var e in Fsinfo.Enumerate(@"C:/Windows/Logs"))
			{
				if (e is Fsinfo.Error error)
				{
					Console.WriteLine($"! {error.Exception?.InnerException?.Message ?? "?"} : {error.ParentInfo}");
				}
				else if (e is Fsinfo.EnterDir enterDir)
				{
					Console.WriteLine($"{indent}>> {enterDir.Info.FullName}");
					indent = indent + "   ";
				}
				else if (e is Fsinfo.LeaveDir leaveDir)
				{
					indent = indent.Substring(3);
					Console.WriteLine($"{indent}<< {leaveDir.Info.FullName}");
				}
				else if (e is Fsinfo.File file)
				{
					Console.WriteLine($"{indent}{file.Info.FullName}");
				}
			}
		}


		public static void Example_SearchPattern()
		{
			foreach (var e in Fsinfo.Enumerate(@"C:/Windows/Logs", Fsinfo.NaturalOrder("*.dat")))
			{
				if (e is Fsinfo.File file)
				{
					Console.WriteLine(file.Info.Name);
				}
			}
		}
	}
}
