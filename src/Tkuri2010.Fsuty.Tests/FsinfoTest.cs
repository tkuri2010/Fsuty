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
		protected TestContext? TestContext { get; set; } = default;


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

			foreach (var info in Fsinfo.Enumerate(fs.TempDir))
			{
				if (info is Fsinfo.EnterDir enterDir)
				{
					// an event that means entering "dir1" or "dir1-2"
					TestContext?.WriteLine(">> entering dir: " + enterDir.Info.Name);
					enterDirCount++;
				}
				else if (info is Fsinfo.LeaveDir leaveDir)
				{
					// an event that means leaving "dir1" or "dir1-2"
					TestContext?.WriteLine("<< leaving dir: " + leaveDir.Info.Name);
					leaveDirCount++;
				}
				else if (info is Fsinfo.File file)
				{
					// a file found
					TestContext?.WriteLine("   file: " + file.Info.Name);
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

				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.FilesThenDirs()))
				{
					if (e is Fsinfo.EnterDir enterDir)
					{
						paths.Add(enterDir.Info.Name);
					}
					else if (e is Fsinfo.File file)
					{
						paths.Add(file.Info.Name);
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

				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.DirsThenFiles()))
				{
					if (e is Fsinfo.EnterDir enterDir)
					{
						paths.Add(enterDir.Info.Name);
					}
					else if (e is Fsinfo.File file)
					{
						paths.Add(file.Info.Name);
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

				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.NaturalOrder(searchPattern: "yay_*")))
				{
					if (e is Fsinfo.EnterDir dir)
						paths.Add(dir.Info.Name);
					else if (e is Fsinfo.File file)
						paths.Add(file.Info.Name);
				}

				Assert.AreEqual(paths.Count, 5);
				Assert.IsTrue(paths.All(it => it.StartsWith("yay_")));
			}
			#endregion

			#region enumerates all dirs, and only files named such as "yay_*"
			{
				var paths = new List<string>();

				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.DirsThenFiles(dirPattern: null, filePattern: "yay_*")))
				{
					if (e is Fsinfo.EnterDir dir)
						paths.Add(dir.Info.Name);
					else if (e is Fsinfo.File file)
						paths.Add(file.Info.Name);
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

			foreach (var info in Fsinfo.Enumerate(fs.TempDir))
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

			foreach (var info in Fsinfo.Enumerate(fs.TempDir))
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
			using var fs = await TempFileSystem.NewAsync(TestContext);

			foreach (var _ in Fsinfo.Enumerate(fs.TempDir))
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

			foreach (var e in Fsinfo.Enumerate("xxx_does_not_exist_xxx"))
			{
				if (e is Fsinfo.Error error)
				{
					errorCount++;

					TestContext?.WriteLine("processing dir:" + error.ParentInfo.Name);
					//=> "xxx_does_not_exist_xxx"
					TestContext?.WriteLine(error.Exception?.InnerException?.Message ?? "no idea..");
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

			foreach (var info in Fsinfo.Enumerate(fs.TempDir))
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
				TestContext,
				"file0.txt",
				"dir1",
				"     / file1-1.txt",
				"     / dir1-2",
				"             / file1-2-1.txt"
			);

			var pathSet = new HashSet<string>(fs.GetProperPaths());

			int enterDirCount = 0;
			int leaveDirCount = 0;
			int fileCount = 0;

			foreach (var info in Fsinfo.Enumerate(fs.TempDir))
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

			foreach (var info in Fsinfo.Enumerate(fs.TempDir))
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

				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.DirsThenFiles()))
				{
					if (e is Fsinfo.EnterDir enterDir)
					{
						paths.Add(enterDir.Info.Name);
					}
					else if (e is Fsinfo.File file)
					{
						paths.Add(file.Info.Name);
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

				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.FilesThenDirs()))
				{
					if (e is Fsinfo.EnterDir enterDir)
					{
						paths.Add(enterDir.Info.Name);
					}
					else if (e is Fsinfo.File file)
					{
						paths.Add(file.Info.Name);
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

				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.DirsThenFiles()))
				{
					if (e is Fsinfo.EnterDir enterDir)
					{
						if (enterDir.Info.Name == "dir1")
						{
							// ignore
						}
						else
						{
							paths.Add(enterDir.Info.Name);
						}
					}
					else if (e is Fsinfo.File file)
					{
						paths.Add(file.Info.Name);
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

				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.FilesThenDirs()))
				{
					if (e is Fsinfo.EnterDir enterDir)
					{
						if (enterDir.Info.Name == "dir1")
						{
							// ignore
						}
						else
						{
							paths.Add(enterDir.Info.Name);
						}
					}
					else if (e is Fsinfo.File file)
					{
						paths.Add(file.Info.Name);
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
			Action<Fsinfo.IInfo> add = (entry) =>
			{
				if (entry is Fsinfo.EnterDir enterDir)
				{
					paths.Add(enterDir.Info.Name);
				}
				else if (entry is Fsinfo.File file)
				{
					paths.Add(file.Info.Name);
				}
			};

			{
				paths.Clear();
				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.NaturalOrder(null)))
				{
					add(e);
				}

				Assert.AreEqual(paths.Count, dirsFiles.Length);
			}

			{
				paths.Clear();
				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.NaturalOrder("yay_*")))
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
				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.DirsThenFiles("yay_*")))
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
				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.DirsThenFiles(dirPattern: "yay_*")))
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
				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.DirsThenFiles(filePattern: "yay_*")))
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
				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.DirsThenFiles(dirPattern: "yay_*", filePattern: "boo_*")))
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
				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.FilesThenDirs("yay_*")))
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
				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.FilesThenDirs(dirPattern: "yay_*")))
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
				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.FilesThenDirs(filePattern: "yay_*")))
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
				foreach (var e in Fsinfo.Enumerate(fs.TempDir, Fsinfo.FilesThenDirs(dirPattern: "yay_*", filePattern: "boo_*")))
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

			foreach (var entry in Fsinfo.Enumerate(fs.TempDir))
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