using System;
using System.Collections.Generic;
using System.IO;

namespace Tkuri2010.Fsuty.Detail
{
	public static class FsinfoDetails
	{
		internal interface IEnumEntriesFunc
		{
			IEnumerable<FileSystemInfo> Enum(DirectoryInfo currentDirInfo);
		}


		internal class EnumEntries : IEnumEntriesFunc
		{
			public IEnumerable<FileSystemInfo> Enum(DirectoryInfo currentDirInfo)
			{
				return currentDirInfo.EnumerateFileSystemInfos();
			}
		}


		internal class EnumIEntriesWithPattern : IEnumEntriesFunc
		{
			string mSearchPattern;

			internal EnumIEntriesWithPattern(string searchPattern)
			{
				mSearchPattern = searchPattern;
			}

			public IEnumerable<FileSystemInfo> Enum(DirectoryInfo currentDirInfo)
			{
				return currentDirInfo.EnumerateFileSystemInfos(mSearchPattern);
			}
		}


		internal class NaturalOrderMode : Fsinfo.IMode
		{
			IEnumEntriesFunc mEnumFunc;


			internal NaturalOrderMode(string? searchPattern)
			{
				mEnumFunc = (searchPattern is null)
						? new EnumEntries()
						: new EnumIEntriesWithPattern(searchPattern);
			}


			public void FillStack(Stack<Fsinfo.IInfo> stack, DirectoryInfo currentDirInfo)
			{
				foreach (var it in mEnumFunc.Enum(currentDirInfo))
				{
					Fsinfo.ISuccess e = (it is DirectoryInfo d)
							? new Fsinfo.EnterDir(d, currentDirInfo)
							: new Fsinfo.File((FileInfo) it, currentDirInfo);
					stack.Push(e);
				}
			}
		}


		internal interface IEnumFilesFunc
		{
			IEnumerable<FileInfo> Enum(DirectoryInfo parent);
		}


		internal class EnumFiles : IEnumFilesFunc
		{
			public IEnumerable<FileInfo> Enum(DirectoryInfo currentDirInfo)
			{
				return currentDirInfo.EnumerateFiles();
			}
		}


		internal class EnumFilesWithPattern : IEnumFilesFunc
		{
			string mSearchPattern;

			internal EnumFilesWithPattern(string searchPattern)
			{
				mSearchPattern = searchPattern;
			}

			public IEnumerable<FileInfo> Enum(DirectoryInfo currentDirInfo)
			{
				return currentDirInfo.EnumerateFiles(mSearchPattern);
			}
		}


		internal interface IEnumDirsFunc
		{
			IEnumerable<DirectoryInfo> Enum(DirectoryInfo currentDirInfo);
		}


		internal class EnumDirs : IEnumDirsFunc
		{
			public IEnumerable<DirectoryInfo> Enum(DirectoryInfo currentDirInfo)
			{
				return currentDirInfo.EnumerateDirectories();
			}
		}


		internal class EnumDirsWithPattern : IEnumDirsFunc
		{
			string mSearchPattern;

			internal EnumDirsWithPattern(string searchPattern)
			{
				mSearchPattern = searchPattern;
			}

			public IEnumerable<DirectoryInfo> Enum(DirectoryInfo currentDirInfo)
			{
				return currentDirInfo.EnumerateDirectories(mSearchPattern);
			}
		}


		internal class DirsThenFilesMode : Fsinfo.IMode
		{
			IEnumDirsFunc mDirs;

			IEnumFilesFunc mFiles;


			internal DirsThenFilesMode(string? dirPattern, string? filePattern)
			{
				mDirs = (dirPattern is null)
						? new EnumDirs()
						: new EnumDirsWithPattern(dirPattern);

				mFiles = (filePattern is null)
						? new EnumFiles()
						: new EnumFilesWithPattern(filePattern);
			}


			public void FillStack(Stack<Fsinfo.IInfo> stack, DirectoryInfo currentDirInfo)
			{
				foreach (var f in mFiles.Enum(currentDirInfo))
				{
					stack.Push(new Fsinfo.File(f, currentDirInfo));
				}

				foreach (var d in mDirs.Enum(currentDirInfo))
				{
					stack.Push(new Fsinfo.EnterDir(d, currentDirInfo));
				}
			}
		}


		internal class FilesThenDirsMode : Fsinfo.IMode
		{
			IEnumFilesFunc mFiles;

			IEnumDirsFunc mDirs;


			internal FilesThenDirsMode(string? filePattern, string? dirPattern)
			{
				mFiles = (filePattern is null)
						? new EnumFiles()
						: new EnumFilesWithPattern(filePattern);

				mDirs = (dirPattern is null)
						? new EnumDirs()
						: new EnumDirsWithPattern(dirPattern);
			}


			public void FillStack(Stack<Fsinfo.IInfo> stack, DirectoryInfo currentDirInfo)
			{
				foreach (var d in mDirs.Enum(currentDirInfo))
				{
					stack.Push(new Fsinfo.EnterDir(d, currentDirInfo));
				}

				foreach (var f in mFiles.Enum(currentDirInfo))
				{
					stack.Push(new Fsinfo.File(f, currentDirInfo));
				}
			}
		}
	}
}
