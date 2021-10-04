I know this markdown document is hard to read. Is there any better documentation tools?

# namespace `Tkuri2010.Fsuty.PathPrefix`

The `Filepath` class has `Prefix` property. This means drive letter, or server and share name on MS-DOS/Windows style paths.

## class `None`
This means the path has no prefix.

```cs
    var unix = Filepath.Parse(@"/home/tkuri2010");
    if (unix.Prefix is PathPrefix.None)
    {
        Console.WriteLine("UNIX style path has no prefix.");
    }

    var relative = Filepath.Parse(@".\dir\item");
    if (relative.Prefix is PathPrefix.None)
    {
        Console.WriteLine("Also relative path has no prefix.");
    }
```


## interface `IDosDevice`
Represents a "DOS Device".
### Known implementations
- class `DosDevice`
- class `DosDeviceDrive`
- class `DosDeviceUnc`

### Property `string SignChar`
`"."` or `"?"`.
```
   \\.\volume....
   \\?\volume....
     ^
     This character
```

### Property `string Volume`
Represents a volume part.
```
    \\.\\volume{asdf-asdf}\dir\item
         ^^^^^^^^^^^^^^^^^
         Here
```

## interface `IHasDrive`
Represents the path has a drive.

### Known implementations
- class `DosDeviceDrive`
- class `Dos`

### Property `string Drive`
Drive letter and volume separator. For example, `"C:"`.

### Property `string DriveLetter`
Represents drive letter. For example, `"C"`.


## interface `IShared`
Represents the path may be a network path.

### Known implementations
- class `DosDeviceUnc`
- class `Unc`

### Property `string Server`
Represents a server name or address.

### Property `string Share`
Represents a share name.


## class `DosDevice`
Represents a "DOS Device path".
```
   \\.\Volume{asdf-asdf-asdf-asdf}\dir\item
```
### Derived from
- interface `IDosDevice`

## class `DosDeviceDrive`
Represents a "DOS Device path", but a drive specified format.
```
    \\.\C:\dir\item
        ^^
        This is the `Drive` property, and also the `Volume` property.
```
### Derived from
- interface `IDosDevice`
- interface `IHasDrive`

## class `DosDeviceUnc`
Represents a "DOS Device path" with UNC sign.
```
    \\.\UNC\server-name\share-name\dir\item
            ^^^^^^^^^^^ ^^^^^^^^^^
              `Server`   `Share`
            `--------------------'
              `Volume` property
```
### Derived from
- interface `IDosDevice`
- interface `IShared`


## class `Dos`
Represents a usual(traditional) MS-DOS/Windows style path.
```
    C:\Users\tkuri2010\dir\item.ext
```
### Derived from
- interface `IHasDrive`
