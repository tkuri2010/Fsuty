# Fsuty
Local Files and Directories Utility (Path descriptor, Directory tree walker, etc...)

## Classes

### `Filepath` (namespace `Tkuri2010.Fsuty`)

File path parser / descriptor.
```cs
	var path = Filepath.Parse(@"C:\dir1\dir2\file.dat");

	var another = path.Parent.Combine(@"another_file.dat");
		//=> C:\dir1\dir2\another_file.dat
```

### `Fsentry` (namespace `Tkuri2010.Fsuty`)

Files and directories enumeration utility. Supports [Asynchronous streams](https://docs.microsoft.com/ja-jp/dotnet/csharp/whats-new/csharp-8#asynchronous-streams).

```cs
	async Task DoMyWorkAsync(Filepath path, [EnumeratorCancellation] CancellationToken ct = default)
	{
		// for example, we want to collect 100 files.
		var first100Files = new List<Filepath>();

		await foreach (var item in Fsentry.VisitAsync(path, ct))
		{
			if (item.Event == FsentryEvent.EnterDir)
			{
				// we can skip walking on the specified directory.
				if (item.Path.EndsWith(".git"))
				{
					item.Command = FsentryCommand.Skip;
					continue;
				}

				Console.WriteLine($"Enter Dir: {item.Path}");
			}
			else if (item.Event == FsentryEvent.LeaveDir)
			{
				Console.WriteLine($"Leave Dir: {item.Path}");
			}
			else // if (item.Event == FsentryEvent.File)
			{
				first100Files.Add(item.Path);
				if (100 <= first100Files.Count)
				{
					break;
				}
			}
		}

		/* ... and more works ... */
	}
```


### `LargeFileLinesProcessor` (namespace `Tkuri2010.Fsuty.Text.Std`)

(hmm... not so fast I expected...)

Process and filter large file lines.
Using Memory Mapped File.
Multi-Thread.

See `src / Tkuri2010.Fsuty.Xmp / LineProcessorXmp1Grep.cs` for living example.

```cs
	public async Task PoormansGrep()
	{
		var largeFile = "our/very-large-log-file.log";
		var pattern = new Regex(@"ERROR|WARN");

		// Filter and Process.
		// This func is executed in many threads.
		// The argument `lineInfo` is a `LineInfo<string>`.
		// You can use `LineInfo.Ok(v)` or `LineInfo.No()` for return value.
		ProcessingFunc<string> processingFunc = (lineInfo) =>
		{
			// process as you like
			var str = lineInfo.LineBytes.ToString(Encoding.UTF8);

			// filter as you like
			return pattern.Match(str).Success
					? str  // or you can `lineInfo.Ok(str)` explicitly
					: lineInfo.No();
		};

		// Dispose later.
		using var processor = new LargeFileLinesProcessor<string>(largeFile, processingFunc);

		await foreach (var result in processor.ProcessAsync())
		{
			// enumerates the result what you filtered
			Console.Write($"{result.LineNumber}: {result.Value}");
		}
	}

```
#### How does it work

1. Specified file is divided into about 256Kbytes each, while finding `LF` character.
1. One chunk is mapped to one thread.
1. In the thread, each line is passed to your `processingFunc(lineInfo)` func sequentially.
	- In your `processingFunc(lineInfo)` func, you can get the bytearray from `lineInfo.LineBytes` property, that has `ToString(encoding)` method.
	- Process the bytearray / string as you want.
	- If you accept the processed result, return the result (there is a implicit conversion), or you can use `lineInfo.Ok(result)` explicitly. If you reject it, return `lineInfo.No()`.
1. The `processor` enumerates the processed result that you accept.

![How works](memos/imgs/how_works.png)

