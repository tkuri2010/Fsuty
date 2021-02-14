using System.Threading;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Linq;
using System.IO;
using System;
using System.Threading.Tasks;

namespace Tkuri2010.Fsuty
{
	public enum FsentryEvent
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

	public enum FsentryCommand
	{
		Advance,

		SkipDirectory,
	}


	public class Fsentry
	{
		/// <summary>
		/// visits the directory, recursivery
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static IAsyncEnumerable<Fsentry> VisitAsync(Filepath basePath, CancellationToken ct = default)
		{
			return VisitAsync(basePath.ToString(), ct);
		}


		/// <summary>
		/// visits the directory, recursivery
		/// </summary>
		/// <param name="basePath"></param>
		/// <param name="ct"></param>
		/// <returns></returns>
		public static async IAsyncEnumerable<Fsentry> VisitAsync(string basePath, [EnumeratorCancellation] CancellationToken ct = default)
		{
			var q = new DirsFilesStack();

			await q.EnumAndPushAsync(basePath, Filepath.Empty, ct);

			while (q.HasMore)
			{
				var entry = q.Pop();

				yield return entry;

				if (entry.Event != FsentryEvent.EnterDir)
				{
					continue;
				}

				q.PushLeavingDir(entry);

				if (entry.Command == FsentryCommand.Advance)
				{
					var name = Filepath.Parse(Path.GetFileName(entry.FullPathString));
					var relativeDir = entry.RelativeParent.Combine(name.Items);
					await q.EnumAndPushAsync(entry.FullPathString, relativeDir, ct);
				}
				else // skip requested
				{
					// nop. skip.
				}
			}
		}


		public FsentryEvent Event { get; private set; }

		public string FullPathString { get; private set; }

		public Filepath RelativeParent { get; private set; }

		public FsentryCommand Command  { get; set; } = FsentryCommand.Advance;

		internal Fsentry(FsentryEvent ev, string rawFullPathString, Filepath relativeParentDir)
		{
			Event = ev;
			FullPathString = rawFullPathString;
			RelativeParent = relativeParentDir;
		}
	}


	class DirsFilesStack
	{
		internal DirsFilesStack()
		{
		}

		Stack<Fsentry> mStack = new Stack<Fsentry>();

		internal bool HasMore => (1 <= mStack.Count);

		internal bool TryPop(out Fsentry rv)
		{
			return mStack.TryPop(out rv);
		}

		internal Fsentry Pop()
		{
			return mStack.Pop();
		}

		internal void PushLeavingDir(Fsentry dirEntry)
		{
			mStack.Push(new Fsentry(FsentryEvent.LeaveDir, dirEntry.FullPathString, dirEntry.RelativeParent));
		}

		internal Task EnumAndPushAsync(string searchPathStr, Filepath relativeHereDir, CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();

			foreach (var file in Directory.EnumerateFiles(searchPathStr))
			{
				mStack.Push(new Fsentry(FsentryEvent.File, file, relativeHereDir));
				ct.ThrowIfCancellationRequested();
			}

			foreach (var dir in Directory.EnumerateDirectories(searchPathStr))
			{
				mStack.Push(new Fsentry(FsentryEvent.EnterDir, dir, relativeHereDir));
				ct.ThrowIfCancellationRequested();
			}

			return Task.CompletedTask;
		}
	}
}