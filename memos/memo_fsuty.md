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



## `LinkedCollection` はよいものか否か

`PathItems` クラスの中でパス文字列を保持するには、どんなデータ構造を使うとよいか？

### 次のようなコードでおよそ 8.5 万ファイルを列挙
```cs
	var all = new List();
	await foreach (var e in Fsentry.VisitAsync(@"D:\there"))
	{
		Console.WriteLine(e.RelativePath);  // 全て文字列化する
		all.Add(e.RelativePath);
	}
```

### 結果
|                      | `GC.GetTotalAllocatedBytes` | `GC.GetAllocatedBytesForCurrentThread` | 経過時間 |
| -                    | -                           | -                                      | -        |
| `ReadOnlyCollection` | 159Mb                       | 157Mb                                  | 1.7秒 |
| `LinkedCollection`   | 143Mb (better)              | 141Mb (better)                         | ~~4.3秒~~ 1.7秒    |

`LinkedCollection` は 10% ほどメモリの使用を抑えられるが、イテレーションが遅いので文字列化に時間がかかる。

だが `LinkedCollection` は逆順にリンクを持つ構造であるため、文字列化の際に `string.Join()` に頼るのではなく逆順に文字列を組み立ててみたところ4秒から2秒未満へ高速化に成功し、遜色が無くなったかもしれない。

ちなみに `LinkedCollection` では列挙をなるべく高速にしたいので「曾祖父」世代の参照を保持しておく事にしているが、この機能を切っても速度は遜色無かった。メモリは機能を有効にした場合に比べて数%だけ抑えられた。

### 文字列化をやめるとどうなるか？

```cs
	var all = new List();
	await foreach (var e in Fsentry.VisitAsync(@"there"))
	{
		// Console.WriteLine(e.RelativePath);  // やめる
		all.Add(e.RelativePath);
	}
```

### 結果
|                      | `GC.GetTotalAllocatedBytes` | `GC.GetAllocatedBytesForCurrentThread` | 経過時間 |
| -                    | -                           | -                                      | -        |
| `ReadOnlyCollection` | 135Mb                       | 133Mb                                  | 1.3秒    |
| `LinkedCollection`   | 119Mb (better)              | 117Mb (better)                         | 1.2秒    |

速度はほぼ同等と言ってよくなった。
