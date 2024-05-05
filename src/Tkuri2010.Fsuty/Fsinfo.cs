using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;

namespace Tkuri2010.Fsuty
{
	/// <summary>
	/// enumerates DirectoryInfo or FileInfo.
	/// </summary>
	public class Fsinfo
	{
		public interface IInfo
		{
			FseventInternalReaction Reaction { get; }

			void LeaveParentDir();
		}


		public interface ISuccess : IInfo
		{
		}


		public class EnterDir : ISuccess
		{
			public DirectoryInfo Info { get; private set; }

			public FseventInternalReaction Reaction { get; private set; } = FseventInternalReaction.Advance;

			public EnterDir(DirectoryInfo info)
			{
				Info = info;
			}

			public void LeaveParentDir()
			{
				Reaction = FseventInternalReaction.EscapeParentDir;
			}


			public void Skip()
			{
				Reaction = FseventInternalReaction.SkipEnterDir;
			}
		}


		public class LeaveDir : ISuccess
		{
			public DirectoryInfo Info { get; private set; }

			public FseventInternalReaction Reaction { get; private set; } = FseventInternalReaction.Advance;

			public LeaveDir(EnterDir enterDir)
			{
				Info = enterDir.Info;
			}

			public void LeaveParentDir()
			{
				Reaction = FseventInternalReaction.EscapeParentDir;
			}
		}


		public class File : ISuccess
		{
			public FileInfo Info { get; private set; }

			public FseventInternalReaction Reaction { get; private set; } = FseventInternalReaction.Advance;

			public File(FileInfo info)
			{
				Info = info;
			}

			public void LeaveParentDir()
			{
				Reaction = FseventInternalReaction.EscapeParentDir;
			}
		}


		public class Error : IInfo
		{
			public Exception? Exception { get; private set; }

			public FseventInternalReaction Reaction { get; private set; } = FseventInternalReaction.Advance;


			public Error(Exception? exception)
			{
				Exception = exception;
			}


			public void LeaveParentDir()
			{
				Reaction = FseventInternalReaction.EscapeParentDir;
			}
		}


		public class EnumerationException : Exception
		{
			public DirectoryInfo CurrentDirectoryInfo { get; private set; }


			public EnumerationException(DirectoryInfo info, Exception innerException)
				: base($"File info enumeration failed for dir {info.FullName}", innerException)
			{
				CurrentDirectoryInfo = info;
			}
		}


		/// <summary>
		/// enumerates all file system entries as DirectoryInfo or FileInfo, recursivery
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static IAsyncEnumerable<IInfo> EnumerateAsync(Filepath basePath, CancellationToken ct = default)
		{
			return EnumerateAsync(basePath.ToString(), ct);
		}


		/// <summary>
		/// enumerates all file system entries as DirectoryInfo or FileInfo, recursivery
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static IAsyncEnumerable<IInfo> EnumerateAsync(string basePath, CancellationToken ct = default)
		{
			return EnumerateAsync(new DirectoryInfo(basePath), ct);
		}


		public static async IAsyncEnumerable<IInfo> EnumerateAsync(DirectoryInfo baseDir, [EnumeratorCancellation] CancellationToken ct = default)
		{
			var y = new Detail.SimpleYielder();

			var stack = new Stack<IInfo>();

			var currentDirInfo = baseDir;

			do
			{
				#region Fill-Stack
				try
				{
					foreach (var fileInfo in currentDirInfo.EnumerateFiles())
					{
						if (y.Countup())
						{
							await y.YieldAsync();
							ct.ThrowIfCancellationRequested();
						}

						stack.Push(new File(fileInfo));
					}

					foreach (var dirInfo in currentDirInfo.EnumerateDirectories())
					{
						if (y.Countup())
						{
							await y.YieldAsync();
							ct.ThrowIfCancellationRequested();
						}

						stack.Push(new EnterDir(dirInfo));
					}
				}
				catch (Exception x)
				{
					stack.Push(new Error(new EnumerationException(currentDirInfo, x)));
				}
				#endregion

				while (stack.TryPop(out var someInfo))
				{
					yield return someInfo;

					if (someInfo.Reaction == FseventInternalReaction.EscapeParentDir)
					{
						#region  Leave-Parent-Dir
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

					if (someInfo.Reaction != FseventInternalReaction.Advance)
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
			while (stack.Count >= 1);
		}
	}
}
