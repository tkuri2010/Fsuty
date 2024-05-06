using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tkuri2010.Fsuty.Tests.Testfx;


namespace Tkuri2010.Fsuty.Tests
{
	[TestClass]
	public class FsentryTest
	{
		public TestContext TestContext { get; set; } = default!;


		static void RemoveMatched(HashSet<string> set, string fullName)
		{
			fullName = fullName.Replace('\\', '/');
			set.RemoveWhere(it => fullName.EndsWith(it));
		}


		/// <summary>
		/// SPEC: simple usage
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task Spec_SimpleUsage_Async()
		{
			using var stc = new StaticTestContext(TestContext);

			using var fs = await TempFileSystem.NewAsync(
				"file0.txt",
				"dir1",
				"     / file1-1.txt",
				"     / dir1-2",
				"              / file1-2-1.txt"
			);

			int enterDirCount = 0;
			int leaveDirCount = 0;
			int fileCount = 0;

			await foreach (var info in Fsentry.EnumerateAsync(fs.TempDir))
			{
				if (info is Fsentry.EnterDir enterDir)
				{
					// an event that means entering "dir1" or "dir1-2"
					StaticTestContext.WriteLine(">> entering dir: " + enterDir.RelativePath);
					enterDirCount++;
				}
				else if (info is Fsentry.LeaveDir leaveDir)
				{
					// an event that means leaving "dir1" or "dir1-2"
					StaticTestContext.WriteLine("<< leaving dir: " + leaveDir.RelativePath);
					leaveDirCount++;
				}
				else if (info is Fsentry.File file)
				{
					// a file found
					StaticTestContext.WriteLine("   file: " + file.RelativePath);
					fileCount++;
				}
				else if (info is Fsentry.Error x)
				{
					Assert.Fail(x.Exception?.Message ?? "no idea...");
				}
			}

			Assert.AreEqual(2, enterDirCount);
			Assert.AreEqual(2, leaveDirCount);
			Assert.AreEqual(3, fileCount);
		}


		/// <summary>
		/// SPEC: How to use EnterDir.Skip(), and what will be happen.
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task Spec_Skip1_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				"dir1",
				"     / file1-1.txt",
				"     / file1-2.txt"
			);

			await foreach (var info in Fsentry.EnumerateAsync(fs.TempDir))
			{
				if (info is Fsentry.EnterDir enterDir)
				{
					enterDir.Skip(); // ★ skip entering "dir1"
				}
				else if (info is Fsentry.LeaveDir)
					Assert.Fail("LeaveDir events will not occur when you call enterDir.Skip()");
				else if (info is Fsentry.File)
					Assert.Fail("File events will not occur when you call enterDir.Skip()");
				else if (info is Fsentry.Error x)
					Assert.Fail(x.Exception?.Message ?? "no idea...");
			}

			Assert.IsTrue(true);
		}


		/// <summary>
		/// SPEC: How to use info.LeaveParentDir()
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task Spec_LeaveParentDir_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				"dir1",
				"     / file1-1.txt",
				"     / file1-2.txt",
				"     / file1-3.txt",
				"     / file1-4.txt",
				"     / file1-5.txt",
				"     / file1-6.txt"
			);

			int enterDirCount = 0;
			int leaveDirCount = 0;
			int fileCount = 0;

			await foreach (var info in Fsentry.EnumerateAsync(fs.TempDir))
			{
				if (info is Fsentry.EnterDir)
				{
					enterDirCount++;
				}
				else if (info is Fsentry.LeaveDir)
				{
					leaveDirCount++;
				}
				else if (info is Fsentry.File file)
				{
					fileCount++;

					if (fileCount >= 2)
					{
						file.LeaveParentDir();  // ★ will leave dir1
					}
				}
				else if (info is Fsentry.Error x)
					Assert.Fail(x.Exception?.Message ?? "no idea...");
			}

			Assert.AreEqual(1, enterDirCount);
			Assert.AreEqual(1, leaveDirCount);

			Assert.AreEqual(2, fileCount);    // ★
			Assert.AreNotEqual(6, fileCount); // ★
		}


		[TestMethod]
		public async Task Test_Empty_Async()
		{
			using var fs = await TempFileSystem.NewAsync();

			await foreach (var _ in Fsentry.EnumerateAsync(fs.TempDir))
			{
				Assert.Fail("nothing must happen.");
			}

			Assert.IsTrue(true);
		}


		[TestMethod]
		public async Task Test_TopLevelError_Async()
		{
			int errorCount = 0;
			int others = 0;

			await foreach (var e in Fsentry.EnumerateAsync("xxx_does_not_exist_xxx"))
			{
				if (e is Fsentry.Error error)
				{
					errorCount++;
					if (error.Exception is Fsentry.EnumerationException x)
					{
						StaticTestContext.WriteLine("processing dir:" + x.DirPathString);
						//=> "xxx_does_not_exist_xxx"
						StaticTestContext.WriteLine(error.Exception?.InnerException?.Message ?? "no idea..");
						//=> "Could not find a part of the path...."
					}
				}
				else
				{
					others++;
				}
			}

			Assert.AreEqual(1, errorCount);
			Assert.AreEqual(0, others);
		}


		[TestMethod]
		public async Task Test_OnlyDeepDirs_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				"dir1 / dir1-2 / dir1-3",
				"dir2 / dir2-2 / dir2-3"
			);

			int enterDirCount = 0;
			int leaveDirCount = 0;
			int fileCount = 0;
			int errorCount = 0;

			await foreach (var info in Fsentry.EnumerateAsync(fs.TempDir))
			{
				if (info is Fsentry.EnterDir)
				{
					enterDirCount++;
				}
				else if (info is Fsentry.LeaveDir)
				{
					leaveDirCount++;
				}
				else if (info is Fsentry.File)
				{
					fileCount++;
				}
				else if (info is Fsentry.Error)
				{
					errorCount++;
				}
			}

			Assert.AreEqual(6, enterDirCount);
			Assert.AreEqual(6, leaveDirCount);
			Assert.AreEqual(0, fileCount);
			Assert.AreEqual(0, errorCount);
		}


		/// <summary>
		/// skip
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task Test_Skip2_Async()
		{
			// ディレクトリやファイルを実際のファイルシステム上に用意
			using var fs = await TempFileSystem.NewAsync(
				"file0.txt",
				"dir1 / file1-1.txt",
				"       file1-2.txt",
				"dir2",
				"dir3-1 / dir3-2 / file3-1.txt",
				"                / dir3-3 / file3-2.txt"
			);

			var pathSet = new HashSet<string>(fs.GetPropertPaths());

			// テスト実行。見つかったディレクトリやファイルを変数 pathSet から消していく
			// ただし、dir3-1 の中身を無視してみる
			var skipCount = 0;
			int loopCount = 0;
			await foreach (var e in Fsentry.EnumerateAsync(fs.TempDir))
			{
				loopCount++;

				if (e is not Fsentry.ISuccess entry)
				{
					Assert.Fail();
					continue;
				}

				Assert.IsFalse(entry.RelativePath.IsAbsolute);

				var relPathStr = entry.RelativePath.ToString('/');

				if (entry is Fsentry.EnterDir enterDir)
				{
					if (relPathStr.Contains("dir3-1"))
					{
						enterDir.Skip();
						skipCount++;
						continue;
					}

					if (pathSet.Contains(relPathStr))
					{
						pathSet.Remove(relPathStr);
					}
				}
				else if (entry is Fsentry.File)
				{
					Assert.IsTrue(pathSet.Contains(relPathStr));
					pathSet.Remove(relPathStr);
				}
			}

			Assert.IsTrue(loopCount >= 1);

			// 1度だけ skip したはず。"dir3-1" を見つけてすぐ skip したので、中のディレクトリの探索はしていないはず
			Assert.AreEqual(1, skipCount);

			// pathSet は 2 つ残ったはず
			Assert.AreEqual(2, pathSet.Count);
		}


		[TestMethod]
		public async Task Test_LeaveParentDir2_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				"dir1",
				"     / file1-1.txt",
				"dir2",
				"     / dir2-1",
				"              / dir2-1-1",
				"                       / file2-1-1.txt",
				"                       / file2-1-2.txt",
				"              / dir2-1-2",
				"                       / file2-1-3.txt",
				"                       / file2-1-4.txt",
				"     / dir2-2",
				"              / dir2-2-1",
				"              / dir2-2-2",
				"dir3",
				"     / file3-1.txt"
			);

			var pathSet = new HashSet<string>(fs.GetPropertPaths());

			var leaveParentDirCount = 0;

			await foreach (var e in Fsentry.EnumerateAsync(fs.TempDir))
			{
				if (e is Fsentry.EnterDir enterDir)
				{
					if (enterDir.FullPathString.EndsWith("dir2-1-1")
						|| enterDir.FullPathString.EndsWith("dir2-1-2"))
					{
						enterDir.LeaveParentDir();
						leaveParentDirCount++;
					}
					else
					{
						StaticTestContext.WriteLine($"enter dir: {enterDir.RelativePath}");
						RemoveMatched(pathSet, enterDir.FullPathString);
					}
				}
				else if (e is Fsentry.File file)
				{
					StaticTestContext.WriteLine($"file: {file.RelativePath}");
					RemoveMatched(pathSet, file.FullPathString);
				}
				else if (e is Fsentry.Error x)
				{
					Assert.Fail(x.Exception?.Message ?? "no idea");
				}
			}

			// LeaveParentDir() msut be called only once.
			Assert.AreEqual(1, leaveParentDirCount);

			foreach (var p in pathSet)
			{
				StaticTestContext.WriteLine($"remaining: {p}");
			}

			Assert.AreEqual(6, pathSet.Count);
			Assert.IsTrue(pathSet.Any(it => it.EndsWith("dir2-1-1")));
			Assert.IsTrue(pathSet.Any(it => it.EndsWith("file2-1-1.txt")));
			Assert.IsTrue(pathSet.Any(it => it.EndsWith("file2-1-2.txt")));
			Assert.IsTrue(pathSet.Any(it => it.EndsWith("dir2-1-2")));
			Assert.IsTrue(pathSet.Any(it => it.EndsWith("file2-1-3.txt")));
			Assert.IsTrue(pathSet.Any(it => it.EndsWith("file2-1-4.txt")));
		}


		[TestMethod]
		public async Task Test99_ErrorHandling_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				"file0.txt",
				"dir1",
				"     / dir1-1",
				"              / file1-1-0.txt",
				"              / file1-1-1.txt",
				"dir2",
				"     / file2-0.txt"
			);

			var enterDirCount = 0;
			var fileCount = 0;
			var errorCount = 0;

			await foreach (var entry in Fsentry.EnumerateAsync(fs.TempDir))
			{
				if (entry is Fsentry.EnterDir enterDir)
				{
					enterDirCount++;

					if (enterDir.RelativePath.LastItem == "dir1-1")
					{
						// ここで消す。
						fs.Delete(enterDir.RelativePath);
						// この後、Fsentry がこのディレクトリ内のアイテムを列挙しようとしてエラーが発生するはず。
					}
				}
				else if (entry is Fsentry.File)
				{
					fileCount++;
				}
				else if (entry is Fsentry.Error)
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
