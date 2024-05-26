using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;


namespace Tkuri2010.Fsuty
{
	public abstract class Fsentry
	{
		/// <summary>
		/// enumerates all file system entries, recursivery
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static IAsyncEnumerable<IEntry> EnumerateAsync(Filepath basePath, CancellationToken ct = default)
		{
			return EnumerateAsync(basePath.ToString(), ct);
		}


		/// <summary>
		/// enumerates all file system entries, recursivery
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static async IAsyncEnumerable<IEntry> EnumerateAsync(string basePath, [EnumeratorCancellation] CancellationToken ct = default)
		{
			var y = new Detail.SimpleYielder();

			var stack = new Stack<IEntry>();

			var currentDirPath = basePath;
			var currentDirRelPath = Filepath.Empty;

			do
			{
				#region Fill-Stack
				try
				{
					#if false
					// REJECT: こんな事をして列挙作業を別々にしても、却ってパフォーマンスは 2～3% くらい（ごくわずかだけど）下がる
					var filesAsync = Task.Run(() => Directory.EnumerateFiles(currentDirPath));
					var dirsAsync = Task.Run(() => Directory.EnumerateDirectories(currentDirPath));
					#endif

					foreach (var file in Directory.EnumerateFiles(currentDirPath))
					{
						if (y.Countup())
						{
							await y.YieldAsync(); // YieldAwaitable には ConfigureAwait() は存在しないらしい
							ct.ThrowIfCancellationRequested();
						}

						stack.Push(new File(file, Resolve(currentDirRelPath, file)));
					}

					foreach (var dir in Directory.EnumerateDirectories(currentDirPath))
					{
						if (y.Countup())
						{
							await y.YieldAsync();
							ct.ThrowIfCancellationRequested();
						}

						stack.Push(new EnterDir(dir, Resolve(currentDirRelPath, dir)));
					}
				}
				catch (Exception x)
				{
					stack.Push(new Error(new EnumerationException(currentDirPath, currentDirRelPath, x)));
				}
				#endregion

				while (stack.TryPop(out var someEntry))
				{
					yield return someEntry;

					if (someEntry.Reaction == FseventInternalReaction.LeaveParentDir)
					{
						#region Leave-Parent-Dir
						for (; ; )
						{
							if (! stack.TryPeek(out var e))
							{
								yield break;
							}

							if (e is LeaveDir)
							{
								break;
							}
							else
							{
								stack.Pop();
							}
						}
						#endregion
					}

					if (someEntry.Reaction != FseventInternalReaction.Advance)
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
			while (stack.Count >= 1);
		}


		static Filepath Resolve(Filepath relativeParentDir, string fullPath)
		{
			return relativeParentDir.Combine(Filepath.Parse(Path.GetFileName(fullPath)).Items);
		}


		/// <summary>(opaque item)</summary>
		public interface IEntry
		{
			/// <summary>(internal use only)</summary>
			FseventInternalReaction Reaction { get; }


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


			public FseventInternalReaction Reaction { get; internal set; } = FseventInternalReaction.Advance;


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
				Reaction = FseventInternalReaction.LeaveParentDir;
			}


			/// <summary>
			/// Command to skip entering into directory.
			/// </summary>
			public void Skip()
			{
				Reaction = FseventInternalReaction.SkipEnterDir;
			}
		}


		/// <summary>
		/// Leaving from a directory.
		/// </summary>
		public class LeaveDir : ISuccess
		{
			public string FullPathString { get; private set; }

			public Filepath RelativePath { get; private set; }

			public FseventInternalReaction Reaction { get; internal set; } = FseventInternalReaction.Advance;

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
				Reaction = FseventInternalReaction.LeaveParentDir;
			}
		}


		/// <summary>
		/// File found.
		/// </summary>
		public class File : ISuccess
		{
			public string FullPathString { get; private set; }

			public Filepath RelativePath { get; private set; }

			public FseventInternalReaction Reaction { get; internal set; } = FseventInternalReaction.Advance;

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
				Reaction = FseventInternalReaction.LeaveParentDir;
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
			public Exception? Exception { get; private set; }


			public FseventInternalReaction Reaction { get; internal set; } = FseventInternalReaction.Advance;


			/// <summary>
			/// (do not call directly)
			/// </summary>
			/// <param name="exception">thrown exception object</param>
			public Error(Exception exception)
			{
				Exception = exception;
			}


			public void LeaveParentDir()
			{
				Reaction = FseventInternalReaction.LeaveParentDir;
			}
		}


		/// <summary>
		/// Some error while enumerating file system items.
		/// Typically, this instance has InnerException for detail.
		/// </summary>
		public class EnumerationException : Exception
		{
			/// <summary>
			/// The directory path that tried to enumerate items. May be absolute, or may be relative.
			/// </summary>
			public string DirPathString { get; private set; }


			/// <summary>
			/// The relative path from Fsentry.EnumerateAsync(HERE) of the `DirPathString`. May be Empty.
			/// </summary>
			public Filepath DirRelativePath { get; private set; }


			public EnumerationException(string dirPath, Filepath relativePath, Exception innerException)
				: base($"File entry enumeration failed for dir {dirPath}", innerException)
			{
				DirPathString = dirPath;
				DirRelativePath = relativePath;
			}
		}

	}


	/// <summary>(internal use)</summary>
	public enum FseventInternalReaction
	{
		Advance = 0,

		LeaveParentDir = 1,

		SkipEnterDir = 2,
	}


#if false
	public class FsentryError : Fsentry
	{
		public Exception? Error { get; private set; } = null;


		public FsentryError(Exception? error = null)
		{
			Error = error;
		}
	}


	public abstract class FsentrySuccess : Fsentry
	{
		/// <summary>Raw result value from `System.IO.Directory.Enum***()` </summary>
		public string FullPathString { get; private set; } = string.Empty;

		public Filepath RelativePath { get; private set; } = Filepath.Empty;

		protected FsentrySuccess(string fullPath, Filepath relativeParentDir)
		{
			FullPathString = fullPath;
			RelativePath = relativeParentDir.Combine(Filepath.Parse(Path.GetFileName(fullPath)).Items);
		}

		/// <summary>
		/// (just copying)
		/// </summary>
		protected FsentrySuccess(FsentrySuccess entry)
		{
			FullPathString = entry.FullPathString;
			RelativePath = entry.RelativePath;
		}
	}


	/// <summary>
	/// A dir found, will enter.
	/// </summary>
	public class FsentryEnterDir : FsentrySuccess
	{
		public FsentryEnterDir(string fullPath, Filepath relativeParentDir)
				: base(fullPath, relativeParentDir)
		{
		}


		public void Skip()
		{
			mReaction = FseventInternalReaction.SkipEnterDir;
		}
	}


	/// <summary>
	/// Leaving the dir.
	/// </summary>
	public class FsentryLeaveDir : FsentrySuccess
	{
		public FsentryLeaveDir(FsentryEnterDir enterDir)
				: base(enterDir)
		{
		}
	}


	/// <summary>
	/// A file found.
	/// </summary>
	public class FsentryFile : FsentrySuccess
	{
		public FsentryFile(string fullPath, Filepath relativeParentDir)
				: base(fullPath, relativeParentDir)
		{
		}
	}
#endif
}

