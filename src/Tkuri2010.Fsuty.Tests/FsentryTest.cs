using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tkuri2010.Fsuty.Tests
{
	[TestClass]
	public class FsentryTest
	{

		static PathItems _AsItems(string relPath) => Filepath.Parse(relPath).Items;


		private Filepath GetTempPath()
		{
			var systemTemp = Filepath.Parse(System.IO.Path.GetTempPath());
			return systemTemp.Combine(_AsItems("FsentryTestDir_" + DateTime.Now.ToString("yyyyMMdd-HHmmss")));
		}


		private async Task SetupPathAsync(Filepath absPath)
		{
			if (absPath.HasExtension)
			{
				System.IO.Directory.CreateDirectory(absPath.Parent.ToString());
				await System.IO.File.WriteAllTextAsync(absPath.ToString(), "some file.", System.Text.Encoding.UTF8);
			}
			else
			{
				System.IO.Directory.CreateDirectory(absPath.ToString());
			}
		}


		[TestMethod]
		public async Task Test1Async()
		{
			// ディレクトリやファイルを実際のファイルシステム上に用意
			var temp = GetTempPath();
			var paths = new []
			{
				"file0.txt",
				"dir1/file1-1.txt",
				"dir1/file1-2.txt",
				"dir2",
				"dir3-1/dir3-2/file3-1.txt",
				"dir3-1/dir3-2/dir3-3/file3-2.txt",
			};
			var pathSet = new HashSet<string>();
			foreach (var path in paths)
			{
				pathSet.Add(path);
				await SetupPathAsync(temp.Combine(_AsItems(path)));
			}

			// テスト実行。見つかったディレクトリやファイルを変数 pathSet から消していく
			await foreach (var entry in Fsentry.VisitAsync(temp))
			{
				var fileName = System.IO.Path.GetFileName(entry.FullPathString);
				var relPath = entry.RelativeParent.Combine(_AsItems(fileName)).ToString("/");
				if (entry.Event == FsentryEvent.EnterDir)
				{
					if (pathSet.Contains(relPath))
					{
						pathSet.Remove(relPath);
					}
				}
				else if (entry.Event == FsentryEvent.File)
				{
					Assert.IsTrue(pathSet.Contains(relPath));
					pathSet.Remove(relPath);
				}
			}

			// pathSet は全て無くなったはず
			Assert.AreEqual(0, pathSet.Count);
		}
	}
}
