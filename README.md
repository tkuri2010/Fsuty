# Fsuty
Local Files and Directories Utility (Path descriptor, Directory tree walker, etc...)

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
    <!-- your project's settings here -->
  </ProjectGroup>

  <ItemGroup>
    <!-- other dependencies here -->
    <PackageReference Include="Tkuri2010.Fsuty" Version="1.0.0-foobar" />  <!-- add this -->
  </ItemGroup>
```


## How to build this project and get the `Tkuri2010.Fsuty.***.nupkg`

```ps
PS> cd src\Tkuri2010.Fsuty

PS> dotnet pack -c Release
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
→ more details: [\[./doc/class_filepath.md\]](./doc/class_filepath.md)


## class `Fsentry` (namespace `Tkuri2010.Fsuty`)

Files and directories enumeration utility. Supports [Asynchronous streams](https://docs.microsoft.com/ja-jp/dotnet/csharp/whats-new/csharp-8#asynchronous-streams).

```cs
	async Task DoMyWorkAsync(Filepath baseDir, [EnumeratorCancellation] CancellationToken ct = default)
	{
		// for example, we want to collect 100 files.
		var first100Files = new List<Filepath>();

		await foreach (var item in Fsentry.EnumerateAsync(baseDir, ct))
		{

			// ex)
			//   baseDir (argument)   =>  "F:\our\works"
			//   item.FullPathString  =>  "F:\our\works\dir\more_dir\file.txt" (string)
			//   item.RelativePath    =>               "dir\more_dir\file.txt" (Filepath object)

			if (item.Event == Fsevent.EnterDir)
			{
				// we can skip walking on the specified directory.
				if (item.FullPathString.EndsWith(".git"))
				{
					item.Command = Fscommand.SkipDirectory;
					continue;
				}

				Console.WriteLine($"Enter Dir: {item.FullPathString}");
			}
			else if (item.Event == Fsevent.LeaveDir)
			{
				Console.WriteLine($"Leave Dir: {item.FullPathString}");
			}
			else if (item.Event == Fsevent.File)
			{
				first100Files.Add(item.RelativePath);
				if (100 <= first100Files.Count)
				{
					break;
				}
			}
			else if (item.Event == Fsevent.Error)
			{
				// error...
			}
		}

		/* ... and more works ... */
	}
```


## (Experimental) class `Fsinfo` (namespace `Tkuri2010.Fsuty`)

***!!! Unstable yet !!!***

Enumerates file system entries, as `DirectoryInfo` or `FileInfo`.

```cs
async Task DoMyWorkAsync(Filepath baseDir, [EnumeratorCancellation] CancellationToken ct = default)
{
	await foreach (var item in Fsinfo.EnumerateAsync(baseDir, ct))
	{
		if (item.WhenEnterDir(out DirectoryInfo enterDirInfo))
		{
			// dirInfo is a `System.IO.DirectoryInfo` here.
			// do something...
		}
		else if (item.WhenLeaveDir(out DirectoryInfo leaveDirInfo))
		{
			// do something...
		}
		else if (item.WhenFile(out FileInfo fileInfo))
		{
			// fileInfo is a `System.IO.FileInfo` here.
		}
		else if (item.WhenError(out Exception exception, out DirectoryInfo currentDirInfo))
		{
			// error...
		}
	}
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

