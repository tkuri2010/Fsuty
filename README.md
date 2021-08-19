# Fsuty
Local Files and Directories Utility (Path descriptor, Directory tree walker, etc...)

## Classes

- `Filepath` (namespace `Tkuri2010.Fsuty`)
	- File path parser / descriptor
- `Fsentry` (namespace `Tkuri2010.Fsuty`)
	- Files and directories enumeration utility.
- (side-product) `LinkedCollection<E>` (namespace `Tkuri2010.Fsuty`)
	- Unique formed collection class.

## Download the `*.nupkg`

I'm not sure yet that this is a library worth publishing on nuget.

Download:
- [v1.0.0-beta.20210819](https://github.com/tkuri2010/Fsuty/releases/tag/v1.0.0-beta.20210819)

### How to use downloaded `*.nupkg`

I'm not sure but this is what I tried. Looks work. (Is there any official documentation? I could not find out yet..)

Your project's structure:
```
  YourSolution/
    +-- libs/
    +    `--  Tkuri2010.Fsuty.1.0.0-foobar.nupkg     <-- where you downloded
    `-- src/
         +-- YourProject/
	 +    +-- Nuget.Config          <-- Add this file
	 +    +-- YourProject.csproj    <-- And edit your project file
	 +    +-- ...
	 `-- TestOfYourProject/
	      +-- Nuget.Config
	      +-- TestOfYourProject.csproj
	      +-- ...
```

`Nuget.Config` file is as...
```xml
<?xml version="1.0" encoding="utf-8"?>
<configuration>
  <packageSources>
    <add key="local-packages" value="../../libs" />   <!-- relative path where you downloaded the *.nupkg -->
  </packageSources>
</configuration>
```

Edit `*.csproj` file...
```xml
<Project Sdk="(snip...)">
  <ProjectGroup>
    <!-- your project's settings here -->
  </ProjectGroup>

  <ItemGroup>
    <!-- other dependencies here -->
    <PackageReference Include="Tkuri2010.Fsuty" Version="1.0.0-foobar" />  <!-- add this -->
  </ItemGroup>
```


## How to get the `*.nupkg`

```ps
PS> cd src\Tkuri2010.Fsuty

PS> dotnet pack -c Release
```

## class `Filepath` (namespace `Tkuri2010.Fsuty`)

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
	var abs = Filepath.Parse("/opt/some-tmp");

	var xxx = abs.Combine("/root");  //=> ... ???
```
Where does the `xxx` object point do you expect? `"/opt/some-tmp/root"`? or `"/root"`? I don't want to be confused by this design.

I have no plans to provide `Combine(Filepath)` nor `Combine(string)` methods. We should remember that the `Combine(PathItems)` method takes a __RELATIVE path-items-object only__.

But you can:
```cs
	PathItems heh(string path)
	{
		return Filepath.Parse(path).Items;
	}

	var abs = Filepath.Parse("/opt/some-tmp");

	var xxx = abs.Combine(heh("/root"));
```
at your own risk.


## class `Fsentry` (namespace `Tkuri2010.Fsuty`)

Files and directories enumeration utility. Supports [Asynchronous streams](https://docs.microsoft.com/ja-jp/dotnet/csharp/whats-new/csharp-8#asynchronous-streams).

```cs
	async Task DoMyWorkAsync(Filepath baseDir, [EnumeratorCancellation] CancellationToken ct = default)
	{
		// for example, we want to collect 100 files.
		var first100Files = new List<Filepath>();

		await foreach (var item in Fsentry.VisitAsync(baseDir, ct))
		{

			// ex)
			//   baseDir (argument)   =>  "F:\our\works"
			//   item.FullPathString  =>  "F:\our\works\dir\more_dir\file.txt" (string)
			//   item.RelativePath    =>               "dir\more_dir\file.txt" (Filepath object)

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
				first100Files.Add(item.RelativePath);
				if (100 <= first100Files.Count)
				{
					break;
				}
			}
		}

		/* ... and more works ... */
	}
```


## (side-product) class `LinkedCollection<E>` (namespace `Tkuri2010.Fsuty`)

When we enumerates file-system-entries, we may hold path strings like:
```
 "var" "log" "httpd"                       <-- one entry
 "var" "log" "httpd" "server1"             <-- another entry
 "var" "log" "httpd" "server1" "access"    <-- and more...
 "var" "log" "httpd" "server1" "access.1"
 "var" "log" "httpd" "server1" "access.2"
 "var" "log" "httpd" "server2"
 "var" "log" "httpd" "server2" "access"
 "var" "log" "php-fpm"
 "var" "log" "php-fpm" "error_log"
 "var" "log" "php-fpm" "error_log.1"
 ...
```

But, actualy, what we need are only these strings and links:
```
 "var"
   `-  "log"
         +-  "httpd"
         |     +-  "server1"
         |     |    +-  "access"
         |     |    +-  "access.1"
         |     |    `-  "access.2"
         |     `-  "server2"
         |           `-  "access"
         +-  "php-fpm"
         |     +-  "error_log"
         |     +-  "error_log.1"
```
`LinkedCollection<E>` is designed to hold this structures efficiently.

