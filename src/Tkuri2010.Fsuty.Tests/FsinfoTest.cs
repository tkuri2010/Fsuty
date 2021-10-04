using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Tkuri2010.Fsuty.Tests
{
	[TestClass]
	public class FsinfoTest
	{
		static void RemoveMatched(HashSet<string> set, string fullName)
		{
			fullName = fullName.Replace('\\', '/');
			set.RemoveWhere(it => fullName.EndsWith(it));
		}


		[TestMethod]
		public async Task Test1_Simple_Async()
		{
			var pathSet = new HashSet<string>()
			{
				"file0.txt",
				"dir1",
				"dir1/file1-1.txt",
				"dir1/dir1-2",
				"dir1/dir1-2/file1-2-1.txt",
			};

			using var fs = await Testfx.TempFileSystem.NewAsync(pathSet);

			int enterDirCount = 0;
			int leaveDirCount = 0;
			int fileCount = 0;

			await foreach (var info in Fsinfo.EnumerateAsync(fs.TempDir))
			{
				if (info.WhenEnterDir(out var enterDir))
				{
					enterDirCount++;
				}
				else if (info.WhenLeaveDir(out var leaveDir))
				{
					leaveDirCount++;
					RemoveMatched(pathSet, leaveDir.FullName); // 登場したものはpathSetから消していく
				}
				else if (info.WhenFile(out var file))
				{
					fileCount++;
					RemoveMatched(pathSet, file.FullName);
				}
				else if (info.WhenError(out var x, out var currDir))
				{
				}
			}

			Assert.AreEqual(2, enterDirCount);
			Assert.AreEqual(2, leaveDirCount);
			Assert.AreEqual(3, fileCount);

			Assert.AreEqual(0, pathSet.Count); // 最終的に空になったはず
		}


		[TestMethod]
		public async Task Test1_Simple2_Async()
		{
			var pathSet = new HashSet<string>()
			{
				"file0.txt",
				"dir1",
				"dir1/file1-1.txt",
				"dir1/dir1-2",
				"dir1/dir1-2/file1-2-1.txt",
			};

			using var fs = await Testfx.TempFileSystem.NewAsync(pathSet);

			int enterDirCount = 0;
			int leaveDirCount = 0;
			int fileCount = 0;

			await foreach (var info in Fsinfo.EnumerateAsync(fs.TempDir))
			{
				if (info.WhenEnterDir(out var enterDir))
				{
					enterDirCount++;
					if (enterDir.FullName.EndsWith("dir1"))
					{
						info.Command = Fscommand.SkipDirectory;  // スキップしてみる
					}
				}
				else if (info.WhenLeaveDir(out var leaveDir))
				{
					leaveDirCount++;
					RemoveMatched(pathSet, leaveDir.FullName); // 登場したものはpathSetから消していく
				}
				else if (info.WhenFile(out var file))
				{
					fileCount++;
					RemoveMatched(pathSet, file.FullName);
				}
				else if (info.WhenError(out var x, out var currDir))
				{
				}
			}

			Assert.AreEqual(1, enterDirCount);
			Assert.AreEqual(1, leaveDirCount);
			Assert.AreEqual(1, fileCount);

			Assert.AreEqual(3, pathSet.Count); //  3つ残ったはず
		}
	}
}