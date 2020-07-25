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
		None,
		Error,
		File,
		EnterDir,
		LeaveDir,
	}

	public enum FsentryCommand
	{
		Continue,

		SkipDirectory,
	}


	public class Fsentry
	{
		public static IAsyncEnumerable<Fsentry> VisitAsync(Filepath basePath, CancellationToken ct = default)
		{
			return VisitAsync(basePath.ToString(), ct);
		}


		public static async IAsyncEnumerable<Fsentry> VisitAsync(string basePath, [EnumeratorCancellation] CancellationToken ct = default)
		{
			var q = new DirsFilesStack();

			await q.EnumAndPushAsync(basePath, ct);

			while (q.HasMore)
			{
				var entry = q.Pop();

				yield return entry;

				if (entry.Event != FsentryEvent.EnterDir)
				{
					continue;
				}

				q.PushLeavingDir(entry.Path);

				if (entry.Command == FsentryCommand.Continue)
				{
					await q.EnumAndPushAsync(entry.Path, ct);
				}
				else // skip requested
				{
					// nop. skip.
				}
			}
		}


		public FsentryEvent Event { get; private set; }

		public string Path { get; private set; }

		public FsentryCommand Command  { get; set; } = FsentryCommand.Continue;

		internal Fsentry(FsentryEvent ev, string rawResultPathString)
		{
			Event = ev;
			Path = rawResultPathString;
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

		internal void PushLeavingDir(string dirStr)
		{
			mStack.Push(new Fsentry(FsentryEvent.LeaveDir, dirStr));
		}

		internal Task EnumAndPushAsync(string searchPathStr, CancellationToken ct)
		{
			ct.ThrowIfCancellationRequested();

			foreach (var file in Directory.EnumerateFiles(searchPathStr))
			{
				mStack.Push(new Fsentry(FsentryEvent.File, file));
				ct.ThrowIfCancellationRequested();
			}

			foreach (var dir in Directory.EnumerateDirectories(searchPathStr))
			{
				mStack.Push(new Fsentry(FsentryEvent.EnterDir, dir));
				ct.ThrowIfCancellationRequested();
			}

			return Task.CompletedTask;
		}
	}
}