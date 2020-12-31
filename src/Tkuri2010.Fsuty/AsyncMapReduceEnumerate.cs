using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

// 大体の場合、一つのタスクからは複数の結果を取得したい事が多いはず。
// 多数のサーバで分散処理する検索エンジンだって
// 一つのサーバに問い合わせるタスクからは、複数の検索結果が得られるはず。
// そのようにインタフェースを設計する。

namespace Tkuri2010.Fsuty
{
	public static class AsyncMapReduceEnumerate
	{
		public static IAsyncEnumerable<TResult> MapReduceAsync<TResult>(IAsyncTaskGenerator<TResult> mapper, CancellationToken ct = default)
		{
			return MapReduceAsync<TResult>(Task.Factory, mapper, ct);
		}

		public static async IAsyncEnumerable<TResult> MapReduceAsync<TResult>(TaskFactory taskFactory, IAsyncTaskGenerator<TResult> mapper, [EnumeratorCancellation] CancellationToken ct = default)
		{
			var mappingTaskFinish = 0;

			var reducerQueue = new Queue<ReducerHelper<TResult>>();

			var mappingTask = taskFactory.StartNew(async () =>
			{
				try
				{
					await foreach (var reducer in mapper.EnumerateAsync(ct))
					{
						var wrappedReducer = new ReducerHelper<TResult>(reducer);
						wrappedReducer.Start(taskFactory, ct);
						reducerQueue.Enqueue(wrappedReducer);
					}
				}
				finally
				{
					Interlocked.Increment(ref mappingTaskFinish);
				}
			});

			while (mappingTaskFinish == 0 || 1 <= reducerQueue.Count)
			{
				if (ct.IsCancellationRequested)
				{
					yield break;
				}

				if (! reducerQueue.TryDequeue(out var reducer))
				{
					await Task.Yield();
					continue;
				}

				try
				{
					await foreach (var result in reducer.EnumerateResultAsync(ct))
					{
						yield return result;
					}
				}
				finally
				{
					reducer.Dispose();
				}
			}
		}
	}


	public interface IAsyncTaskGenerator<out TResult>
	{
		IAsyncEnumerable<IAsyncReducer<TResult>> EnumerateAsync(CancellationToken ct);
	}


	public interface IAsyncReducer<out TResult>
	{
		IAsyncEnumerable<TResult> EnumerateResultAsync(CancellationToken ct);
	}


	internal class ReducerHelper<TResult> : IDisposable
	{
		internal Task? Task = null;

		internal IAsyncReducer<TResult> ReducerPayload;


		internal Queue<TResult> Q = new Queue<TResult>();


		int mTaskStarted = 0;


		int mTaskFinish = 0;


		int mDisposeCalled = 0;


		internal ReducerHelper(IAsyncReducer<TResult> payload)
		{
			ReducerPayload = payload;
		}


		internal void Start(TaskFactory taskFactory, CancellationToken ct)
		{
			Task = taskFactory.StartNew(async () =>
			{
				Interlocked.Increment(ref mTaskStarted);
				try
				{
					await CollectResultAsync(ct);
				}
				finally
				{
					Interlocked.Increment(ref mTaskFinish);
				}
			});
		}


		async Task CollectResultAsync(CancellationToken ct)
		{
			Func<bool> broken = () => ct.IsCancellationRequested || (1 <= mDisposeCalled);

			if (broken()) return;

			await foreach (var result in ReducerPayload.EnumerateResultAsync(ct))
			{
				Q.Enqueue(result);

				if (broken()) return;
			}
		}


		internal async IAsyncEnumerable<TResult> EnumerateResultAsync([EnumeratorCancellation] CancellationToken ct)
		{
			Func<bool> broken = () => ct.IsCancellationRequested || (1 <= mDisposeCalled);

			while (!broken() && mTaskStarted <= 0)
			{
				await Task.Yield();
				//Thread.Yield();
			}

			while (!broken() && (mTaskFinish == 0 || 1 <= Q.Count))
			{
				if (! Q.TryDequeue(out var result))
				{
					await Task.Yield();
					//Thread.Yield();
					continue;
				}

				yield return result;
			}
		}


		public void Dispose()
		{
			Interlocked.Increment(ref mDisposeCalled);
		}
	}
}