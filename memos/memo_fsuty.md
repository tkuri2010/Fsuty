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


## LangVersion = 9.0 にしているのは何故だったか？

忘れた。おそらく、サポートしたい dotnet SDK のバージョンの都合に合わせたはずだけど、合理性がなさそうであれば 10 以降に上げたい。フラットな `namespace` とか使いたい。


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
	await foreach (var e in Fsentry.EnumerateAsync(@"D:\there"))
	{
		Console.WriteLine(e.RelativePath);  // 全て文字列化する
		all.Add(e.RelativePath);
	}
```

### 結果
|                      | `GC.GetTotalAllocatedBytes` | `GC.GetAllocatedBytesForCurrentThread` | 経過時間 |
| -                    | -                           | -                                      | -        |
| `ReadOnlyCollection` | 159Mb                       | 157Mb                                  | 1.7秒 |
| `LinkedCollection`   | ~~143Mb~~ 137Mb (better)    | ~~141Mb~~ 135Mb (better)               | ~~4.3秒~~ 1.7秒    |

`LinkedCollection` は 10% ほどメモリの使用を抑えられるが、イテレーションが遅いので文字列化に時間がかかる。

だが `LinkedCollection` は逆順にリンクを持つ構造であるため、文字列化の際に `string.Join()` に頼るのではなく逆順に文字列を組み立ててみたところ4秒から2秒未満へ高速化に成功し、遜色が無くなったかもしれない。

ちなみに `LinkedCollection` では列挙をなるべく高速にしたいので「曾祖父」世代の参照を保持しておく事にしているが、この機能を切っても速度は遜色無かった。メモリは機能を有効にした場合に比べて数%だけ抑えられた。

※ (2021/9/24) アイテムの列挙時、なぜか entry が持っている RelativePath と同じものをローカルのロジックで作り直していたというバグを修正して再計測。メモリの使用量がもう少し抑えられたらしい。

### 文字列化をやめるとどうなるか？

```cs
	var all = new List();
	await foreach (var e in Fsentry.EnumerateAsync(@"there"))
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


## `enum FsentryEvent` をやめて `WhenFile(out FileEvent fe)` 系のインターフェースにしようか？

`Fsinfo` は、`DirectoryInfo` か `FileInfo` を取り出すために `When_()` メソッドを使うインターフェースにしてある。`Fsentry` もその形式に倣いたいと思ってた。

###### Legacy concept:
```cs
    await foreach (var e in Fsentry.EnumerateAsync())
    {
        if (e.Event == FsentryEvent.EnterDir)
        {
            if (e.RelativePath.LastItem == ".git")
            {
                e.Command = FsentryCommand.Skip;
                continue;
            }
            // ...
        }
        else if (e.Event == FsentryEvent.File)
        {
            // ...
            // 間違えてここで
            //   e.Comand = FsentryCommand.Skip;
            // などミスをしでかす可能性もあり
        }
    }
```

↓の New concept と交互に実行してみた記録:

|       | GetTotalAllocatedBytes | 実行時間 | (参考?) GetAllocatedBytesForCurrentThread |
| ---   | -                      | -        | - |
| 1回目 |  189,025,328           | 21.8     | 44,690,440 |
| 2回目 |  189,018,016           | 17.2     | 67,317,976 |
| 3回目 |  189,025,328           | 17.4     | 8,367,392  |

- ディレクトリ数:16,120, ファイル数:105,771
- 1回目はSSDのキャッシュも無かっただろうし、少なくとも実行時間は無視するべき
- `GetAllocatedBytesForCurrentThread` は名前の通り、カレントスレッドのメモリ量よね？今回は `ConfigureAwait(false)` を使ったので、きっと作業に使われたスレッドは分散したと思う。なので参考程度。

###### New concept:
```cs
    await foreach (var e in Fsentry.EnumerateAsync())
    {
        if (e.WhenEnterDir(out var enterDir)
        {
            if (enterDir.RelativePath.LastItem == ".git")
            {
                enterDir.Skip();
                continue;
            }
            // ...
        }
        else if (e.WhenFile(out var file))
        {
            // file は Skip() なんてメソッドは持ってない
        }
    }
```

↑のLegacy版と交互に実行した結果:

|       | GetTotalAllocatedBytes | 実行時間 | (参考?) GetAllocatedBytesForCurrentThread |
| ---   | -                      | -        | - |
| 1回目 |  189,019,976           | 17.5     | 58,089,400 |
| 2回目 |  188,997,664           | 17.2     | 58,904,208 |
| 3回目 |  189,271,928           | 17.3     | 41,943,352 |

なんで実行時間に遜色無いんだろう？ `is` 演算子を使って実行時型情報を使ってるのに。
なぜかはともかく、`enum` を判定する版と比べて実行時間は「全く」遜色ない。
といって真面目に実装したら 1% 遅くなり 17.44sec などの記録。。。GetTotalAll..() が 185.. とか下がったし、まあいいか。

上の版は一つ抽象クラスを挟み実装の継承をしていたりしていたが、そこにオーバーヘッドかかってそう？
クラスの構成をシンプルにした。

|          | GetTotalAllocatedBytes | 実行時間 | (参考?) GetAllocatedBytesForCurrentThread |
| ---      | -                      | -        | - |
| Legacy   |  185,888,920           | 16.1     | 73,704,216 |
| 新しい版 |  184,792,288           | 16.3     | 17,996,952 |

global.json の SDK が 5.0 だったのを 6.0 に変えた成果もあるかも知れないけど、速くなった。新しい版も変わらず「遜色なし」と言えそう。
