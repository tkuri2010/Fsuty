using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
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
	}


	public class LineInfo
	{
		public ByteArray LineBytes { get; set; } = ByteArray.Empty;
	}


	/// <summary>
	/// FIXME: Result という名前は一般的すぎてここで使いたくない。どうしよう。。
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class Result<T>
	{
		static readonly ResultNo<T> _CahcedNo = new ResultNo<T>();

		public static Result<T> No()
		{
			return _CahcedNo;
		}


		public static Result<T> Ok(T val)
		{
			return new ResultOk<T>(val);
		}


		public abstract bool IsOk { get; }

		public long LineNumber { get; set; } = -1;

		public abstract T Value { get; }
	}


	class ResultNo<T> : Result<T>
	{
		public override bool IsOk => false;

		public override T Value => throw new NotImplementedException();
	}


	class ResultOk<T> : Result<T>
	{
		public override bool IsOk => true;

		public override T Value { get; }

		public ResultOk(T val)
		{
			Value = val;
		}
	}


	public class LargeFileLinesProcessor<T> : IDisposable
	{
		LargeFileLinesProcessorSettings mSettings = LargeFileLinesProcessorSettings.Default;

		string mFilePath;

		Func<LineInfo, Result<T>> mProcess;

		LinkedList<Lfdetail.Chunk>? mDisposableChunkList = null;


		public LargeFileLinesProcessor(LargeFileLinesProcessorSettings settings, string filePath, Func<LineInfo, Result<T>> process)
		{
			mSettings = settings;
			mFilePath = filePath;
			mProcess = process;
		}


		public LargeFileLinesProcessor(string filePath, Func<LineInfo, Result<T>> process)
		{
			mFilePath = filePath;
			mProcess = process;
		}


		public async IAsyncEnumerable<Result<T>> ProcessAsync()
		{
			var chunksEnumerator = new Lfdetail.LargeFileChunkEnumerator(mSettings);

			mDisposableChunkList = new LinkedList<Lfdetail.Chunk>(chunksEnumerator.Enumerate(mFilePath));

			// ToList() を実行する事で、全てのタスク実行開始を確実にする。
			// （実際には、タスクスケジューラの都合によっては数個ずつしかタスクが始まらないかも知れないが
			//   ここでは問題にならない。以下で await などをしているが、その間でもいつかタスクを初めてくれればよい）
			var tasks = mDisposableChunkList.Select(chunk => StartProcess(chunk)).ToList();

			long lineNumOffset = 0;

			foreach (var task in tasks)
			{
				var chunkProcessor = await task;

				foreach (var rv in chunkProcessor.ResultList)
				{
					rv.LineNumber += lineNumOffset;
					yield return rv;
				}

				lineNumOffset += chunkProcessor.LineNumInChunk;
			}
		}


		Task<Lfdetail.ChunkProcessor<T>> StartProcess(Lfdetail.Chunk chunk)
		{
			// IEnumerable は、実際に値が必要とされるまで処理を遅延させられる。
			// 遅延を避けるため、ToList() を明示的に呼び出している。
			return mSettings.TaskFactory.StartNew(() =>
			{
				var cp = new Lfdetail.ChunkProcessor<T>();
				cp.ProcessChunk(new Lfdetail.MemFileAccess(chunk.Access, chunk.Size), mProcess);
				return cp;
			});
		}


		#region disposing

		bool disposedValue = false;

		public void Dispose()
		{
			if (!disposedValue)
			{
				var m = mDisposableChunkList;
				mDisposableChunkList = null;
				if (m != null)
				{
					foreach (var i in m)
					{
						i.Access.Dispose();
					}
				}
				disposedValue = true;
			}
		}

		#endregion
	}
}

namespace Tkuri2010.Fsuty.Text.Std.Lfdetail
{
	/// <summary>
	/// `Chunk` オブジェクトを受け取り、`BasicLineEnumerator` を使って行に分割し、
	/// 受け取った Func<> で任意の処理を行い、
	/// 結果に応じて Result<> を列挙するロジック。
	/// 処理した行数をプロパティとして保持している。
	/// </summary>
	public class ChunkProcessor<TResult>
	{
		public long LineNumInChunk { get; private set; } = 0;


		public List<Result<TResult>> ResultList = new List<Result<TResult>>();


		/// <summary>
		/// 
		/// </summary>
		/// <param name="chunk"></param>
		/// <param name="process"></param>
		public void ProcessChunk(IReadable chunk, Func<LineInfo, Result<TResult>> process)
		{
			var lineInfo = new LineInfo();
			var enumerator = new Lfdetail.BasicLineEnumerator();
			foreach (var lineBuf in enumerator.Enumerate(chunk))
			{
				LineNumInChunk++;
				lineInfo.LineBytes = lineBuf;
				var result = process(lineInfo);
				if (result.IsOk)
				{
					result.LineNumber = LineNumInChunk; // 暫定値である事に注意
					ResultList.Add(result);
				}
			}
		}
	}


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
		public IEnumerable<ByteArray> Enumerate(IReadable readable)
		{
			var buffer = new ByteArray();
			long offset = 0;
			long pos = 0; // 現在注視している場所
			for (; ; )
			{
				pos = FindLineEnd(readable, pos);
				var size = pos - offset;
				if (size <= 0) break;
				readable.ReadBuffer(offset, buffer, (int)size);
				yield return buffer;
				offset = pos;
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


	public class Chunk
	{
		public MemoryMappedViewAccessor Access;

		/// <summary>
		/// 元のメモリマップトファイルの、何バイト目からを切り出したかを保持しているが
		/// 切り出した後は有用に使う事はなさそう？
		/// </summary>
		public long Offset;

		public long Size;

		public bool IsLastChunk;


		internal Chunk(MemoryMappedViewAccessor acc, long offset, long size, bool isLast)
		{
			Offset = offset;
			Size = size;
			Access = acc;
			IsLastChunk = isLast;
		}
	}


	public class LargeFileChunkEnumerator
	{
		private LargeFileLinesProcessorSettings mSettings;

		public LargeFileChunkEnumerator(LargeFileLinesProcessorSettings settings)
		{
			mSettings = settings;
		}

		public IEnumerable<Chunk> Enumerate(string filePath)
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


		Chunk FindChunk(MemoryMappedFile mm, long offset, long fileTotalSize)
		{
			var localOffset = 0L;
			var chunkSize = mSettings.RoughChunkSize;
			for (var trial = 0; trial < 1024; trial++)
			{
				// これでファイルの終わりに到達するか？
				var isLastChunk = (fileTotalSize <= offset + chunkSize);
				if (isLastChunk)
				{
					var lastChunkSize = fileTotalSize - offset;
					var lastChunk = mm.CreateViewAccessor(offset, lastChunkSize, MemoryMappedFileAccess.Read);
					return new Chunk(lastChunk, offset, lastChunkSize, isLastChunk);
				}

				MemoryMappedViewAccessor? chunk = null;
				bool chunkShouldBeDisposed = true;
				try
				{
					chunk = mm.CreateViewAccessor(offset, chunkSize, MemoryMappedFileAccess.Read);
					// LF を探す
					var lastLfPos = FindLastByte(chunk, localOffset, chunkSize, (byte)'\n');

					if (0 <= lastLfPos)
					{
						chunkShouldBeDisposed = false;
						return new Chunk(chunk, offset, lastLfPos + 1, false);
					}
				}
				finally
				{
					if (chunkShouldBeDisposed) chunk?.Dispose();
				}

				localOffset = chunkSize;
				chunkSize += (mSettings.RoughChunkSize / 2); // 適当に増やす。。。どう増やそうか？
			}

			throw new System.Exception("Line separator not found. Too long line exists?");
		}


		static long FindLastByte(MemoryMappedViewAccessor mm, long minPos, long maxSize, byte target)
		{
			for (long p = maxSize - 1; p >= minPos; p--)
			{
				if (target == mm.ReadByte(p))
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
}