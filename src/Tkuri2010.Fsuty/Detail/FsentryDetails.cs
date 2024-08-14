using System.Collections.Generic;
using System.IO;

namespace Tkuri2010.Fsuty.Detail
{
	public static class FsentryDetails
	{
		static Filepath Resolve(Filepath relativeParentDir, string fullPath)
		{
			return relativeParentDir.Combine(Filepath.Parse(Path.GetFileName(fullPath)).Items);
		}


		internal interface IEnumFunc
		{
			IEnumerable<string> Enum(string path);
		}


		internal class EnumEntries : IEnumFunc
		{
			public IEnumerable<string> Enum(string path)
			{
				return Directory.EnumerateFileSystemEntries(path);
			}
		}


		internal class EnumEntriesWithPattern : IEnumFunc
		{
			string mSearchPattern;

			internal EnumEntriesWithPattern(string searchPattern)
			{
				mSearchPattern = searchPattern;
			}

			public IEnumerable<string> Enum(string path)
			{
				return Directory.EnumerateFileSystemEntries(path, mSearchPattern);
			}
		}


		/// <summary>
		/// enumerates files and dirs in undefined order
		/// </summary>
		internal class NaturalOrderMode : Fsentry.IMode
		{
			IEnumFunc mEnumFunc;


			internal NaturalOrderMode(string? searchPattern)
			{
				mEnumFunc = (searchPattern is null)
						? new EnumEntries()
						: new EnumEntriesWithPattern(searchPattern);
			}


			public void FillStack(Stack<Fsentry.IEntry> stack, string currentDirPath, Filepath currentRelPath)
			{
				foreach (var it in mEnumFunc.Enum(currentDirPath))
				{
					var relPath = Resolve(currentRelPath, it);
					Fsentry.ISuccess e = Directory.Exists(it)
							? new Fsentry.EnterDir(it, relPath)
							: new Fsentry.File(it, relPath);
					stack.Push(e);
				}
			}
		}


		internal class EnumFiles : IEnumFunc
		{
			public IEnumerable<string> Enum(string path)
			{
				return Directory.EnumerateFiles(path);
			}
		}


		internal class EnumFilesWithPattern : IEnumFunc
		{
			string mSearchPattern;


			internal EnumFilesWithPattern(string searchPattern)
			{
				mSearchPattern = searchPattern;
			}


			public IEnumerable<string> Enum(string path)
			{
				return Directory.EnumerateFiles(path, mSearchPattern);
			}
		}


		internal class EnumDirs : IEnumFunc
		{
			public IEnumerable<string> Enum(string path)
			{
				return Directory.EnumerateDirectories(path);
			}
		}


		internal class EnumDirsWithPattern : IEnumFunc
		{
			string mSearchPattern;

			internal EnumDirsWithPattern(string searchPattern)
			{
				mSearchPattern = searchPattern;
			}

			public IEnumerable<string> Enum(string path)
			{
				return Directory.EnumerateDirectories(path, mSearchPattern);
			}
		}


		/// <summary>
		/// In each directory, first enumerates dirs, then files.
		/// </summary>
		internal class DirsThenFilesMode : Fsentry.IMode
		{
			IEnumFunc mFiles;

			IEnumFunc mDirs;


			internal DirsThenFilesMode(string? dirPattern, string? filePattern)
			{
				mFiles = (filePattern is null)
						? new EnumFiles()
						: new EnumFilesWithPattern(filePattern);

				mDirs = (dirPattern is null)
						? new EnumDirs()
						: new EnumDirsWithPattern(dirPattern);
			}


			public void FillStack(Stack<Fsentry.IEntry> stack, string currentDirPath, Filepath currentRelPath)
			{
				foreach (var f in mFiles.Enum(currentDirPath))
				{
					stack.Push(new Fsentry.File(f, Resolve(currentRelPath, f)));
				}

				foreach (var d in mDirs.Enum(currentDirPath))
				{
					stack.Push(new Fsentry.EnterDir(d, Resolve(currentRelPath, d)));
				}
			}
		}


		/// <summary>
		/// In each directory, first enumerates files, then dirs.
		/// </summary>
		internal class FilesThenDirsMode : Fsentry.IMode
		{
			IEnumFunc mDirs;

			IEnumFunc mFiles;


			internal FilesThenDirsMode(string? dirPattern, string? filePattern)
			{
				mDirs = (dirPattern is null)
						? new EnumDirs()
						: new EnumDirsWithPattern(dirPattern);

				mFiles = (filePattern is null)
						? new EnumFiles()
						: new EnumFilesWithPattern(filePattern);
			}


			public void FillStack(Stack<Fsentry.IEntry> stack, string currentDirPath, Filepath currentRelPath)
			{
				foreach (var d in mDirs.Enum(currentDirPath))
				{
					stack.Push(new Fsentry.EnterDir(d, Resolve(currentRelPath, d)));
				}

				foreach (var f in mFiles.Enum(currentDirPath))
				{
					stack.Push(new Fsentry.File(f, Resolve(currentRelPath, f)));
				}
			}
		}
	}
}
