using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tkuri2010.Fsuty.Tests
{
	[TestClass]
	public class FsentryTest
	{
		// static PathItems _AsItems(string relPath) => Filepath.Parse(relPath).Items;

		[TestMethod]
		public async Task Test1Async()
		{
			var pathSet = new HashSet<string>()
			{
				"file0.txt",
				"dir1/file1-1.txt",
				"dir1/file1-2.txt",
				"dir2",
				"dir3-1/dir3-2/file3-1.txt",
				"dir3-1/dir3-2/dir3-3/file3-2.txt",
			};

			// ディレクトリやファイルを実際のファイルシステム上に用意
			using var fs = await Testfx.TempFileSystem.NewAsync(pathSet);

			// テスト実行。見つかったディレクトリやファイルを変数 pathSet から消していく
			await foreach (var entry in Fsentry.EnumerateAsync(fs.TempDir))
			{
				Assert.IsFalse(entry.RelativePath.IsAbsolute);

				var relPathStr = entry.RelativePath.ToString('/');

				if (entry.Event == Fsevent.EnterDir)
				{
					if (pathSet.Contains(relPathStr))
					{
						pathSet.Remove(relPathStr);
					}
				}
				else if (entry.Event == Fsevent.File)
				{
					Assert.IsTrue(pathSet.Contains(relPathStr));
					pathSet.Remove(relPathStr);
				}
			}

			// pathSet は全て無くなったはず
			Assert.AreEqual(0, pathSet.Count);
		}


		[TestMethod]
		public async Task Test2Async()
		{
			var pathSet = new HashSet<string>()
			{
				"file0.txt",
				"dir1/file1-1.txt",
				"dir1/file1-2.txt",
				"dir2",
				"dir3-1/dir3-2/file3-1.txt",
				"dir3-1/dir3-2/dir3-3/file3-2.txt",
			};

			// ディレクトリやファイルを実際のファイルシステム上に用意
			using var fs = await Testfx.TempFileSystem.NewAsync(pathSet);

			// テスト実行。見つかったディレクトリやファイルを変数 pathSet から消していく
			// ただし、dir3-1 の中身を無視してみる
			var skipCount = 0;
			await foreach (var entry in Fsentry.EnumerateAsync(fs.TempDir))
			{
				Assert.IsFalse(entry.RelativePath.IsAbsolute);

				var relPathStr = entry.RelativePath.ToString('/');

				if (entry.Event == Fsevent.EnterDir)
				{
					if (relPathStr.Contains("dir3-1"))
					{
						entry.Command = Fscommand.SkipDirectory;
						skipCount++;
						continue;
					}

					if (pathSet.Contains(relPathStr))
					{
						pathSet.Remove(relPathStr);
					}
				}
				else if (entry.Event == Fsevent.File)
				{
					Assert.IsTrue(pathSet.Contains(relPathStr));
					pathSet.Remove(relPathStr);
				}
			}

			// 1度だけ skip したはず。"dir3-1" を見つけてすぐ skip したので、中のディレクトリの探索はしていないはず
			Assert.AreEqual(1, skipCount);

			// pathSet は 2 つ残ったはず
			Assert.AreEqual(2, pathSet.Count);
		}


		[TestMethod]
		public async Task Test99_ErrorHandling_Async()
		{
			var pathSet = new HashSet<string>()
			{
				"file0.txt",
				"dir1",
				"dir1/dir1-1",
				"dir1/dir1-1/file1-1-0.txt",
				"dir1/dir1-1/file1-1-1.txt",
				"dir2",
				"dir2/file2-0.txt",
			};

			using var fs = await Testfx.TempFileSystem.NewAsync(pathSet);

			var enterDirCount = 0;
			var fileCount = 0;
			var errorCount = 0;

			await foreach (var entry in Fsentry.EnumerateAsync(fs.TempDir))
			{
				if (entry.Event == Fsevent.EnterDir)
				{
					enterDirCount++;

					if (entry.RelativePath.LastItem == "dir1-1")
					{
						// これを消してみる。ディレクトリ内のアイテムを列挙しようとしてエラーが発生するはず
						fs.Delete(entry.RelativePath);
					}
				}
				else if (entry.Event == Fsevent.File)
				{
					fileCount++;
				}
				else if (entry.Event == Fsevent.Error)
				{
					errorCount++;
				}
			}

			Assert.AreEqual(3, enterDirCount); // "dir1", "dir1/dir1-1", "dir2"
			Assert.AreEqual(2, fileCount);  // "file0.txt", "file2-0.txt"
			Assert.AreEqual(1, errorCount);
		}
	}
}
