using System;
using System.Collections.Generic;
using System.Linq;
using Tkuri2010.Fsuty.Detail;


namespace Tkuri2010.Fsuty
{
	/// <summary>
	/// enumerates file system entries.
	/// </summary>
	public class Fsentry
	{
		/// <summary>
		/// enumerates all file system entries, recursivery
		/// </summary>
		/// <param name="basePath">directory path</param>
		/// <param name="enumMode">specifies enumeration order (unordered files and dirs, dirs then files, or files then dirs) and search pattern </param>
		/// <returns></returns>
		public static IEnumerable<IEntry> Enumerate(Filepath basePath, IMode? enumMode = null)
		{
			return Enumerate(basePath.ToString(), enumMode);
		}


		/// <summary>
		/// enumerates all file system entries, recursivery
		/// </summary>
		/// <param name="basePath">directory path</param>
		/// <param name="enumMode">specifies enumeration order (unordered files and dirs, dirs then files, or files then dirs) and search pattern </param>
		/// <returns></returns>
		public static IEnumerable<IEntry> Enumerate(string basePath, IMode? enumMode = null)
		{
			enumMode ??= NaturalOrder();

			var stack = new Stack<IEntry>();

			var currentDirPath = basePath;
			var currentDirRelPath = Filepath.Empty;

			do
			{
				try
				{
					enumMode.FillStack(stack, currentDirPath, currentDirRelPath);
				}
				catch (Exception x)
				{
					stack.Push(new Error(x, currentDirPath, currentDirRelPath));
				}

				while (stack.XTryPop(out var someEntry))
				{
					yield return someEntry!;

					if (someEntry!.Reaction == InternalReaction.LeaveParentDir)
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

					if (someEntry.Reaction != InternalReaction.Advance)
					{
						continue;
					}

					if (someEntry is not EnterDir enterDir)
					{
						continue;
					}

					stack.Push(new LeaveDir(enterDir));

					currentDirPath = enterDir.FullPathString;
					currentDirRelPath = enterDir.RelativePath;
					break;
				}
			}
			while (stack.Any());
		}


		/// <summary>
		/// In each directory, enumerates files and dirs in undefined order,
		/// Internally it uses the `Directory.EnumerateFileSystemEntries()` method for enumeration.
		/// </summary>
		/// <param name="searchPattern">
		///   item name pattern. Enumerates all items when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefilesystementries#system-io-directory-enumeratefilesystementries(system-string-system-string)">
		///     `System.Io.Directory.EnumerateFileSystemEntries()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		public static IMode NaturalOrder(string? searchPattern = null)
		{
			return new FsentryDetails.NaturalOrderMode(searchPattern);
		}


		/// <summary>
		/// In each directory, first enumerates dirs, then files.
		/// Internally it uses the `Directory.EnumerateDirectories()` method followed by the `Directory.EnumerateFiles()` method for enumeration.
		/// </summary>
		/// <param name="searchPattern">
		///   item name pattern. Enumerates all items when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratedirectories#system-io-directory-enumeratedirectories(system-string-system-string)">
		///     `System.Io.Directory.EnumerateDirectories()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		public static IMode DirsThenFiles(string? searchPattern)
		{
			return new FsentryDetails.DirsThenFilesMode(searchPattern, searchPattern);
		}


		/// <summary>
		/// In each directory, first enumerates dirs, then files.
		/// Internally it uses the `Directory.EnumerateDirectories()` method followed by the `Directory.EnumerateFiles()` method for enumeration.
		/// </summary>
		/// <param name="dirPattern">
		///   dir name pattern. Enumerates all dirs when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratedirectories#system-io-directory-enumeratedirectories(system-string-system-string)">
		///     `System.Io.Directory.EnumerateDirectories()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		/// <param name="filePattern">
		///   file name pattern. Enumerates all files when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles#system-io-directory-enumeratefiles(system-string-system-string)">
		///     `System.Io.Directory.EnumerateFiles()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		public static IMode DirsThenFiles(string? dirPattern = null, string? filePattern = null)
		{
			return new FsentryDetails.DirsThenFilesMode(dirPattern: dirPattern, filePattern: filePattern);
		}


		/// <summary>
		/// In each directory, first enumerates files, then dirs.
		/// Internally it uses the `Directory.EnumerateFiles()` method followed by the `Directory.EnumerateDirectories()` method for enumeration.
		/// </summary>
		/// <param name="searchPattern">
		///   item name pattern. Enumerates all items when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles#system-io-directory-enumeratefiles(system-string-system-string)">
		///     `System.Io.Directory.EnumerateFiles()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		public static IMode FilesThenDirs(string? searchPattern)
		{
			return new FsentryDetails.FilesThenDirsMode(searchPattern, searchPattern);
		}


		/// <summary>
		/// In each directory, first enumerates files, then dirs.
		/// Internally it uses the `Directory.EnumerateFiles()` method followed by the `Directory.EnumerateDirectories()` method for enumeration.
		/// </summary>
		/// <param name="filePattern">
		///   file name pattern. Enumerates all files when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratefiles#system-io-directory-enumeratefiles(system-string-system-string)">
		///     `System.Io.Directory.EnumerateFiles()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		/// <param name="dirPattern">
		///   dir name pattern. Enumerates all dirs when this parameter is null.
		///   See <see href="https://learn.microsoft.com/en-us/dotnet/api/system.io.directory.enumeratedirectories#system-io-directory-enumeratedirectories(system-string-system-string)">
		///     `System.Io.Directory.EnumerateDirectories()` parameter - `searchPattern`
		///   </see> for more detail.
		/// </param>
		public static IMode FilesThenDirs(string? filePattern = null, string? dirPattern = null)
		{
			return new FsentryDetails.FilesThenDirsMode(filePattern: filePattern, dirPattern: dirPattern);
		}


		/// <summary>
		/// enumeration mode
		/// </summary>
		public interface IMode
		{
			void FillStack(Stack<IEntry> stack, string currentDirPath, Filepath currentRelPath);
		}


		/// <summary>(opaque item)</summary>
		public interface IEntry
		{
			/// <summary>(internal use only)</summary>
			InternalReaction Reaction { get; }


			/// <summary>
			/// Command to leave parent directory.
			/// </summary>
			void LeaveParentDir();
		}


		/// <summary>(opaque item)</summary>
		public interface ISuccess : IEntry
		{
			/// <summary>
			/// Raw result value from <code>System.IO.Directory.Enum***()</code>.
			/// </summary>
			string FullPathString { get; }


			/// <summary>
			/// Relative path object from the path input to <code>EnumerateAsync(HERE)</code>.
			/// </summary>
			Filepath RelativePath { get; }
		}


		/// <summary>
		/// Entering into a Directory
		/// </summary>
		public class EnterDir : ISuccess
		{
			public string FullPathString { get; private set; }


			public Filepath RelativePath { get; private set; }


			public InternalReaction Reaction { get; internal set; } = InternalReaction.Advance;


			/// <summary>
			/// (do not call directly)
			/// </summary>
			/// <param name="fullPathString"></param>
			/// <param name="relativePath"></param>
			public EnterDir(string fullPathString, Filepath relativePath)
			{
				FullPathString = fullPathString;
				RelativePath = relativePath;
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
			public string FullPathString { get; private set; }

			public Filepath RelativePath { get; private set; }

			public InternalReaction Reaction { get; internal set; } = InternalReaction.Advance;

			/// <summary>
			/// (do not call directly)
			/// </summary>
			/// <param name="fullPathString"></param>
			/// <param name="relativePath"></param>
			public LeaveDir(string fullPathString, Filepath relativePath)
			{
				FullPathString = fullPathString;
				RelativePath = relativePath;
			}


			/// <summary>
			/// (do not call directly)
			/// </summary>
			/// <param name="enterDir"></param>
			public LeaveDir(EnterDir enterDir)
					: this(enterDir.FullPathString, enterDir.RelativePath)
			{
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
			public string FullPathString { get; private set; }

			public Filepath RelativePath { get; private set; }

			public InternalReaction Reaction { get; internal set; } = InternalReaction.Advance;

			/// <summary>
			/// (do not call directly)
			/// </summary>
			/// <param name="fullPathString"></param>
			/// <param name="relativePath"></param>
			public File(string fullPathString, Filepath relativePath)
			{
				FullPathString = fullPathString;
				RelativePath = relativePath;
			}

			public void LeaveParentDir()
			{
				Reaction = InternalReaction.LeaveParentDir;
			}
		}


		/// <summary>
		/// Error
		/// </summary>
		public class Error : IEntry
		{
			/// <summary>
			/// Thrown exception object (if available).
			/// Typically it may be an EnumerationException,
			/// </summary>
			public Exception Exception { get; internal set; }


			/// <summary>
			/// The directory path that tried to enumerate items. May be absolute, or may be relative.
			/// </summary>
			public string DirPathString { get; internal set; }


			/// <summary>
			/// The relative path from Fsentry.Enumerate(HERE) of the `DirPathString`. May be Empty.
			/// </summary>
			public Filepath DirRelativePath { get; internal set; }


			public InternalReaction Reaction { get; internal set; } = InternalReaction.Advance;


			/// <summary>(internal use only)</summary>
			public Error(Exception exception, string dirPath, Filepath relativePath)
			{
				Exception = exception;
				DirPathString = dirPath;
				DirRelativePath = relativePath;
			}


			public void LeaveParentDir()
			{
				Reaction = InternalReaction.LeaveParentDir;
			}
		}
	}
}

