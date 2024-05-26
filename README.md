# Fsuty
.NET library of Utility for Local Files and Directories (Path descriptor, Directory tree walker, etc...)

[![build and test](https://github.com/tkuri2010/Fsuty/actions/workflows/test.yaml/badge.svg)](https://github.com/tkuri2010/Fsuty/actions/workflows/test.yaml)

## NOT TO USE (version 1.0.0-delta.20240430)

The current version (1.0.0-delta.20240430) works as I intended, but is disorganized and contains various elements that are not yet systematically arranged. This version is committed as a personal memorial.

In particular, I think that it was a bad idea to make the `Fsentry.EnumerateAsync()` an asynchronous process. I an going to make some significant changes.


## Basic info

- Supports .NET Standard 2.1
- Tested on Windows and Linux. Maybe also it works on macOS well. (not tested on macOS yet... <https://github.com/tkuri2010/Fsuty/issues/32>)


## Classes

- `Filepath` (namespace `Tkuri2010.Fsuty`)
	- File path parser / descriptor
- `Fsentry` (namespace `Tkuri2010.Fsuty`)
	- Files and directories enumeration utility.
- `Fsinfo` (namespace `Tkuri2010.Fsuty`) (experimental!)
    - `System.IO.FileInfo` and `DirectoryInfo` enumeration utility.
- (side-product) `LinkedCollection<E>` (namespace `Tkuri2010.Fsuty`)
	- Unique formed collection class.

## Download the `*.nupkg`

I'm not sure yet that this is a library worth publishing on nuget.

Download the `Tkuri2010.Fsuty.***.nupkg` file from [Releases page](https://github.com/tkuri2010/Fsuty/releases).

### How to use a `*.nupkg` you downloaded

I'm not sure but this is what I tried. Looks work. (Is there any official documentation? I could not find out yet..)

Your project's structure:
```
  YourSolution/
    +-- libs/
    +    `--  Tkuri2010.Fsuty.1.0.0-foobar.nupkg     <-- where you downloded
    `-- src/
         +-- YourProject/
         +    +-- nuget.config          <-- Add this file
         +    +-- YourProject.csproj    <-- And edit your project file
         +    +-- ...
         `-- TestOfYourProject/
              +-- nuget.config
              +-- TestOfYourProject.csproj
              +-- ...
```

`nuget.config` file is as...
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
    <!-- settings of your project here -->
  </ProjectGroup>

  <ItemGroup>
    <!-- other dependencies here -->

    <PackageReference Include="Tkuri2010.Fsuty" Version="1.0.0-foobar" />  <!-- add this -->
  </ItemGroup>
```


## How to build this project and get the `Tkuri2010.Fsuty.***.nupkg`

```ps
> cd src\Tkuri2010.Fsuty

> dotnet pack -c Release
```

## class `Filepath` (namespace `Tkuri2010.Fsuty`)

File path parser / descriptor.
```cs
    var path = Filepath.Parse(@"C:\dir1\dir2\file.dat");

    Console.WriteLine( path.ToString() );
        //=> C:\dir1\dir2\file.dat
    Console.WriteLine( path.IsAbsolute );
        //=> true
    Console.WriteLine( path.Items[0] );
        //=> dir1
    Console.WriteLine( path.Items[1] );
        //=> dir2
    Console.WriteLine( path.LastItem );
        //=> file.dat
    Console.WriteLine( path.Extension );
        //=> .dat

    var parent = path.Parent;
        //=~ C:\dir1\dir2

    var another = Filepath.Parse("more/another_file.dat");

    Console.WriteLine( another.IsAbsolute );
        //=> false

    var combined = parent.Combine(another.Items);
        //=> C:\dir1\dir2\more\another_file.dat
```
→ more details: [./doc/class_filepath.md](./doc/class_filepath.md)


## class `Fsentry` (namespace `Tkuri2010.Fsuty`)

Files and directories enumeration utility. Supports [Asynchronous streams](https://docs.microsoft.com/ja-jp/dotnet/csharp/whats-new/csharp-8#asynchronous-streams).

```cs
	async Task DoMyWorkAsync(Filepath baseDir, CancellationToken ct = default)
	{
		// for example, we want to collect 100 files.
		var first100Files = new List<Filepath>();

		await foreach (var it in Fsentry.EnumerateAsync(baseDir, ct).ConfigureAwait(false))
		{

			if (it is Fsentry.EnterDir enterDir)
			{
				// ex)
				//   baseDir (argument)       =>  "F:\our\works"
				//   enterDir.FullPathString  =>  "F:\our\works\dir\more_dir\file.txt" (string)
				//   enterDir.RelativePath    =>               "dir\more_dir\file.txt" (Filepath object)

				// we can skip walking on the specified directory.
				if (enterDir.FullPathString.EndsWith(".git"))
				{
					enterDir.Skip();
					continue;
				}

				Console.WriteLine($"Enter Dir: {enterDir.FullPathString}");
			}
			else if (it is Fsentry.LeaveDir leaveDir)
			{
				Console.WriteLine($"Leave Dir: {leaveDir.FullPathString}");
			}
			else if (it is Fsentry.File file)
			{
				first100Files.Add(file.RelativePath);
				if (100 <= first100Files.Count)
				{
					break;
				}
			}
			else if (it is Fsentry.Error error)
			{
				if (error.Exception is Fsentry.EnumerationException ex)
				{
					Console.WriteLine($"error in directory: " + ex.DirPathString);
					Console.WriteLine("cause: " + (ex.InnerException?.Message ?? "no idea..."));
				}
			}
		}

		/* ... and more works ... */
	}
```


## class `Fsinfo` (namespace `Tkuri2010.Fsuty`)

Enumerates file system entries, as `System.IO.DirectoryInfo` or `System.IO.FileInfo`.

```cs
async Task DoMyWorkAsync(Filepath baseDir, CancellationToken ct = default)
{
	await foreach (var it in Fsinfo.EnumerateAsync(baseDir, ct).ConfigureAwait(false))
	{
		if (it is Fsinfo.EnterDir enterDir)
		{
			// enterDir.Info is a `System.IO.DirectoryInfo` here.
			Console.WriteLine($"enter dir: {enterDir.Info.Name}");
		}
		else if (it is Fsinfo.LeaveDir leaveDir)
		{
			// also leaveDir.Info is a `System.IO.DirectoryInfo` here.
			Console.WriteLine($"leave dir: {leaveDir.Info.Name}");
			// ...
		}
		else if (it is Fsinfo.File file))
		{
			// file.Info is a `System.IO.FileInfo` here.
			Console.WriteLine($"file: {file.Info.Name}");
		}
		else if (it is Fsinfo.Error error)
		{
			// error...
		}
	}
}
```

## (side-product) class `LinkedCollection<E>` (namespace `Tkuri2010.Fsuty`)

When we enumerate file system entries, we may hold a lot of path strings like:
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
         |     |     +-  "access"
         |     |     +-  "access.1"
         |     |     `-  "access.2"
         |     `-  "server2"
         |           `-  "access"
         +-  "php-fpm"
         |     +-  "error_log"
         |     +-  "error_log.1"
```
`LinkedCollection<E>` is designed to hold this structure efficiently.

