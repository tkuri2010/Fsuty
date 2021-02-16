## C# 9.0 からの `init` が使えない

```cs
class Foo
{
    public string Prop { get; init; }
    //                        ~~~~  CS0518
    //      定義済みの型 'System.Runtime.CompilerServices.IsExternalInit' は
    //      定義、またはインポートされていません
}
```

https://github.com/dotnet/roslyn/issues/45510#issuecomment-725091019



## `PathCanonicalize` (shlwapi.dll / Win10) の挙動

```c++
    TCHAR output[MAX_PATH] = {0};
    PathCanonicalize(output, input);
```

(以下のテーブルのはサンプルアプリ `Tkuri2010.Fsuty.Xmp` の `Try_PathCanonicalize()` メソッドを用いて出力)

| input   | output |
|---------|--------|
|(empty) | `\` |
|`.` | `\` |
|`..` | `\` |
|`.\` | `\` |
|`c:.` | `c:\` |

ディレクトリ名やファイル名が無くなる場合は、絶対パスとなるらしい。

| input   | output |
|---------|--------|
|`a..` | `a` |
|`..a` | `..a` |
|`a..b` | `a..b` |
|`./` | `./` |
|`./.\.` | `./` |
|`.a.\.` | `.a` |
|`.\./.` | `./` |

ファイル名・ディレクトリ名の最後のピリオドは省かれるらしい。先頭や中間のピリオドは省かれないらしい。

スラッシュも、英数字などと同じと見做されるらしい。

上記の表で「`./.\.`」→「`./`」となる理由は、「`/`」を「`a`」に置き換えて考えると分かりやすい。つまり「`.a.\.`」→「`.a`」となる理由と同じ。


|input| output|
|-------|-----|
|`.\..\dir\...\xxx\..\file.txt` | `dir\...\file.txt` |

3つのピリオドは特別な効力を持たないらしい。


## `Path.Combine(path1, path2)` の挙動

|path1|path2|result|
|-----|-----|------|
| `c:/base` | `dir` | `c:/base\dir` |
| `c:/base/` | `dir` | `c:/base/dir` |
| `c:\base` | `.` | `c:\base\.` |
| `c:\base` | `..\..\dir1` | `c:\base\..\..\dir1` |

基本的に単純に文字列を連結するのみ。ディレクトリセパレータが必要そうであれば、ちょっと補うのみ。

以下は、`path1` は完全に無視され、戻り値として `path2` がそのまま出力される。

|path1|path2|result|
|-----|-----|------|
| `c:\base` | `/.` | `/.` |
| `c:\base` | `\.` | `\.` |
| `c:\base` | `/dir1` | `/dir1` |
| `c:\base` | `d:dir1` | `d:dir1` |
| `c:\base` | `\\?\server\share-name\dir1` | `\\?\server\share-name\dir1` |


