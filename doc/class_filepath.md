I know this markdown document is hard to read. Is there any better documentation tools?

# class `Filepath` (namespace: `Tkuri2010.Fsuty`)

File path parser / descriptor.

## Basic idea

The `Filepath` has properties that represents "path prefix", "flag - absolute path or not" and "path items".

For example, consider this traditional MS-DOS style path:

```
    C:\Users\tkuri2010\foobar.txt
```

Parsed as:
```
    C:  \   Users\tkuri2010\foobar.txt
    ^^  ^   ^^^^^ ^^^^^^^^^ ^^^^^^^^^^
    1   2   3     3         3

    1: path prefix
    2: this means that this is an absolute path
    3: path items
```

Another example:
```
    \\server-name\share-name\our-vaults\foobar.txt
```

Parsed as:
```
    \\server-name\share-name   \   our-vaults\foobar.txt
    ^^^^^^^^^^^^^^^^^^^^^^^^   ^   ^^^^^^^^^^ ^^^^^^^^^^
    1                          2   3          3

    1: path prefix
    2: this means that this is an absolute path
    3: path items
```

And another:
```
    ./src/Fsuty/debug
```

Parsed as:
```
    ./src/Fsuty/debug
    ^ ^^^ ^^^^^ ^^^^^
    3 3   3     3

    1: ... no path prefix
    2: ... this is a relative path
    3: path items. The starting "." is one of items here. You can "canonicalize" easily.
```

## Supported path formsts.

Well known simple UNIX style paths:

| Source path string  | path prefix | absolute flag | items   | note |
|-                    |-            |-              |-        |-     |
| (empty string)      | (none)      | `false`       | (empty) | `Filepath` can handle the empty string. |
| `.`                 | (none)      | `false`       | only `"."`  | |
| `dir/item`          | (none)      | `false`       | `"dir"` and `"item"` | |
| `../dir/item`       | (none)      | `false`       | `".."`, `"dir"` and `"item"` | |
| `/`                 | (none)      | `true`        | (empty) | |
| `/root/dir/./item`  | (none)      | `true`        | `"root"`, `"dir"`, `"."` and `"item"` | |


Well known simple MS-DOS/Windows style paths: __Note that forward slashes are allowd in all cases.__

| Source path string  | path prefix | absolute flag | items   | note |
|-                    |-            |-              |-        |-     |
| `C:`                | `"C:"`      | `false`       | (empty) | Traditional DOS style, but a drive letter only. |
| `C:\dir\item`       | `"C:"`      | `true`        | `"dir"` and `"item"` | |
| `C:/dir/item`       | `"C:"`      | `true`        | `"dir"` and `"item"` | Forward slashes allowed!! |
| `C:dir\item`        | `"C:"`      | `false`       | `"dir"` and `"item"` | Drive spec and a relative path. Did you know such a format? |


### Dos device path
```
  Path:
    \\.\Volume{asdfasdf-asfdasdf}\dir\item
    \\?\Volume{asdfasdf-asfdasdf}\dir\item

  Parsed as:
    \\.\Volume{asdfasdf-asfdasdf}  \  dir\item
    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^  ^  ^^^ ^^^^
    \\?\Volume{asdfasdf-asfdasdf}  \  dir\item
    ^^^^^^^^^^^^^^^^^^^^^^^^^^^^^  ^  ^^^ ^^^^
    1                              2  3   3
```


### Dos device path, specified with a drive
```
  Path:
    \\.\C:\dir\item
    \\?\C:\dir\item

  Parsed as:
    \\.\C:  \  dir\item
    ^^^^^^  ^  ^^^ ^^^^
    \\?\C:  \  dir\item
    ^^^^^^  ^  ^^^ ^^^^
    1       2  3   3
```


### Dos device with UNC
```
  Path:
    \\.\UNC\server\share-name\dir\item
    \\?\UNC\server\share-name\dir\item

  Parsed as:
    \\.\UNC\server\share-name  \  dir\item
    ^^^^^^^^^^^^^^^^^^^^^^^^^  ^  ^^^ ^^^^
    \\?\UNC\server\share-name  \  dir\item
    ^^^^^^^^^^^^^^^^^^^^^^^^^  ^  ^^^ ^^^^
    1                          2  3   3
```


### UNC
```
  Path:
    \\server\share-name\dir\item

  Parsed as:
    \\server\share-name  \  dir\item
    ^^^^^^^^^^^^^^^^^^^  ^  ^^^ ^^^^
    1                    2  3   3
```

Refer: https://docs.microsoft.com/en-us/dotnet/standard/io/file-path-formats

-------------------------------------------------------------------

## Handling colon and backslash

Consider this:
```cs
    var path = Filepath.Parse(@"C:\item1\item2/item3");
```

On UNIX style system, the `':'`(colon) and the `'\'`(backslash) characters are one of path string characters.
```cs
    Console.WriteLine( path.Prefix );       //=> class `PathPrefix.None`
    Console.WriteLine( path.Items.Count );  //=> should be 2
    Console.WriteLine( path.Items[0] );     //=> should be "C:\item1\item2"
    Console.WriteLine( path.Items[1] );     //=> should be "item3"
```

On MS-DOS/Windows, the `':'` may be a "volume separator character", and the `'\'` character is a directory separator.
```cs
    Console.WriteLine( path.Prefix );       //=> class `PathPrefix.Dos`
    Console.WriteLine( path.Items.Count );  //=> should be 3
    Console.WriteLine( path.Items[0] );     //=> should be "item1"
```

So, the `Filepath` can switch this behavior.

Defaultly, if `System.IO.Path.DirectorySeparatorChar` or `System.IO.Path.AltDirectorySeparatorChar` equals `'\'`(backslash), the `Filepath` assumes that it is on a MS-DOS/Windows, otherwise it is on a UNIX style system.

You can switch this behavior by setting the `Filepath.CurrentParser` static prpoerty. See below.


-------------------------------------------------------------------
## enum `Filepath.Style`
- `_Unknown` - could not detect.
- `Unix` - means Unix style path
- `Win32` - means MS-DOS/Windows style path

-------------------------------------------------------------------
## class `Filepath` static properties

### `statkc Filepath.Style DefaultStyle`
#### Description
System default value, `Style.Unix` or `Style.Win32`

### `static IFilepathParser DefaultParser`
#### Description
Gets the system default Filepath parser instance.

### `static IFilepathParser UnixParser`
#### Description
Gets the Unix style file path parser instance.

### `static IFilepathParser Win32Parser`
#### Description
Gets the MS-DOS/Windows style file path parser instance.

### `static IFilepathParser CurrentParser`
#### Description
Gets or sets a path string parser implementation.
#### Default value
Detects from dotnet runtime's state.
#### Example
```cs
    // system default
    Filepath.CurrentParser = Filepath.DefaultParser;

    // Unix
    Filepath.CurrentParser = Filepath.UnixParser;

    // Win32
    Filepath.CurrentParser = Filepath.Win32Parser;
```

### `static readonly Filepath Empty`
#### Description
Represents empty `Filepath` instance. It has no path prefix, means relative path, no path items.


---
## class `Filepath` static methods

### `static Filepath Parse(string path)`

#### Parameters
- `path` - the source path string
#### Returns
New `Filepath` instance.
#### Description
Parses the path string.
#### Excample
```cs
    var fp = Filepath.Parse(@"C:\Users\tkuri2010\dir");
```

---
## class `Filepath` properties

### `IPathPrefix Prefix`
#### Description
Represents a prefix part of MS-DOS/Windows style path. For instance, a drive letter, or a shared folder's server name and share name, and so on.

See [\[class_pathprefix.md\]](./class_pathprefix.md) for more detail.

#### Example
```cs
    var fp = Filepath.Parse(@"C:\dir\file");
    if (fp.Prefix is PathPrefix.Dos dosPrefix)
    {
        Console.WriteLine( dosPrefix.Drive ); //=> C:
    }
```


### `bool IsAbsolute`
#### Description
Represents the path means an absolute path or not.


### `PathItems Items`
#### Description
Represents path items. Many strings' collection. 
#### Example
```cs
    var fp = Filepath.Parse(@"dir/file");
    Console.WriteLine( fp.Items.Count ); //=> 2
    Console.WriteLine( fp.Items[0] );    //=> dir
    Console.WriteLine( fp.Items[1] );    //=> file
```


### `string LastItem`
#### Description
Short hand of `Items[Items.Count - 1]`. if `Item.Count == 0`, this value is a `string.Empty`;

### `bool HasExtension`
#### Description
Short hand of `System.IO.Path.HasExtension(LastItem)`.


### `string Extension`
#### Description
Short hand of `System.IO.Path.GetExtension(LastItem)`.


### `string LastItemWithoutExtension`
#### Description
Short hand of `System.IO.Path.GetFilenameWithoutExtension(LastItem)`.

### `Filepath Parent`
#### Description
Gets the parent directory, or empty instance. Short hand of `this.Slice(0, -1)` or `this.Ascend()`.


---
##  class `Filepath` methods

### `string ToString()`
#### Returns
A string representation.
#### Description
Gets a string representation. The path items are joined with `System.IO.Path.DirectorySeparatorChar`.
#### Example
```cs
    var fp = Filepath.Parse(@"C:\dir\item");
    Console.WriteLine( fp.ToString() );   //=> "C:\dir\item"

    var fp2 = Filepath.Parse(@"\\server\share-name/dir/item");
    Console.WriteLine( fp2.ToString() );  //=> "\\server\share-name\dir\item"
```


### `string ToString(char directorySeparatorChar)`
#### Parameters
- directorySeparatorChar
#### Returns
A string representation.
#### Example
```cs
    var fp = Filepath.Parse(@"C:\dir\item");
    Console.WriteLine( fp.ToString('_') );   //=> "C:_dir_item"
```


### `Filepath Combine(PathItems items)`
#### Parameters
- items - Path items
#### Returns
New `Filepath` instance.
#### Example
```cs
    var origin = Filepath.Parse("./dir1/dir2");

    var more = Filepath.Parse("sub1/sub2");

    var combined = origin.Combine(more.Items);
    Console.WriteLine( combined ); //=> "./dir1/dir2/sub1/sub2"
```


### `Filepath Slice(int start, int count = int.MaxValue)`
#### Parameters
- `start` - takes integer, usually `-Count` ~ `Count - 1`.
- `count` (default = `int.MaxValue`) - takes integer, usually  `-Count` ~ `Count`.
#### Returns
New `Filepath` instance. 
#### Description
Gets a sub path. The returned instance's `Prefix` property is same as the original instance's. The `Items` property has sub sequence of the original instance's one. The `IsAbsolute` property depends on the `start` parameter.
#### Example
The returned instance's `IsAbsolute` property depends on the `start` parameter.
```cs
    var original = Filepath.Parse("/root/dir/item");

    var sliced0 = original.Slice(0);
    Console.WriteLine( sliced0 ); //=> "/root/dir/item"
    Console.WriteLine( sliced0.IsAbsolute ); //=> true


    var sliced1 = original.Slice(1);
    Console.WriteLine( sliced1 );  //=> "dir/item"
    Console.WriteLine( sliced1.IsAbsolute );  //=> false
```

The `start` parameter can take negative integer.
```cs
    var sliced2 = original.Slice(-1);  //=> "item"

    var sliced3 = original.Slice(-2);  //=> "dir/item"

    var slicedXX = original.Slice(int.MinValue);
    //=> perhaps does not crash, but the result value is unstable.
```

Alos the `count` parameter can take negative integer.
```cs
    var sliced4 = original.Slice(0, -1);
    //=> negative `count` means "this.Count - 1", so the result is "/root/dir"

    var sliced5 = original.Slice(0, -2);  //=> "/root"
```


### `Filepath Ascend(int level = 1)`
#### Parameter
- `level`(default = 1) - Ascending level. 1 = gets a parent directory, 2 = grand parent, 3 = great grand parent, and more...
#### Returns
New `Filepath` instance.
#### Description
Actualy, a short hand of `Slice(0, -level)`.



### `Filepath Canonicalize()`
#### Returens
New `Filepath` instance.
#### Description
Resolves `"."` or `".."`.
#### Example
```cs
    var original = Filepath.Parse("dir1/./dir2/../dir3/item");
    var canon = original.Canonicalize(); //=> dir1/dir3/item"


    // This behavior is same as the Win32 API `PathCanonicalize()`
    //   - Beginning ".." is removed.
    //   - "..." remains.
    var orig2 = Filepath.Parse("../dir1/.../item");
    var can2 = orig2.Canonicalize(); //=>  "dir1/.../item"
```


-------------------------------------------------------------------------
## More examples

### How to handle `Prefix` property
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

-------------------------------------------------------------------------

## `Combine()` -- Why don't we have convenient `Combine(Filepath)` nor `Combine(string)` methods?

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
    PathItems _Rel(string path)
    {
        return Filepath.Parse(path).Items;
    }

    var abs = Filepath.Parse("/opt/some-tmp");

    var xxx = abs.Combine(_Rel("/root"));   //=> /opt/some-tmp/root
```
at your own risk.
