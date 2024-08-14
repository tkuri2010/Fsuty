using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tkuri2010.Fsuty.Tests.Testfx;


namespace Tkuri2010.Fsuty.Tests
{
	[TestClass]
	public class FsentryTest
	{
		public TestContext? TestContext { get; set; } = default;


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
			using var fs = await TempFileSystem.NewAsync(
				TestContext,
				"file0.txt",
				"dir1",
				"     / file1-1.txt",
				"     / dir1-2",
				"              / file1-2-1.txt"
			);

			int enterDirCount = 0;
			int leaveDirCount = 0;
			int fileCount = 0;

			foreach (var info in Fsentry.Enumerate(fs.TempDir))
			{
				if (info is Fsentry.EnterDir enterDir)
				{
					// an event that means entering "dir1" or "dir1-2"
					TestContext?.WriteLine(">> entering dir: " + enterDir.RelativePath);
					enterDirCount++;
				}
				else if (info is Fsentry.LeaveDir leaveDir)
				{
					// an event that means leaving "dir1" or "dir1-2"
					TestContext?.WriteLine("<< leaving dir: " + leaveDir.RelativePath);
					leaveDirCount++;
				}
				else if (info is Fsentry.File file)
				{
					// a file found
					TestContext?.WriteLine("   file: " + file.RelativePath);
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


		[TestMethod]
		public async Task Spec_EnumMode_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				TestContext,
				"test-dir-1",
				"test-file-1.txt",
				"test-dir-2",
				"test-file-2.txt",
				"test-dir-3",
				"test-file-3.txt"
			);

			#region enomMode = FilesThenDirs
			{
				var paths = new List<string>();

				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.FilesThenDirs()))
				{
					if (e is Fsentry.EnterDir enterDir)
					{
						paths.Add(enterDir.FullPathString);
					}
					else if (e is Fsentry.File file)
					{
						paths.Add(file.FullPathString);
					}
				}

				Assert.IsTrue(paths[0].Contains("test-file-"));
				Assert.IsTrue(paths[1].Contains("test-file-"));
				Assert.IsTrue(paths[2].Contains("test-file-"));
				Assert.IsTrue(paths[3].Contains("test-dir-"));
				Assert.IsTrue(paths[4].Contains("test-dir-"));
				Assert.IsTrue(paths[5].Contains("test-dir-"));
			}
			#endregion

			#region enumMode = DirsThenFiles
			{
				var paths = new List<string>();

				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.DirsThenFiles()))
				{
					if (e is Fsentry.EnterDir enterDir)
					{
						paths.Add(enterDir.FullPathString);
					}
					else if (e is Fsentry.File file)
					{
						paths.Add(file.FullPathString);
					}
				}

				Assert.IsTrue(paths[0].Contains("test-dir-"));
				Assert.IsTrue(paths[1].Contains("test-dir-"));
				Assert.IsTrue(paths[2].Contains("test-dir-"));
				Assert.IsTrue(paths[3].Contains("test-file-"));
				Assert.IsTrue(paths[4].Contains("test-file-"));
				Assert.IsTrue(paths[5].Contains("test-file-"));
			}
			#endregion
		}


		/// <summary>
		/// SPEC: How to specify search pattern, and enumerates in natural order / files-then-dirs / dirs-then-files
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task Spec_SearchPattern_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				TestContext,
				"yay_dir1",
				"         / file1-1.txt",
				"         / file1-2.txt",
				"         / yay_file1-3.txt",
				"dir2",
				"    / yay_file2-1.txt",
				"    / yay_file2-2.txt",
				"yay_dir3",
				"         / yay_file3-1.txt",
				"         / yay_file3-2.txt"
			);

			#region enumerates only files and dirs named such as "yay_*"
			{
				var paths = new List<string>();

				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.NaturalOrder(searchPattern: "yay_*")))
				{
					if (e is Fsentry.EnterDir dir)
						paths.Add(dir.RelativePath.LastItem);
					else if (e is Fsentry.File file)
						paths.Add(file.RelativePath.LastItem);
				}

				Assert.AreEqual(paths.Count, 5);
				Assert.IsTrue(paths.All(it => it.StartsWith("yay_")));
			}
			#endregion

			#region enumerates all dirs, and only files named such as "yay_*"
			{
				var paths = new List<string>();

				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.DirsThenFiles(dirPattern: null, filePattern: "yay_*")))
				{
					TestContext?.WriteLine(((Fsentry.ISuccess)e).FullPathString);

					if (e is Fsentry.EnterDir dir)
						paths.Add(dir.RelativePath.LastItem);
					else if (e is Fsentry.File file)
						paths.Add(file.RelativePath.LastItem);
				}

				Assert.AreEqual(paths.Count, 8);
				Assert.IsTrue(paths.All(it => (it == "dir2") || it.StartsWith("yay_")));
			}
			#endregion

			#region `Fsentry.FilesThenDirs()` enumerates files at first, and then dirs in each directory.
			{
				var paths = new List<string>();

				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.FilesThenDirs(dirPattern: null, filePattern: "yay_*")))
				{
					TestContext?.WriteLine(((Fsentry.ISuccess)e).FullPathString);

					if (e is Fsentry.EnterDir dir)
						paths.Add(dir.RelativePath.LastItem);
					else if (e is Fsentry.File file)
						paths.Add(file.RelativePath.LastItem);
				}

				Assert.AreEqual(paths.Count, 8);
				Assert.IsTrue(paths.All(it => (it == "dir2") || it.StartsWith("yay_")));
			}
			#endregion
		}


		/// <summary>
		/// SPEC: How to use EnterDir.Skip(), and what will be happen.
		/// </summary>
		/// <returns></returns>
		[TestMethod]
		public async Task Spec_Skip1_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				TestContext,
				"dir1",
				"     / file1-1.txt",
				"     / file1-2.txt"
			);

			foreach (var info in Fsentry.Enumerate(fs.TempDir))
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
				TestContext,
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

			foreach (var info in Fsentry.Enumerate(fs.TempDir))
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
			using var fs = await TempFileSystem.NewAsync(TestContext);

			foreach (var _ in Fsentry.Enumerate(fs.TempDir))
			{
				Assert.Fail("nothing must happen.");
			}

			Assert.IsTrue(true);
		}


		[TestMethod]
		public void Test_TopLevelError()
		{
			int errorCount = 0;
			int others = 0;

			foreach (var e in Fsentry.Enumerate("xxx_does_not_exist_xxx"))
			{
				if (e is Fsentry.Error error)
				{
					errorCount++;
					TestContext?.WriteLine("processing dir:" + error.DirPathString);
					//=> "xxx_does_not_exist_xxx"
					TestContext?.WriteLine(error.Exception?.Message ?? "no idea..");
					//=> "Could not find a part of the path...."
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
				TestContext,
				"dir1 / dir1-2 / dir1-3",
				"dir2 / dir2-2 / dir2-3"
			);

			int enterDirCount = 0;
			int leaveDirCount = 0;
			int fileCount = 0;
			int errorCount = 0;

			foreach (var info in Fsentry.Enumerate(fs.TempDir))
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
				TestContext,
				"file0.txt",
				"dir1 / file1-1.txt",
				"       file1-2.txt",
				"dir2",
				"dir3-1 / dir3-2 / file3-1.txt",
				"                / dir3-3 / file3-2.txt"
			);

			var pathSet = new HashSet<string>(fs.GetProperPaths());

			// テスト実行。見つかったディレクトリやファイルを変数 pathSet から消していく
			// ただし、dir3-1 の中身を無視してみる
			var skipCount = 0;
			int loopCount = 0;
			foreach (var e in Fsentry.Enumerate(fs.TempDir))
			{
				loopCount++;

				if (e is not Fsentry.ISuccess entry)
				{
					Assert.Fail();
					continue;
				}

				Assert.IsFalse(entry.RelativePath.IsFromRoot);

				var relPathStr = entry.RelativePath.ToString('/');

				if (entry is Fsentry.EnterDir enterDir)
				{
					if (relPathStr.StartsWith("dir3-"))
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
				TestContext,
				"dir1",
				"     / file1-1.txt",
				"dir2",
				"     / dir2-1",
				"              / dir2-1-1",
				"                         / file2-1-1.txt",
				"                         / file2-1-2.txt",
				"              / dir2-1-2",
				"                         / file2-1-3.txt",
				"                         / file2-1-4.txt",
				"     / dir2-2",
				"              / dir2-2-1",
				"              / dir2-2-2",
				"dir3",
				"     / file3-1.txt"
			);

			var pathSet = new HashSet<string>(fs.GetProperPaths());

			var leaveParentDirCount = 0;

			foreach (var e in Fsentry.Enumerate(fs.TempDir))
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
						RemoveMatched(pathSet, enterDir.FullPathString);
					}
				}
				else if (e is Fsentry.File file)
				{
					RemoveMatched(pathSet, file.FullPathString);
				}
				else if (e is Fsentry.Error x)
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
		public async Task Test_EnumOrder_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				TestContext,
				"dir1/",
				"file2.txt",
				"dir3/",
				"file4.txt"
			);

			// dirs then files
			{
				var paths = new List<string>();

				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.DirsThenFiles()))
				{
					if (e is Fsentry.EnterDir enterDir)
					{
						paths.Add(enterDir.RelativePath.LastItem);
					}
					else if (e is Fsentry.File file)
					{
						paths.Add(file.RelativePath.LastItem);
					}
				}

				Assert.IsTrue(paths[0].StartsWith("dir"));
				Assert.IsTrue(paths[1].StartsWith("dir"));
				Assert.IsTrue(paths[2].StartsWith("file"));
				Assert.IsTrue(paths[3].StartsWith("file"));
			}

			// files then dirs
			{
				var paths = new List<string>();

				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.FilesThenDirs()))
				{
					if (e is Fsentry.EnterDir enterDir)
					{
						paths.Add(enterDir.RelativePath.LastItem);
					}
					else if (e is Fsentry.File file)
					{
						paths.Add(file.RelativePath.LastItem);
					}
				}

				Assert.IsTrue(paths[0].StartsWith("file"));
				Assert.IsTrue(paths[1].StartsWith("file"));
				Assert.IsTrue(paths[2].StartsWith("dir"));
				Assert.IsTrue(paths[3].StartsWith("dir"));
			}
		}


		[TestMethod]
		public async Task Test_EnumOrder_2_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				TestContext,
				"dir1/",
				"     dir1-1/",
				"     file1-2.txt",
				"     dir1-3/",
				"     file1-4.txt"
			);

			// dirs then files
			{
				var paths = new List<string>();

				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.DirsThenFiles()))
				{
					if (e is Fsentry.EnterDir enterDir)
					{
						if (enterDir.RelativePath.LastItem == "dir1")
						{
							// ignore
						}
						else
						{
							paths.Add(enterDir.RelativePath.LastItem);
						}
					}
					else if (e is Fsentry.File file)
					{
						paths.Add(file.RelativePath.LastItem);
					}
				}

				Assert.IsTrue(paths[0].StartsWith("dir"));
				Assert.IsTrue(paths[1].StartsWith("dir"));
				Assert.IsTrue(paths[2].StartsWith("file"));
				Assert.IsTrue(paths[3].StartsWith("file"));
			}

			// files then dirs
			{
				var paths = new List<string>();

				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.FilesThenDirs()))
				{
					if (e is Fsentry.EnterDir enterDir)
					{
						if (enterDir.RelativePath.LastItem == "dir1")
						{
							// ignore
						}
						else
						{
							paths.Add(enterDir.RelativePath.LastItem);
						}
					}
					else if (e is Fsentry.File file)
					{
						paths.Add(file.RelativePath.LastItem);
					}
				}

				Assert.IsTrue(paths[0].StartsWith("file"));
				Assert.IsTrue(paths[1].StartsWith("file"));
				Assert.IsTrue(paths[2].StartsWith("dir"));
				Assert.IsTrue(paths[3].StartsWith("dir"));
			}
		}


		[TestMethod]
		public async Task Test_SearchPattern1_Async()
		{
			var dirsFiles = new string[]
			{
				"yay_dir1 /",
				"          yay_dir1-1 /",
				"          boo_dir1-2 /",
				"          yay_file1-1.txt",
				"          boo_file1-2.txt",
				"boo_dir2 /",
				"          yay_dir2-1 /",
				"          yay_file2-1.txt",
				"yay_file3.txt",
				"boo_file4.txt",
			};
			using var fs = await TempFileSystem.NewAsync(TestContext, dirsFiles);

			var paths = new List<string>();
			Action<Fsentry.IEntry> add = (entry) =>
			{
				if (entry is Fsentry.EnterDir enterDir)
				{
					paths.Add(enterDir.RelativePath.LastItem);
				}
				else if (entry is Fsentry.File file)
				{
					paths.Add(file.RelativePath.LastItem);
				}
			};

			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.NaturalOrder(null)))
				{
					add(e);
				}

				Assert.AreEqual(paths.Count, dirsFiles.Length);
			}

			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.NaturalOrder("yay_*")))
				{
					add(e);
				}

				Assert.IsTrue(paths.All(it => it.StartsWith("yay_")));
				Assert.IsTrue(paths.Any(it => it.Contains("dir")));
				Assert.IsTrue(paths.Any(it => it.Contains("file")));

				// boo_dir2 must be ignored...
				Assert.IsTrue(paths.All(it => it != "yay_dir2-1"));
				Assert.IsTrue(paths.All(it => it != "yay_file2-1.txt"));
			}

			#region DirsThenFiles
			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.DirsThenFiles(null)))
				{
					add(e);
				}

				Assert.AreEqual(paths.Count, dirsFiles.Length);
			}

			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.DirsThenFiles("yay_*")))
				{
					add(e);
				}

				Assert.IsTrue(paths.All(it => it.StartsWith("yay_")));
				Assert.IsTrue(paths.Any(it => it.Contains("dir")));
				Assert.IsTrue(paths.Any(it => it.Contains("file")));

				// boo_dir2 must be ignored...
				Assert.IsTrue(paths.All(it => it != "yay_dir2-1"));
				Assert.IsTrue(paths.All(it => it != "yay_file2-1.txt"));
			}

			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.DirsThenFiles(dirPattern: "yay_*")))
				{
					add(e);
				}

				Assert.IsTrue(paths
						.Where(it => it.Contains("dir"))
						.All(it => it.StartsWith("yay_")));
				Assert.IsTrue(paths.Any(it => it.StartsWith("yay_file")));
				Assert.IsTrue(paths.Any(it => it.StartsWith("boo_file")));
			}

			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.DirsThenFiles(filePattern: "yay_*")))
				{
					add(e);
				}

				Assert.IsTrue(paths.Any(it => it.StartsWith("yay_dir")));
				Assert.IsTrue(paths.Any(it => it.StartsWith("boo_dir")));
				Assert.IsTrue(paths
						.Where(it => it.Contains("file"))
						.All(it => it.StartsWith("yay_")));
			}

			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.DirsThenFiles(dirPattern: "yay_*", filePattern: "boo_*")))
				{
					add(e);
				}

				Assert.IsTrue(paths
						.Where(it => it.Contains("dir"))
						.All(it => it.StartsWith("yay_")));
				Assert.IsTrue(paths
						.Where(it => it.Contains("file"))
						.All(it => it.StartsWith("boo_")));
			}
			#endregion


			#region FilesThenDirs
			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.FilesThenDirs(null)))
				{
					add(e);
				}

				Assert.AreEqual(paths.Count, dirsFiles.Length);
			}

			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.FilesThenDirs("yay_*")))
				{
					add(e);
				}

				Assert.IsTrue(paths.All(it => it.StartsWith("yay_")));
				Assert.IsTrue(paths.Any(it => it.Contains("dir")));
				Assert.IsTrue(paths.Any(it => it.Contains("file")));

				// boo_dir2 must be ignored...
				Assert.IsTrue(paths.All(it => it != "yay_dir2-1"));
				Assert.IsTrue(paths.All(it => it != "yay_file2-1.txt"));
			}

			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.FilesThenDirs(dirPattern: "yay_*")))
				{
					add(e);
				}

				Assert.IsTrue(paths
						.Where(it => it.Contains("dir"))
						.All(it => it.StartsWith("yay_")));
				Assert.IsTrue(paths.Any(it => it.StartsWith("yay_file")));
				Assert.IsTrue(paths.Any(it => it.StartsWith("boo_file")));
			}

			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.FilesThenDirs(filePattern: "yay_*")))
				{
					add(e);
				}

				Assert.IsTrue(paths.Any(it => it.StartsWith("yay_dir")));
				Assert.IsTrue(paths.Any(it => it.StartsWith("boo_dir")));
				Assert.IsTrue(paths
						.Where(it => it.Contains("file"))
						.All(it => it.StartsWith("yay_")));
			}

			{
				paths.Clear();
				foreach (var e in Fsentry.Enumerate(fs.TempDir, Fsentry.FilesThenDirs(dirPattern: "yay_*", filePattern: "boo_*")))
				{
					add(e);
				}

				Assert.IsTrue(paths
						.Where(it => it.Contains("dir"))
						.All(it => it.StartsWith("yay_")));
				Assert.IsTrue(paths
						.Where(it => it.Contains("file"))
						.All(it => it.StartsWith("boo_")));
			}
			#endregion
		}


		[TestMethod]
		public async Task Test99_ErrorHandling_Async()
		{
			using var fs = await TempFileSystem.NewAsync(
				TestContext,
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

			foreach (var entry in Fsentry.Enumerate(fs.TempDir))
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
