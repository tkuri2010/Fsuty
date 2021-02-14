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
		static PathItems _AsItems(string relPath) => Filepath.Parse(relPath).Items;

		static int tempDirSeq = 0;

		static int NextTempDirSeq() => ++tempDirSeq;

		private static Filepath GetTempDir() =>
			Filepath.Parse(System.IO.Path.GetTempPath())
				.Combine(_AsItems("FsentryTestDir_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "_" + NextTempDirSeq()))
				.Canonicalize()
				.Also(self => {   // 安全そうなパスであることをできるだけチェック
					Assert.IsTrue(self.IsAbsolute);
					Assert.IsTrue(2 <= self.Items.Count);
				});


		private static void CleanupTempDir(Filepath tempDir)
		{
			Directory.Delete(tempDir.ToString(), recursive: true);
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
			var temp = GetTempDir();
			using var deleteLater = new Defer(() => CleanupTempDir(temp));
			var pathSet = new HashSet<string>()
			{
				"file0.txt",
				"dir1/file1-1.txt",
				"dir1/file1-2.txt",
				"dir2",
				"dir3-1/dir3-2/file3-1.txt",
				"dir3-1/dir3-2/dir3-3/file3-2.txt",
			};
			foreach (var path in pathSet)
			{
				await SetupPathAsync(temp.Combine(_AsItems(path)));
			}

			// テスト実行。見つかったディレクトリやファイルを変数 pathSet から消していく
			await foreach (var entry in Fsentry.VisitAsync(temp))
			{
				Assert.IsFalse(entry.RelativeParent.IsAbsolute);

				var fileName = System.IO.Path.GetFileName(entry.FullPathString);
				var relPathStr = entry.RelativeParent.Combine(_AsItems(fileName)).ToString("/");

				if (entry.Event == FsentryEvent.EnterDir)
				{
					if (pathSet.Contains(relPathStr))
					{
						pathSet.Remove(relPathStr);
					}
				}
				else if (entry.Event == FsentryEvent.File)
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
			// ディレクトリやファイルを実際のファイルシステム上に用意
			var temp = GetTempDir();
			using var deleteLater = new Defer(() => CleanupTempDir(temp));
			var pathSet = new HashSet<string>()
			{
				"file0.txt",
				"dir1/file1-1.txt",
				"dir1/file1-2.txt",
				"dir2",
				"dir3-1/dir3-2/file3-1.txt",
				"dir3-1/dir3-2/dir3-3/file3-2.txt",
			};
			foreach (var path in pathSet)
			{
				await SetupPathAsync(temp.Combine(_AsItems(path)));
			}

			// テスト実行。見つかったディレクトリやファイルを変数 pathSet から消していく
			// ただし、dir3-1 の中身を無視してみる
			var skipCount = 0;
			await foreach (var entry in Fsentry.VisitAsync(temp))
			{
				Assert.IsFalse(entry.RelativeParent.IsAbsolute);

				var fileName = System.IO.Path.GetFileName(entry.FullPathString);
				var relPathStr = entry.RelativeParent.Combine(_AsItems(fileName)).ToString("/");

				if (entry.Event == FsentryEvent.EnterDir)
				{
					if (relPathStr.Contains("dir3-1"))
					{
						entry.Command = FsentryCommand.SkipDirectory;
						skipCount++;
						continue;
					}

					if (pathSet.Contains(relPathStr))
					{
						pathSet.Remove(relPathStr);
					}
				}
				else if (entry.Event == FsentryEvent.File)
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
	}


	/// <summary>
	/// experimental... is this useful?
	/// </summary>
	static class Ext
	{
		/// <summary> (like Kotlin) </summary>
		public static TSelf Also<TSelf>(this TSelf self, Action<TSelf> action)
		{
			action(self);
			return self;
		}
	}


	class Defer : IDisposable
	{

		Action DeferredAction { get; init; }


		internal Defer(Action action)
		{
			DeferredAction = action;
		}


		private bool disposedValue;

		protected virtual void Dispose(bool disposing)
		{
			if (!disposedValue)
			{
				if (disposing)
				{
					DeferredAction();
				}

				disposedValue = true;
			}
		}

		public void Dispose()
		{
			// このコードを変更しないでください。クリーンアップ コードを 'Dispose(bool disposing)' メソッドに記述します
			Dispose(disposing: true);
			GC.SuppressFinalize(this);
		}
	}
}
