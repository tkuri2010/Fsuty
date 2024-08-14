using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Tkuri2010.Fsuty.Detail;


namespace Tkuri2010.Fsuty
{
	/// <summary>
	/// enumerates DirectoryInfo or FileInfo.
	/// </summary>
	public class Fsinfo
	{

		/// <summary>
		/// enumerates all file system entries as DirectoryInfo or FileInfo, recursivery
		/// </summary>
		/// <param name="basePath">directory path</param>
		/// <param name="enumMode">specifies enumeration order (unordered files and dirs, dirs then files, or files then dirs) and search pattern </param>
		/// <returns></returns>
		public static IEnumerable<IInfo> Enumerate(Filepath basePath, IMode? enumMode = null)
		{
			return Enumerate(basePath.ToString(), enumMode);
		}


		/// <summary>
		/// enumerates all file system entries as DirectoryInfo or FileInfo, recursivery
		/// </summary>
		/// <param name="basePath">directory path</param>
		/// <param name="enumMode">specifies enumeration order (unordered files and dirs, dirs then files, or files then dirs) and search pattern </param>
		/// <returns></returns>
		public static IEnumerable<IInfo> Enumerate(string basePath, IMode? enumMode = null)
		{
			return Enumerate(new DirectoryInfo(basePath), enumMode);
		}


		/// <summary>
		/// enumerates all file system entries as DirectoryInfo or FileInfo, recursivery
		/// </summary>
		/// <param name="baseDir">directory info</param>
		/// <param name="enumMode">specifies enumeration order (unordered files and dirs, dirs then files, or files then dirs) and search pattern </param>
		/// <returns></returns>
		public static IEnumerable<IInfo> Enumerate(DirectoryInfo baseDir, IMode? enumMode = null)
		{
			enumMode ??= NaturalOrder();

			var stack = new Stack<IInfo>();

			var currentDirInfo = baseDir;

			do
			{
				try
				{
					enumMode.FillStack(stack, currentDirInfo);
				}
				catch (Exception x)
				{
					stack.Push(new Error(x, currentDirInfo));
				}

				while (stack.XTryPop(out var someInfo))
				{
					yield return someInfo!;

					if (someInfo!.Reaction == InternalReaction.LeaveParentDir)
					{
						stack.XPopWhile(it => it is not LeaveDir);

						if (stack.Any())
						{
							// Assert: stack.Peek() is LeaveDir;
							continue;
						}
						else
						{
							yield break;
						}
					}

					if (someInfo.Reaction != InternalReaction.Advance)
					{
						continue;
					}

					if (someInfo is not EnterDir enterDir)
					{
						continue;
					}

					stack.Push(new LeaveDir(enterDir));

					currentDirInfo = enterDir.Info;
					break;
				}
			}
			while (stack.Any());
		}


		/// <summary>
		/// In each directory, enumerates files and dirs in undefined order,
		/// Internally it uses the `DirectoryInfo.EnumerateFileSystemInfos()` method for enumeration.
		/// </summary>
		/// <param name="searchPattern">
		///   item name pattern. Enumerates all items when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratefilesysteminfos#system-io-directoryinfo-enumeratefilesysteminfos(system-string)">
		///     `System.Io.DirectoryInfo.EnumerateFileSystemInfos()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		public static IMode NaturalOrder(string? searchPattern = null)
		{
			return new FsinfoDetails.NaturalOrderMode(searchPattern);
		}


		/// <summary>
		/// In each directory, first enumerates dirs, then files.
		/// Internally it uses the `DirectoryInfo.EnumerateDirectories()` method followed by the `DirectoryInfo.EnumerateFiles()` method for enumeration.
		/// </summary>
		/// <param name="searchPattern">
		///   item name pattern.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratedirectories#system-io-directoryinfo-enumeratedirectories(system-string)">
		///     `System.Io.DirectoryInfo.EnumerateDirectories()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		public static IMode DirsThenFiles(string searchPattern)
		{
			return DirsThenFiles(searchPattern, searchPattern);
		}


		/// <summary>
		/// In each directory, first enumerates dirs, then files.
		/// Internally it uses the `DirectoryInfo.EnumerateDirectories()` method followed by the `DirectoryInfo.EnumerateFiles()` method for enumeration.
		/// </summary>
		/// <param name="dirPattern">
		///   dir name pattern. Enumerates all dirs when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratedirectories#system-io-directoryinfo-enumeratedirectories(system-string)">
		///     `System.Io.DirectoryInfo.EnumerateDirectories()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		/// <param name="filePattern">
		///   file name pattern. Enumerates all files when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratefiles#system-io-directoryinfo-enumeratefiles(system-string)">
		///     `System.Io.DirectoryInfo.EnumerateFiles()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		public static IMode DirsThenFiles(string? dirPattern = null, string? filePattern = null)
		{
			return new FsinfoDetails.DirsThenFilesMode(dirPattern: dirPattern, filePattern: filePattern);
		}


		/// <summary>
		/// In each directory, first enumerates files, then dirs.
		/// Internally it uses the `DirectoryInfo.EnumerateFiles()` method followed by the `DirectoryInfo.EnumerateDirectories()` method for enumeration.
		/// </summary>
		/// <param name="searchPattern">
		///   item name pattern. Enumerates all items when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratefiles#system-io-directoryinfo-enumeratefiles(system-string)">
		///     `System.Io.DirectoryInfo.EnumerateFiles()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		public static IMode FilesThenDirs(string searchPattern)
		{
			return FilesThenDirs(searchPattern, searchPattern);
		}


		/// <summary>
		/// In each directory, first enumerates files, then dirs.
		/// Internally it uses the `DirectoryInfo.EnumerateFiles()` method followed by the `DirectoryInfo.EnumerateDirectories()` method for enumeration.
		/// </summary>
		/// <param name="filePattern">
		///   file name pattern. Enumerates all files when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratefiles#system-io-directoryinfo-enumeratefiles(system-string)">
		///     `System.Io.DirectoryInfo.EnumerateFiles()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		/// <param name="dirPattern">
		///   dir name pattern. Enumerates all dirs when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directoryinfo.enumeratedirectories#system-io-directoryinfo-enumeratedirectories(system-string)">
		///     `System.Io.DirectoryInfo.EnumerateDirectories()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		public static IMode FilesThenDirs(string? filePattern = null, string? dirPattern = null)
		{
			return new FsinfoDetails.FilesThenDirsMode(filePattern: filePattern, dirPattern: dirPattern);
		}


		/// <summary>
		/// enumeration mode
		/// </summary>
		public interface IMode
		{
			void FillStack(Stack<IInfo> stack, DirectoryInfo currentDirInfo);
		}


		/// <summary>(opaque item)</summary>
		public interface IInfo
		{
			/// <summary>(internal use only)</summary>
			InternalReaction Reaction { get; }


			/// <summary>
			/// Command to leave parent directory.
			/// </summary>
			void LeaveParentDir();


			/// <summary>
			/// Parent directory info.
			/// </summary>
			DirectoryInfo ParentInfo { get; }
		}


		/// <summary>(opaque item)</summary>
		public interface ISuccess : IInfo
		{
		}


		/// <summary>
		/// Entering into a Directory
		/// </summary>
		public class EnterDir : ISuccess
		{
			/// <summary>
			/// DirectoryInfo
			/// </summary>
			public DirectoryInfo Info { get; private set; }


			public InternalReaction Reaction { get; private set; } = InternalReaction.Advance;


			public DirectoryInfo ParentInfo { get; private set; }


			/// <summary>
			/// (do not call directly)
			/// </summary>
			/// <param name="info"></param>
			/// <param name="parentInfo"></param>
			public EnterDir(DirectoryInfo info, DirectoryInfo parentInfo)
			{
				Info = info;
				ParentInfo = parentInfo;
			}


			public void LeaveParentDir()
			{
				Reaction = InternalReaction.LeaveParentDir;
			}


			/// <summary>
			/// Command to skip entering into directory.
			/// </summary>
			public void Skip()
			{
				Reaction = InternalReaction.SkipEnterDir;
			}
		}


		/// <summary>
		/// Leaving from a directory.
		/// </summary>
		public class LeaveDir : ISuccess
		{
			/// <summary>
			/// DirectoryInfo
			/// </summary>
			public DirectoryInfo Info { get; private set; }


			public InternalReaction Reaction { get; private set; } = InternalReaction.Advance;


			public DirectoryInfo ParentInfo { get; private set; }


			/// <summary>
			/// (do not call directly)
			/// </summary>
			/// <param name="enterDir"></param>
			/// <param name="parentInfo"></param>
			public LeaveDir(EnterDir enterDir)
			{
				Info = enterDir.Info;
				ParentInfo = enterDir.ParentInfo;
			}


			public void LeaveParentDir()
			{
				Reaction = InternalReaction.LeaveParentDir;
			}
		}


		/// <summary>
		/// File found.
		/// </summary>
		public class File : ISuccess
		{
			/// <summary>
			/// FileInfo
			/// </summary>
			public FileInfo Info { get; private set; }


			public InternalReaction Reaction { get; private set; } = InternalReaction.Advance;


			public DirectoryInfo ParentInfo { get; private set; }


			/// <summary>
			/// (do not call directly)
			/// </summary>
			/// <param name="info"></param>
			/// <param name="parentInfo"></param>
			public File(FileInfo info, DirectoryInfo parentInfo)
			{
				Info = info;
				ParentInfo = parentInfo;
			}


			public void LeaveParentDir()
			{
				Reaction = InternalReaction.LeaveParentDir;
			}
		}


		/// <summary>
		/// Error
		/// </summary>
		public class Error : IInfo
		{
			/// <summary>
			/// Thrown exception object (if available).
			/// Typically it may be an EnumerationException,
			/// </summary>
			public Exception? Exception { get; private set; }


			public InternalReaction Reaction { get; private set; } = InternalReaction.Advance;


			public DirectoryInfo ParentInfo { get; private set; }


			/// <summary>
			/// (do not call directly)
			/// </summary>
			/// <param name="exception"></param>
			/// <param name="parentInfo"></param>
			public Error(Exception? exception, DirectoryInfo parentInfo)
			{
				Exception = exception;
				ParentInfo = parentInfo;
			}


			public void LeaveParentDir()
			{
				Reaction = InternalReaction.LeaveParentDir;
			}
		}
	}
}
