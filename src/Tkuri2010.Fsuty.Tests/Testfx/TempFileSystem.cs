using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tkuri2010.Fsuty.Tests.Testfx
{
	public class TempFileSystem : IDisposable
	{
		static readonly object _CreatingDirLock = new();

		static PathItems _AsItems(string relPath) => Filepath.Parse(relPath).Items;


		/// <summary>
		/// 相対パスのリストを指定してインスタンスを取得
		/// </summary>
		public static async ValueTask<TempFileSystem> NewAsync(params string[] initialRelativePaths)
		{
			var self = new TempFileSystem();

			var properPaths = Ext.FillOmittedLines(initialRelativePaths)
					.Select(it => it.Replace(" ", ""));

			foreach (var path in properPaths)
			{
				await self.SetupPathAsync(path);
				self.m_properPaths.Add(path);
			}

			return self;
		}


		public static Filepath GetSafeSystemTempDir()
		{
			return Filepath.Parse(Path.GetTempPath())
					.Canonicalize()
					.Also(AssertSafeTempPath);
		}


		/// <summary>Asserts that the input directory is safe for use in testing.</summary>
		/// <param name="tempPath"></param>
		public static void AssertSafeTempPath(Filepath tempPath)
		{
			var errorMessage = $"The path is not safe for use in testing: {tempPath}";

			if (! tempPath.IsAbsolute) throw new Exception(errorMessage);
			if (! (tempPath.Items.Count >= 1)) throw new Exception(errorMessage);

			bool containsTempName = tempPath.Items.Any(it =>
					it.ToLower().Contains("temp")
					|| it.ToLower().Contains("tmp"));
			if (! containsTempName)
			{
				throw new Exception(errorMessage);
			}
		}


		public static Filepath CreateSafeTempDir()
		{
			var tempBase = Filepath.Parse(Path.GetTempPath())
					.Canonicalize()
					.Also(AssertSafeTempPath);

			Filepath rv = Filepath.Empty;
			bool ok = false;
			for (var limit = 0; limit < 10; limit++)
			{
				var random = $"{Ext.RandomChar()}{Ext.RandomChar()}{Ext.RandomChar()}";
				rv = tempBase.Combine(_AsItems("FsutyTestDir_" + DateTime.Now.ToString("HHmmss") + "_" + random))
						.Also(AssertSafeTempPath);
				var pathString = rv.ToString();
				lock (_CreatingDirLock)
				{
					if (Directory.Exists(pathString))
					{
						continue;
					}
					else
					{
						StaticTestContext.WriteLine($"using tempdir {pathString}");
						Directory.CreateDirectory(pathString);
						ok = true;
						break;
					}
				}
			}

			if (! ok)
			{
				throw new Exception($"Preparing Test Failed: Could not create a safe temp dir. The last candidate path was: {rv}");
			}

			return rv;
		}


		public readonly Filepath TempDir;


		private List<string> m_properPaths = new();


		public TempFileSystem()
		{
			TempDir = CreateSafeTempDir();
		}


		/// <summary>
		/// NewAsync() の引数に入力されたパスで、省略部分が埋められたり、余分な空白が削除されたりしてあるもの。
		/// </summary>
		/// <returns></returns>
		public IEnumerable<string> GetPropertPaths()
		{
			return m_properPaths;
		}


		/// <summary>
		/// 拡張子が存在するパス名なら、ファイルを作る
		/// そうでなければディレクトリを作る
		/// </summary>
		public async ValueTask SetupPathAsync(string relativePath)
		{
			var relPath = Filepath.Parse(relativePath);
			if (relPath.Prefix is not PathPrefix.None) throw new Exception($"Preparing Test Failed: The argument must not have any prefixes (drive letter, server name...): {relativePath}");
			if (relPath.IsAbsolute) throw new Exception($"Preparing Test Failed: must be relative path: {relativePath}");

			if (relPath.HasExtension)
			{
				await CreateTextFileAsync(relPath);
			}
			else
			{
				CreateDirectory(relPath);
			}
		}


		public void CreateDirectory(Filepath relativePath)
		{
			var absPath = TempDir.Combine(relativePath.Items)
					.Canonicalize()
					.Also(AssertSafeTempPath);

			Directory.CreateDirectory(absPath.ToString());
		}


		public async Task CreateTextFileAsync(Filepath relativePath)
		{
			var absPath = TempDir.Combine(relativePath.Items)
					.Canonicalize()
					.Also(AssertSafeTempPath);

			Directory.CreateDirectory(absPath.Parent.ToString());

			await File.WriteAllTextAsync(absPath.ToString(), "some file.", System.Text.Encoding.UTF8);
		}


		public void Delete(Filepath relativePath)
		{
			var full = TempDir.Combine(relativePath.Items)
					.Canonicalize()
					.Also(AssertSafeTempPath)
					.ToString();

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
				StaticTestContext.WriteLine($"Not exist: {relativePath}");
			}
		}


		public void Delete(string path)
		{
			var pathobj = Filepath.Parse(path);
			if (! pathobj.IsAbsolute)
			{
				pathobj = TempDir.Combine(pathobj.Items);
			}

			var full = pathobj.Canonicalize()
					.Also(AssertSafeTempPath)
					.ToString();

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
				StaticTestContext.WriteLine($"not exist: {path}");
			}
		}


		public void CleanupTempDir()
		{
			AssertSafeTempPath(TempDir);

			var dirString = TempDir.ToString();

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
		/// <summary> (same as Kotlin) </summary>
		internal static TSelf Also<TSelf>(this TSelf self, Action<TSelf> action)
		{
			action(self);
			return self;
		}


		public static char RandomChar() => (char)('a' + Random.Shared.Next(25));


		/// <summary>
		/// [
		///   "dir1/file1.txt",
		///   "     file2.txt",
		///   "     file3.txt",
		/// ]
		///        ↓
		/// [
		///   "dir1/file1.txt",
		///   "dir1/file2.txt",
		///   "dir1/file3.txt",
		/// ]
		/// </summary>
		/// <param name="originalLines"></param>
		/// <returns></returns>
		internal static IEnumerable<string> FillOmittedLines(IEnumerable<string> originalLines)
		{
			string prevLine = string.Empty;
			foreach (var line in originalLines)
			{
				if (line.StartsWith(" "))
				{
					var spaces = CountSpaces(line);
					var takes = Math.Min(prevLine.Length, spaces);
					var newLine = prevLine.Substring(0, takes) + line.Substring(takes);
					yield return newLine;
					prevLine = newLine;
				}
				else
				{
					yield return line;
					prevLine = line;
				}
			}
		}


		internal static int CountSpaces(string line)
		{
			int count = 0;
			foreach (var c in line)
			{
				if (c == ' ')
				{
					count++;
				}
				else
				{
					break;
				}
			}
			return count;
		}
	}

}

