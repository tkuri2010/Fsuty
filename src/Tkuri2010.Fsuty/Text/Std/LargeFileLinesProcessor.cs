using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Tkuri2010.Fsuty.Text.Std
{

	public class ByteArray
	{
		private static readonly byte[] _EmptyBytes = new byte[0];


		public static readonly ByteArray Empty = new ByteArray();


		/// <summary>
		/// バイト配列本体
		/// </summary>
		public byte[] Body = _EmptyBytes;

		/// <summary>
		/// Body中の有効なデータのサイズ
		/// </summary>
		public int Count = 0;

		public byte[] RequireSize(int size)
		{
			if (Body.Length < size)
			{
				Count = 0;
				Body = new byte[size];
			}

			return Body;
		}


		override public string ToString()
		{
			return ToString(Encoding.Default);
		}


		public string ToString(Encoding encoding)
		{
			return encoding.GetString(Body, 0, Count);
		}
	}


	public class LineInfo<TResult>
	{
		static readonly ResultNo<TResult> _CahcedNo = new ResultNo<TResult>();

		public ByteArray LineBytes { get; set; } = ByteArray.Empty;


		public Result<TResult> No()
		{
			return _CahcedNo;
		}


		public Result<TResult> Ok(TResult val)
		{
			return new ResultOk<TResult>(val);
		}
	}


	public delegate Result<TResult> ProcessingFunc<TResult>(LineInfo<TResult> lineInfo);


	/// <summary>
	/// FIXME: Result という名前は一般的すぎてここで使いたくない。どうしよう。。
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class Result<T>
	{
		public abstract bool IsOk { get; }

		public long LineNumber => LocalLineNumber + (Reducer?.PreviousTotalLineCount ?? 0);

		internal long LocalLineNumber = -1;

		internal Lfdetail.LfReducer<T>? Reducer = null;

		public abstract T Value { get; }

		public static implicit operator Result<T>(T val) => new ResultOk<T>(val);
	}


	internal class ResultNo<T> : Result<T>
	{
		public override bool IsOk => false;

		public override T Value => throw new NotImplementedException();
	}


	internal class ResultOk<T> : Result<T>
	{
		public override bool IsOk => true;

		public override T Value { get; }

		internal ResultOk(T val)
		{
			Value = val;
		}
	}


	public class LargeFileLinesProcessor
	{
		public static IAsyncEnumerable<Result<T>> ProcessAsync<T>(string filePath, ProcessingFunc<T> processingFunc, CancellationToken ct = default)
		{
			return ProcessAsync(
					LargeFileLinesProcessorSettings.Default,
					filePath,
					processingFunc,
					ct
			);
		}


		public static async IAsyncEnumerable<Result<T>> ProcessAsync<T>(LargeFileLinesProcessorSettings settings, string filePath, ProcessingFunc<T> processingFunc, [EnumeratorCancellation] CancellationToken ct = default)
		{
			await using var mapper = new Lfdetail.LfMapper<T>(filePath, settings.RoughChunkSize, processingFunc);
			await foreach (var result in AsyncMapReduceEnumerate.MapReduceAsync(settings.TaskFactory, mapper, ct))
			{
				yield return result;
			}
		}
	}
}

namespace Tkuri2010.Fsuty.Text.Std.Lfdetail
{
	public interface IReadable
	{
		long Length { get;}


		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"></param>
		/// <param name="buffer"></param>
		/// <param name="count"></param>
		/// <returns>読み取ったサイズ。読み取れなかった場合はゼロ</returns>
		int ReadBuffer(long position, ByteArray buffer, int count);


		/// <summary>
		/// 
		/// </summary>
		/// <param name="position"></param>
		/// <returns></returns>
		byte ReadByte(long position);
	}


	public class MemFileViewStrm : IReadable
	{
		public MemoryMappedViewStream Payload { get; private set; }

		public long Length { get; private set ;}


		MmvsCache mCache;

		public MemFileViewStrm(MemoryMappedViewStream mmvs, long length)
		{
			Payload = mmvs;
			Length = length;
			mCache = new MmvsCache();
		}


		public int ReadBuffer(long position, ByteArray buffer, int count)
		{
			if (Payload.Position != position) Payload.Position = position;
			var rv = Payload.Read(buffer.RequireSize(count), 0, count);
			buffer.Count = rv;
			return rv;
		}


		public byte ReadByte(long position)
		{
			return (byte)mCache.ReadEx(Payload, position);
		}
	}


	class MmvsCache
	{
		byte[] Cacheds;

		long CachedPosStart = 0;

		long CachedPosOver = 0;

		bool HasCache(long pos) => CachedPosStart <= pos && pos < CachedPosOver;


		internal MmvsCache() : this(24 * 1024)
		{
		}


		internal MmvsCache(int cacheableSize)
		{
			Cacheds = new byte[cacheableSize];
		}


		internal async Task CacheAsync(MemoryMappedViewStream stream, long start)
		{
			stream.Position = start;
			var readSize = await stream.ReadAsync(Cacheds, 0, Cacheds.Length);
			CachedPosStart = start;
			CachedPosOver = start + readSize;
		}


		internal void Cache(MemoryMappedViewStream stream, long start)
		{
			stream.Position = start;
			var readSize = stream.Read(Cacheds, 0, Cacheds.Length);
			CachedPosStart = start;
			CachedPosOver = start + readSize;
		}


		internal int Read(long position)
		{
			return HasCache(position) ? Cacheds[position - CachedPosStart] : -1;
		}


		internal int ReadEx(MemoryMappedViewStream stream, long position)
		{
			if (!HasCache(position))
			{
				Cache(stream, position);
			}
			return Read(position);
		}
	}


	/// <summary>
	/// 引数の MemoryMappedViewAccessor は Dispose されない。外部で Dispose すること。
	/// </summary>
	public class MemFileAccess : IReadable
	{
		public MemoryMappedViewAccessor Payload { get; private set; }

		public long Length { get; private set; }

		public MemFileAccess(MemoryMappedViewAccessor access, long length)
		{
			Payload = access;
			Length = length;
		}


		public int ReadBuffer(long position, ByteArray buffer, int count)
		{
			var rv = Payload.ReadArray(position, buffer.RequireSize(count), 0, count);
			buffer.Count = rv;
			return rv;
		}


		public byte ReadByte(long position)
		{
			return Payload.ReadByte(position);
		}
	}


	/// <summary>
	/// (ユニットテスト用途) 単純なバイト配列に対して IReadable インタフェースをラップ
	/// </summary>
	public class SimpleReadableBytes : IReadable
	{
		byte[] mByteArray;


		public long Length => mByteArray.Length;


		public SimpleReadableBytes(byte[] byteArray)
		{
			mByteArray = byteArray;
		}


		public int ReadBuffer(long position, ByteArray buffer, int count)
		{
			if (position < 0 || mByteArray.Length < position || count < 0)
			{
				throw new ArgumentOutOfRangeException($"available bytes length = {mByteArray.Length}, argument pos={position}, count={count}");
			}
			if (mByteArray.Length == position || count == 0)
			{
				return 0;
			}

			var availableBytes = mByteArray.Length - position;
			var copyCount = (int)Math.Min(availableBytes, count);
			Array.Copy(mByteArray, (int)position, buffer.RequireSize(count), 0, copyCount);
			buffer.Count = copyCount;
			return copyCount;
		}


		public byte ReadByte(long position)
		{
			return mByteArray[position];
		}
	}


	/// <summary>
	/// IReadable インタフェースを実装したオブジェクトを受け取り、行の単位で列挙するロジック
	/// </summary>
	public class BasicLineEnumerator
	{
		public static TimeSpan elaps = TimeSpan.Zero;

		public IEnumerable<ByteArray> Enumerate(IReadable readable)
		{
			var watch = new System.Diagnostics.Stopwatch();
			//watch.Start();
			try
			{
			var buffer = new ByteArray();
			long offset = 0;
			long pos = 0; // 現在注視している場所
			for (; ; )
			{
				watch.Start();
				pos = FindLineEnd(readable, pos);
				watch.Stop();
				var size = pos - offset;
				if (size <= 0) break;
				readable.ReadBuffer(offset, buffer, (int)size);
				yield return buffer;
				offset = pos;
			}
			}
			finally
			{
				//watch.Stop();
				elaps += watch.Elapsed;
			}
		}


		/// <summary>
		/// '\n' の次、またはバッファの終わりまで走査
		/// </summary>
		/// <param name="readable"></param>
		/// <param name="offset"></param>
		/// <returns></returns>
		public long FindLineEnd(IReadable readable, long offset)
		{
			long L = readable.Length;
			long p = offset;

			while (p < L)
			{
				if (readable.ReadByte(p) == '\n')
				{
					return p + 1;
				}

				p++;
			}

			return p;
		}
	}


	public class DisposableChunk : IDisposable
	{
		//public MemoryMappedViewAccessor Access;
		public MemoryMappedViewStream Stream;

		/// <summary>
		/// 元のメモリマップトファイルの、何バイト目からを切り出したかを保持しているが
		/// 切り出した後は有用に使う事はなさそう？
		/// </summary>
		public long Offset;

		public long Size;

		public bool IsLastChunk;


		internal DisposableChunk(MemoryMappedViewStream strm, long offset, long size, bool isLast)
		{
			Offset = offset;
			Size = size;
			Stream = strm;
			IsLastChunk = isLast;
		}


		public void Dispose()
		{
			Stream.Dispose();
		}
	}


	public class LargeFileChunkEnumerator
	{
		//private LargeFileLinesProcessorSettings mSettings;
		long mRoughChunkSize;

		public LargeFileChunkEnumerator(long roughChunkSize)
		{
			mRoughChunkSize = roughChunkSize;
		}

		public IEnumerable<DisposableChunk> Enumerate(string filePath)
		{
			int LOOP_MAX = 8 * 1024;
			var safe = 0;

			var fileInfo = new FileInfo(filePath);
			var totalSize = fileInfo.Length;

			using var mm = MemoryMappedFile.CreateFromFile(filePath, FileMode.Open);

			long offset = 0;
			while (++safe < LOOP_MAX)
			{
				//_Debug($"loop: off:{offset} / chunk size:{chunkSize}");

				var chunk = FindChunk(mm, offset, totalSize);

				yield return chunk;

				if (chunk.IsLastChunk)
				{
					break; // ok, exit.
				}

				offset += chunk.Size;
			}

			if (safe < LOOP_MAX)
			{
				// ok
			}
			else
			{
				// 全部処理しきれていない
			}
		}


		DisposableChunk FindChunk(MemoryMappedFile mm, long offset, long fileTotalSize)
		{
			var localOffset = 0L;
			var chunkSize = mRoughChunkSize;
			for (var trial = 0; trial < 1024; trial++)
			{
				// これでファイルの終わりに到達するか？
				var isLastChunk = (fileTotalSize <= offset + chunkSize);
				if (isLastChunk)
				{
					var lastChunkSize = fileTotalSize - offset;
					//var lastChunk = mm.CreateViewAccessor(offset, lastChunkSize, MemoryMappedFileAccess.Read);
					var lastChunk = mm.CreateViewStream(offset, lastChunkSize, MemoryMappedFileAccess.Read);
					return new DisposableChunk(lastChunk, offset, lastChunkSize, isLastChunk);
				}

				//MemoryMappedViewAccessor? chunk = null;
				MemoryMappedViewStream? chunk = null;
				bool chunkShouldBeDisposed = true;
				try
				{
					//chunk = mm.CreateViewAccessor(offset, chunkSize, MemoryMappedFileAccess.Read);
					chunk = mm.CreateViewStream(offset, chunkSize, MemoryMappedFileAccess.Read);
					// LF を探す
					var lastLfPos = FindLastByte(chunk, localOffset, chunkSize, (byte)'\n');

					if (0 <= lastLfPos)
					{
						chunkShouldBeDisposed = false;
						return new DisposableChunk(chunk, offset, lastLfPos + 1, false);
					}
				}
				finally
				{
					if (chunkShouldBeDisposed) chunk?.Dispose();
				}

				localOffset = chunkSize;
				chunkSize += (mRoughChunkSize / 2); // 適当に増やす。。。どう増やそうか？
			}

			throw new System.Exception("Line separator not found. Too long line exists?");
		}


		//static long FindLastByte(MemoryMappedViewAccessor mm, long minPos, long maxSize, byte target)
		static long FindLastByte(MemoryMappedViewStream mm, long minPos, long maxSize, byte target)
		{
			for (long p = maxSize - 1; p >= minPos; p--)
			{
				mm.Position = p;
				if (target == mm.ReadByte())
				{
					return p;
				}
			}
			return -1;
		}

		static void _Debug(object o)
		{
			System.Console.WriteLine(o);
		}
	}


	public class LfMapper<T> : IAsyncTaskGenerator<Result<T>>, IAsyncDisposable
	{
		string mFileName;

		long mRoughChunkSize;

		ProcessingFunc<T> mProcessingFunc;

		List<LfReducer<T>> mTasks = new List<LfReducer<T>>();

		int mLoopProcessing = 0;

		int mDisposeCalledCount = 0;


		public LfMapper(string fileName, long roughChunkSize, ProcessingFunc<T> processingFunc)
		{
			mFileName = fileName;
			mRoughChunkSize = roughChunkSize;
			mProcessingFunc = processingFunc;
		}


		public async IAsyncEnumerable<IAsyncReducer<Result<T>>> EnumerateAsync([EnumeratorCancellation] CancellationToken ct)
		{
			try
			{
				Interlocked.Increment(ref mLoopProcessing);

				LfReducer<T>? lastReducer = null;

				var chunkEnum = new LargeFileChunkEnumerator(mRoughChunkSize);

				foreach (var c in chunkEnum.Enumerate(mFileName))
				{
					Action disposeLater = () => c.Dispose();

					if (1 <= mDisposeCalledCount)
					{
						disposeLater.Invoke();
						break;
					}

					if (ct.IsCancellationRequested)
					{
						disposeLater.Invoke();
						break;
					}

					//var mem = new MemFileAccess(c.Access, c.Size);
					var mem = new MemFileViewStrm(c.Stream, c.Size);
					var t = new LfReducer<T>(lastReducer, mem, mProcessingFunc, disposeLater);
					mTasks.Add(t);
					yield return t;
					lastReducer = t;
				}
			}
			finally
			{
				Interlocked.Decrement(ref mLoopProcessing);
			}
		}


		#region disposing

		public async ValueTask DisposeAsync()
		{
			Interlocked.Increment(ref mDisposeCalledCount);

			int safeCount = 0;
			while (1 <= mLoopProcessing)
			{
				await Task.Yield();
				if (100 < safeCount++) break;
			}

			foreach (var t in mTasks)
			{
				await t.DisposeAsync();
			}
		}

		#endregion
	}


	public class LfReducer<T> : IAsyncReducer<Result<T>>, IAsyncDisposable
	{
		long LocalTotalLineCount = 0;

		/// <summary>
		/// 本reducerまでに存在した行の行数を、遅延して計算したい
		/// </summary>
		LfReducer<T>? mPrevReducer = null;

		long TotalLineCountUntilHere => LocalTotalLineCount + PreviousTotalLineCount;

		internal long PreviousTotalLineCount
		{
			get
			{
				if (mPreviousTotalLineCountCache < 0)
				{
					mPreviousTotalLineCountCache = mPrevReducer?.TotalLineCountUntilHere ?? 0;
				}
				return mPreviousTotalLineCountCache;
			}
		}

		long mPreviousTotalLineCountCache = -1;

		int mDisposeCalledCount = 0;

		IReadable mReadable;

		ProcessingFunc<T> mProcessingFunc;

		Action mDisposeProcess;

		public LfReducer(LfReducer<T>? prevReducer, IReadable readable, ProcessingFunc<T> processingFunc, Action disposeProcess)
		{
			mPrevReducer = prevReducer;
			mReadable = readable;
			mProcessingFunc = processingFunc;
			mDisposeProcess = disposeProcess;
		}


		public async IAsyncEnumerable<Result<T>> EnumerateResultAsync([EnumeratorCancellation] CancellationToken ct)
		{
			var e = new BasicLineEnumerator();
			var lineInfo = new LineInfo<T>();
			foreach (var line in e.Enumerate(mReadable))
			{
				LocalTotalLineCount++;

				if (1 <= mDisposeCalledCount)
				{
					break;
				}
				if (ct.IsCancellationRequested)
				{
					break;
				}

				lineInfo.LineBytes = line;
				var r = mProcessingFunc.Invoke(lineInfo);
				if (r.IsOk)
				{
					r.Reducer = this;
					r.LocalLineNumber = LocalTotalLineCount;
					yield return r;
				}
			}
		}

		#region disposing

		public ValueTask DisposeAsync()
		{
			Interlocked.Increment(ref mDisposeCalledCount);

			mPrevReducer = null;

			mDisposeProcess.Invoke();

			return new ValueTask();
		}

		#endregion
	}
}

