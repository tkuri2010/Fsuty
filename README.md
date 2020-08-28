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

Filter and process large file lines.
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
		// Use `LineInfo.Ok(v)` or `LineInfo.No()` for return value.
		ProcessingFunc<string> func = (lineInfo) =>
		{
			var str = lineInfo.LineBytes.ToString(Encoding.UTF8);

			if (pattern.Match(str).Success)
				return lineInfo.Ok(str);
			else
				return lineInfo.No();
		};

		using var processor = new LargeFileLinesProcessor<string>(largeFile, func);

		await foreach (var result in processor.ProcessAsync())
		{
			Console.Write($"{result.LineNumber}: {result.Value}");
		}
	}

```
