using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Tkuri2010.Fsuty
{

	public class Fsinfo
	{
		/// <summary>
		/// enumerates all file system entries as DirectoryInfo or FileInfo, recursivery
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static IAsyncEnumerable<Fsinfo> EnumerateAsync(Filepath basePath, CancellationToken ct = default)
		{
			return EnumerateAsync(basePath.ToString(), ct);
		}


		/// <summary>
		/// enumerates all file system entries as DirectoryInfo or FileInfo, recursivery
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static IAsyncEnumerable<Fsinfo> EnumerateAsync(string basePath, CancellationToken ct = default)
		{
			return EnumerateAsync(new DirectoryInfo(basePath), ct);
		}


		/// <summary>
		/// enumerates all file system entries as DirectoryInfo or FileInfo, recursivery
		/// </summary>
		/// <param name="baseDirInfo"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static async IAsyncEnumerable<Fsinfo> EnumerateAsync(DirectoryInfo baseDirInfo, [EnumeratorCancellation] CancellationToken ct = default)
		{
			var y = new Detail.SimpleYielder();

			var stack = new Stack<Fsinfo>();
			stack.Push(new(baseDirInfo));
			var isFirst = true;

			var currDirInfo = baseDirInfo;

			while (1 <= stack.Count)
			{
				var e = stack.Pop();
				if (isFirst)
				{
					isFirst = false;
				}
				else
				{
					yield return e;

					if (! e.WhenEnterDir(out var dirInfo))
					{
						continue;
					}
					currDirInfo = dirInfo;

					stack.Push(new Fsinfo(currDirInfo, asLeaving: true));

					if (e.Command != Fscommand.Advance)
					{
						continue;
					}
				}

				try
				{
					foreach (var fileInfo in currDirInfo.EnumerateFiles())
					{
						if (y.Countup()) await y.YieldAsync();

						stack.Push(new(fileInfo));

						ct.ThrowIfCancellationRequested();
					}

					foreach (var dirInfo in currDirInfo.EnumerateDirectories())
					{
						if (y.Countup()) await y.YieldAsync();

						stack.Push(new(dirInfo));

						ct.ThrowIfCancellationRequested();
					}
				}
				catch (Exception x)
				{
					stack.Push(new(x, currDirInfo));
				}
			}
		}


		public bool WhenEnterDir([NotNullWhen(true)] out DirectoryInfo? dirInfo)
		{
			dirInfo = (Event == Fsevent.EnterDir) ? mDirInfo : null;
			return (dirInfo is not null);
		}


		public bool WhenLeaveDir([NotNullWhen(true)] out DirectoryInfo? dirInfo)
		{
			dirInfo = (Event == Fsevent.LeaveDir) ? mDirInfo : null;
			return (dirInfo is not null);
		}


		public bool WhenFile([NotNullWhen(true)] out FileInfo? fileInfo)
		{
			fileInfo = (Event == Fsevent.File) ? mFileInfo : null;
			return (fileInfo is not null);
		}


		public bool WhenError(
				[NotNullWhen(true)] out Exception? exception,
				[NotNullWhen(true)] out DirectoryInfo? dirInfo)
		{
			exception = (Event == Fsevent.Error) ? mException : null;
			dirInfo = (Event == Fsevent.Error) ? mDirInfo : null;

			return (exception is not null) && (dirInfo is not null);
		}


		public Fsevent Event { get; private set; } = Fsevent.None;


		public Fscommand Command { get; set; } = Fscommand.Advance;


		DirectoryInfo? mDirInfo = null;


		FileInfo? mFileInfo = null;


		Exception? mException = null;


		internal Fsinfo(DirectoryInfo dirInfo, bool asLeaving = false)
		{
			Event = asLeaving ? Fsevent.LeaveDir : Fsevent.EnterDir;
			mDirInfo = dirInfo;
		}


		internal Fsinfo(FileInfo fileInfo)
		{
			Event = Fsevent.File;
			mFileInfo = fileInfo;
		}


		internal Fsinfo(Exception exception, DirectoryInfo dirInfo)
		{
			Event = Fsevent.Error;
			mException = exception;
			mDirInfo = dirInfo;
		}
	}
}
