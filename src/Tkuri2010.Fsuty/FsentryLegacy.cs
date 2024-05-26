using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.IO;
using System;


namespace Tkuri2010.Fsuty
{
	[Obsolete]
	public enum Fsevent
	{
		/// <summary>
		/// (not used)
		/// </summary>
		None,
		/// <summary>
		/// (not used)
		/// </summary>
		Error,
		/// <summary>
		/// a file found
		/// </summary>
		File,
		/// <summary>
		/// a directory found, enter
		/// </summary>
		EnterDir,
		/// <summary>
		/// leaving from a directory
		/// </summary>
		LeaveDir,
	}


	[Obsolete]
	public enum Fscommand
	{
		Advance,

		SkipDirectory,
	}


	[Obsolete]
	public class FsentryLegacy
	{
		/// <summary>
		/// enumerates all file system entries, recursivery
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static IAsyncEnumerable<FsentryLegacy> EnumerateAsync(Filepath basePath, CancellationToken ct = default)
		{
			return EnumerateAsync(basePath.ToString(), ct);
		}


		/// <summary>
		/// enumerates all file system entries, recursivery
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static async IAsyncEnumerable<FsentryLegacy> EnumerateAsync(string basePath, [EnumeratorCancellation] CancellationToken ct = default)
		{
			var y = new Detail.SimpleYielder();

			var stack = new Stack<FsentryLegacy>();
			stack.Push(AsFirstEntry(basePath));
			var isFirst = true;

			while (1 <= stack.Count)
			{
				var entry = stack.Pop();
				if (isFirst)
				{
					isFirst = false;
				}
				else
				{
					yield return entry;

					if (entry.Event != Fsevent.EnterDir)
					{
						continue;
					}

					stack.Push(AsLeavingDir(entry));

					if (entry.Command != Fscommand.Advance)
					{
						continue;
					}
				}

				try
				{
					foreach (var file in Directory.EnumerateFiles(entry.FullPathString))
					{
						if (y.Countup()) await y.YieldAsync();

						stack.Push(new FsentryLegacy(Fsevent.File, file, entry.RelativePath));

						ct.ThrowIfCancellationRequested();
					}

					foreach (var dir in Directory.EnumerateDirectories(entry.FullPathString))
					{
						if (y.Countup()) await y.YieldAsync();

						stack.Push(new FsentryLegacy(Fsevent.EnterDir, dir, entry.RelativePath));

						ct.ThrowIfCancellationRequested();
					}
				}
				catch (Exception x)
				{
					stack.Push(new FsentryLegacy(x, entry.FullPathString, entry.RelativePath));
				}
			}
		}


		public Fsevent Event { get; private set; } = Fsevent.None;

		/// <summary>Raw result value from `System.IO.Directory.Enum***()` </summary>
		public string FullPathString { get; private set; } = string.Empty;

		public Filepath RelativePath { get; private set; } = Filepath.Empty;

		public Fscommand Command  { get; set; } = Fscommand.Advance;

		public Exception? Exception { get; private set; } = null;


		internal FsentryLegacy()
		{
		}


		internal FsentryLegacy(Fsevent ev, string rawFullPathString, Filepath relativeParentDir)
		{
			Event = ev;
			FullPathString = rawFullPathString;
			RelativePath = relativeParentDir.Combine(Filepath.Parse(Path.GetFileName(rawFullPathString)).Items);
		}


		internal FsentryLegacy(Exception x, string currentFullPathString, Filepath currentDir)
		{
			Event = Fsevent.Error;
			FullPathString = currentFullPathString;
			RelativePath = currentDir;
			Exception = x;
		}


		// 初回の列挙のための特別なインスンタンス。取り扱い注意!
		internal static FsentryLegacy AsFirstEntry(string basePath)
		{
			return new()
			{
				FullPathString = basePath, // プロパティ名に反して、これは相対パスかも知れない。本インスタンスの取り扱いには注意!
				RelativePath = Filepath.Empty,
			};
		}


		internal static FsentryLegacy AsLeavingDir(FsentryLegacy enteringDir)
		{
			// assert: enteringDir.Event == FsentryEvent.EnterDir

			return new()
			{
				Event = Fsevent.LeaveDir,
				FullPathString = enteringDir.FullPathString,
				RelativePath = enteringDir.RelativePath,
			};
		}
	}

}