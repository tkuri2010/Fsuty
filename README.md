# Fsuty
Local Files and Directories Utility (Path descriptor, Directory tree walker, etc...)

## Classes

### `Filepath` (namespace `Tkuri2010.Fsuty`)

File path parser / descriptor.
```cs
	var path = Filepath.Parse(@"C:\dir1\dir2\file.dat");

	var another = Filepath.Parse("more/another_file.dat");

	var combined = path.Parent.Combine(another.Items);
		//=> C:\dir1\dir2\more\another_file.dat
```

more formats are supported:
```cs
	var dosDevice  = Filepath.Parse(@"\\.\Volume{cafebabe-cafebabe}\dir\file.txt");
	var dosDevice2 = Filepath.Parse(@"//./Volume{cafebabe-cafebabe}/dir/file.txt"); // forward slashes are ok!

	var dosDeviceWithDrive = Filepath.Parse(@"\\.\C:\dir\file.txt");

	var dosDeviceUnc = Filepath.Parse(@"\\.\UNC\192.168.11.15\share-name\dir\file.txt");

	var unc = Filepath.Parse(@"\\server\share-name\dir\file.txt");

	var traditionalDos = Filepath.Parse(@"c:\dir\file.txt");
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
