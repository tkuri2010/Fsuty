using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Tkuri2010.Fsuty.Tests.Testfx
{
	public class TempFileSystem : IDisposable
	{
		static PathItems _AsItems(string relPath) => Filepath.Parse(relPath).Items;

		static int tempDirSeq = 0;

		static int NextTempDirSeq() => ++tempDirSeq;


		/// <summary>
		/// 相対パスのリストを指定してインスタンスを取得
		/// </summary>
		public static async ValueTask<TempFileSystem> NewAsync(IEnumerable<string> initialRelativePaths)
		{
			var self = new TempFileSystem();
			await self.SetupPathAllAsync(initialRelativePaths);
			return self;
		}


		public readonly Filepath TempDir;


		public TempFileSystem()
		{
			TempDir = Filepath.Parse(System.IO.Path.GetTempPath())
				.Combine(_AsItems("FsentryTestDir_" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + "_" + NextTempDirSeq()))
				.Canonicalize()
				.Also(self => {   // 安全そうなパスであることをできるだけチェック
					if (! self.IsAbsolute) throw new Exception("Preparing Test Failed.");
					if (self.Items.Count < 2) throw new Exception("Preparing Test Failed.");
				});
		}


		/// <summary>
		/// 拡張子が存在するパス名なら、ファイルを作る
		/// そうでなければディレクトリを作る
		/// </summary>
		public async ValueTask SetupPathAsync(string relativePath)
		{
			var relPath = Filepath.Parse(relativePath);
			if (relPath.IsAbsolute) throw new Exception($"Preparing Test Failed: {relativePath}: must not absolute path.");

			if (relPath.HasExtension)
			{
				await CreateTextFileAsync(relPath);
			}
			else
			{
				CreateDirectory(relPath);
			}
		}


		public async ValueTask SetupPathAllAsync(IEnumerable<string> paths)
		{
			foreach (var path in paths)
			{
				await SetupPathAsync(path);
			}
		}


		public void CreateDirectory(Filepath relativePath)
		{
			if (relativePath.IsAbsolute)
			{
				throw new Exception("abs");
			}

			var absPath = TempDir.Combine(relativePath.Items);
			System.IO.Directory.CreateDirectory(absPath.ToString());
		}


		public async Task CreateTextFileAsync(Filepath relativePath)
		{
			if (relativePath.IsAbsolute)
			{
				throw new Exception("abs");
			}

			var absPath = TempDir.Combine(relativePath.Items);
			System.IO.Directory.CreateDirectory(absPath.Parent.ToString());
			await System.IO.File.WriteAllTextAsync(absPath.ToString(), "some file.", System.Text.Encoding.UTF8);
		}


		public void Delete(Filepath relativePath)
		{
			if (relativePath.IsAbsolute)
			{
				throw new Exception("abs");
			}

			var full = TempDir.Combine(relativePath.Items).ToString();
			if (Directory.Exists(full))
			{
				Directory.Delete(full, recursive: true);
			}
			else if (File.Exists(full))
			{
				File.Delete(full);
			}
			else
			{
				System.Diagnostics.Debug.WriteLine($"does not exist: {relativePath}");
			}
		}


		public void CleanupTempDir()
		{
			if (! TempDir.IsAbsolute)
			{
				throw new Exception("Celanup Failed.");
			}

			var dirString = TempDir.ToString();
			if (dirString.Length < "/tmp".Length) // テストに使ったディレクトリ名は「/tmp」よりは長いはず
			{
				throw new Exception($"CleanupTempDir: invalid tempDir: {dirString}");
			}

			Directory.Delete(dirString, recursive: true);
		}


		public void Dispose()
		{
			CleanupTempDir();
		}
	}


	/// <summary>
	/// experimental... is this useful?
	/// </summary>
	internal static class Ext
	{
		/// <summary> (like Kotlin) </summary>
		internal static TSelf Also<TSelf>(this TSelf self, Action<TSelf> action)
		{
			action(self);
			return self;
		}
	}

}

