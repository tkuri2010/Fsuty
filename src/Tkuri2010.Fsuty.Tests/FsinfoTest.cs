using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tkuri2010.Fsuty.Tests.Testfx;

namespace Tkuri2010.Fsuty.Tests
{
	[TestClass]
	public class FsinfoTest
	{
		protected TestContext TestContext { get; set; } = default!;


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

			await foreach (var info in Fsinfo.EnumerateAsync(fs.TempDir))
			{
				if (info is Fsinfo.EnterDir enterDir)
				{
					// an event that means entering "dir1" or "dir1-2"
					StaticTestContext.WriteLine(">> entering dir: " + enterDir.Info.Name);
					enterDirCount++;
				}
				else if (info is Fsinfo.LeaveDir leaveDir)
				{
					// an event that means leaving "dir1" or "dir1-2"
					StaticTestContext.WriteLine("<< leaving dir: " + leaveDir.Info.Name);
					leaveDirCount++;
				}
				else if (info is Fsinfo.File file)
				{
					// a file found
					StaticTestContext.WriteLine("   file: " + file.Info.Name);
					fileCount++;
				}
				else if (info is Fsinfo.Error x)
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

			await foreach (var info in Fsinfo.EnumerateAsync(fs.TempDir))
			{
				if (info is Fsinfo.EnterDir enterDir)
				{
					enterDir.Skip(); // ★ skip entering "dir1"
				}
				else if (info is Fsinfo.LeaveDir)
					Assert.Fail("LeaveDir events will not occur when you call enterDir.Skip()");
				else if (info is Fsinfo.File)
					Assert.Fail("File events will not occur when you call enterDir.Skip()");
				else if (info is Fsinfo.Error x)
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

			await foreach (var info in Fsinfo.EnumerateAsync(fs.TempDir))
			{
				if (info is Fsinfo.EnterDir)
				{
					enterDirCount++;
				}
				else if (info is Fsinfo.LeaveDir)
				{
					leaveDirCount++;
				}
				else if (info is Fsinfo.File file)
				{
					fileCount++;

					if (fileCount >= 2)
					{
						file.LeaveParentDir();  // ★ will leave dir1
					}
				}
				else if (info is Fsinfo.Error x)
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

			await foreach (var _ in Fsinfo.EnumerateAsync(fs.TempDir))
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

			await foreach (var e in Fsinfo.EnumerateAsync("xxx_does_not_exist_xxx"))
			{
				if (e is Fsinfo.Error error)
				{
					errorCount++;
					if (error.Exception is Fsinfo.EnumerationException x)
					{
						StaticTestContext.WriteLine("processing dir:" + x.CurrentDirectoryInfo.Name);
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

			await foreach (var info in Fsinfo.EnumerateAsync(fs.TempDir))
			{
				if (info is Fsinfo.EnterDir)
				{
					enterDirCount++;
				}
				else if (info is Fsinfo.LeaveDir)
				{
					leaveDirCount++;
				}
				else if (info is Fsinfo.File)
				{
					fileCount++;
				}
				else if (info is Fsinfo.Error)
				{
					errorCount++;
				}
			}

			Assert.AreEqual(6, enterDirCount);
			Assert.AreEqual(6, leaveDirCount);
			Assert.AreEqual(0, fileCount);
			Assert.AreEqual(0, errorCount);
		}


		[TestMethod]
		public async Task Test_Skip2_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				"file0.txt",
				"dir1",
				"     / file1-1.txt",
				"     / dir1-2",
				"             / file1-2-1.txt"
			);

			var pathSet = new HashSet<string>(fs.GetPropertPaths());

			int enterDirCount = 0;
			int leaveDirCount = 0;
			int fileCount = 0;

			await foreach (var info in Fsinfo.EnumerateAsync(fs.TempDir))
			{
				if (info is Fsinfo.EnterDir enterDir)
				{
					enterDirCount++;
					if (enterDir.Info.FullName.EndsWith("dir1"))
					{
						enterDir.Skip(); // ★ skip!
					}
				}
				else if (info is Fsinfo.LeaveDir leaveDir)
				{
					leaveDirCount++;
					RemoveMatched(pathSet, leaveDir.Info.FullName); // 登場したものはpathSetから消していく
				}
				else if (info is Fsinfo.File file)
				{
					fileCount++;
					RemoveMatched(pathSet, file.Info.FullName);
				}
				else if (info is Fsinfo.Error x)
				{
				}
			}

			Assert.AreEqual(1, enterDirCount);
			Assert.AreEqual(0, leaveDirCount); // enter dir をスキップしたので leave もしなかったはず
			Assert.AreEqual(1, fileCount);

			Assert.AreEqual(4, pathSet.Count); // 4つ残ったはず
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

			await foreach (var info in Fsinfo.EnumerateAsync(fs.TempDir))
			{
				if (info is Fsinfo.EnterDir enterDir)
				{
					if (enterDir.Info.Name.EndsWith("dir2-1-1")
						|| enterDir.Info.Name.EndsWith("dir2-1-2"))
					{
						enterDir.LeaveParentDir();
						leaveParentDirCount++;
					}
					else
					{
						RemoveMatched(pathSet, enterDir.Info.FullName);
					}
				}
				else if (info is Fsinfo.File file)
				{
					RemoveMatched(pathSet, file.Info.FullName);
				}
				else if (info is Fsinfo.Error x)
				{
					Assert.Fail(x.Exception?.Message ?? "no idea");
				}
			}

			// LeaveParentDir() msut be called only once.
			Assert.AreEqual(1, leaveParentDirCount);

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

			await foreach (var entry in Fsinfo.EnumerateAsync(fs.TempDir))
			{
				if (entry is Fsinfo.EnterDir enterDir)
				{
					enterDirCount++;

					if (enterDir.Info.Name == "dir1-1")
					{
						// ここで消す。
						fs.Delete(enterDir.Info.FullName);
						// Fsinfo がこのディレクトリ内のアイテムを列挙しようとしてエラーが発生するはず。
					}
				}
				else if (entry is Fsinfo.File)
				{
					fileCount++;
				}
				else if (entry is Fsinfo.Error)
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