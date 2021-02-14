# Fsuty
Local Files and Directories Utility (Path descriptor, Directory tree walker, etc...)

## Classes

- `Filepath` (namespace `Tkuri2010.Fsuty`)
	- File path parser / descriptor
- `Fsentry` (namespace `Tkuri2010.Fsuty`)
	- Files and directories enumeration utility.

## How to use in your project.

I'm not sure yet that this is a library worth publishing on nuget.

How to get the `*.nupkg`:

```ps
PS> cd src\Tkuri2010.Fsuty

PS> dotnet pack -c Release
```

## `Filepath` (namespace `Tkuri2010.Fsuty`)

File path parser / descriptor.
```cs
	var path = Filepath.Parse(@"C:\dir1\dir2\file.dat");

	var another = Filepath.Parse("more/another_file.dat");

	var combined = path.Parent.Combine(another.Items);
		//=> C:\dir1\dir2\more\another_file.dat

	Console.WriteLine(combined.IsAbsolute);
		//=> true
	Console.WriteLine(combined.Items[0]);
		//=> dir1
	Console.WriteLine(combined.Items[1]);
		//=> dir2
	Console.WriteLine(combined.LastItem);
		//=> another_file.dat
	Console.WriteLine(combined.Extension);
		//=> .dat
```

more formats are supported:
```cs
	var unix = Filepath.Parse("/home/tkuri2010/dir/file.txt");
	if (unix.Prefix is PathPrefix.None none)
	{
		Console.WriteLine("UNIX path doesn't have prefix.");
	}

	var dosDevice  = Filepath.Parse(@"\\.\Volume{cafebabe-cafebabe}\dir\file.txt");
	var dosDevice2 = Filepath.Parse(@"//./Volume{cafebabe-cafebabe}/dir/file.txt"); // forward slashes are ok!
	if (dosDevice.Prefix is PathPrefix.DosDevice dosDevicePrefix)
	{
		Console.WriteLine(dosDevicePrefix.Volume);
			//=> Volume{cafebabe-cafebabe}
	}

	var dosDeviceWithDrive = Filepath.Parse(@"\\.\C:\dir\file.txt");
	if (dosDeviceWithDrice.Prefix is PathPrefix.DosDeviceDrive dosDeviceDrivePrefix)
	{
		Console.WriteLine(dosDeviceDrivePrefix.DriveLetter);
			//=> C
	}

	var dosDeviceUnc = Filepath.Parse(@"\\.\UNC\192.168.11.15\share-name\dir\file.txt");
	if (dosDeviceUnc.Prefix is PathPrefix.DosDeviceUnc dosDeviceUncPrefix)
	{
		Console.WriteLine(dosDeviceUncPrefix.Server);
			//=> 192.168.11.15
		Console.WriteLine(dosDeviceUncPrefix.Share);
			//=> share-name
	}

	var unc = Filepath.Parse(@"\\server\share-name\dir\file.txt");
	if (unc.Prefix is PathPrefix.Unc uncPrefix)
	{
		Console.WriteLine(uncPrefix.Server);
		Console.WriteLine(uncPrefix.Share);
	}

	var traditionalDos = Filepath.Parse(@"c:\dir\file.txt");
	if (traditionalDos.Prefix is PathPrefix.Dos dosPrefix)
	{
		Console.WriteLine(dosPrefix.DriveLetter);
	}
```


### `Combine()` -- Why don't we have convenient `Combine(Filepath)` nor `Combine(string)` methods?

We have only `Combine(PathItems)` method.
```cs
	var path = Filepath.Parse("/home/tkuri2010/dir");

	// We can:
	var rel = Filepath.Parse("more/file.txt");
	var ok = path.Combine(rel.Items);
		//=> /home/tkuri2010/dir/more/file.txt

	// We cannot:
	var x1 = path.Combine(rel);
	var x2 = path.Combine(Filepath.Parse("more/file.txt"));
	var x3 = path.Combine("more/file.txt");
```

Why?

Consider this:
```cs
	string getRelativePath_withBug()
	{
		return RandomBool()
			? "correct/relative/dir"
			: @"D:\unregainable-our-treasures"; // accidentally returning absolute path...
	}

	var expectsRelative = getRelativePath_withBug();

	var absPath = Filepath.Parse(@"c:\playground\we-can-use-here-freely");

	var thePath = absPath.Combine(expectsRelative);
		//=> ...???

	DeleteAllItems(thePath); // What should we expect?
```

Refer to the `System.IO.Path.Combine()`:
```cs
	var expectsRelative = getRelativePath_withBug(); // OMG...

	var absPath = @"C:\playground\we-can-use-here-freely";

	var thePath = Path.Combine(absPath, expectsRelative);
		//=> `thePath` is now "D:\unregainable-our-treasures"

	DeleteAllItems(thePath); // buh bye our treasures!
```
Did you expect this behavior? I don't like this.

I don't provide `Combine(Filepath)` nor `Combine(string)` methods so that we can remember that the `Combine(PathItems)` method takes a (relative) path-items-object ONLY.


## `Fsentry` (namespace `Tkuri2010.Fsuty`)

Files and directories enumeration utility. Supports [Asynchronous streams](https://docs.microsoft.com/ja-jp/dotnet/csharp/whats-new/csharp-8#asynchronous-streams).

```cs
	async Task DoMyWorkAsync(Filepath baseDir, [EnumeratorCancellation] CancellationToken ct = default)
	{
		// for example, we want to collect 100 files.
		var first100Files = new List<Filepath>();

		await foreach (var item in Fsentry.VisitAsync(baseDir, ct))
		{

			// ex)
			//   baseDir =>  F:\our\works
			//   item.FullPathString =>  "F:\our\works\dir\more_dir\file.txt" (string)
			//   item.RelativeParent =>  "dir\more_dir" (Filepath object)

			if (item.Event == FsentryEvent.EnterDir)
			{
				// we can skip walking on the specified directory.
				if (item.FullPathString.EndsWith(".git"))
				{
					item.Command = FsentryCommand.SkipDirectory;
					continue;
				}

				Console.WriteLine($"Enter Dir: {item.FullPathString}");
			}
			else if (item.Event == FsentryEvent.LeaveDir)
			{
				Console.WriteLine($"Leave Dir: {item.FullPathString}");
			}
			else // if (item.Event == FsentryEvent.File)
			{
				first100Files.Add(item.FullPathString);
				if (100 <= first100Files.Count)
				{
					break;
				}
			}
		}

		/* ... and more works ... */
	}
```
