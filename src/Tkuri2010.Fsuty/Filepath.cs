#define USE_LINKEDCOLLECTION
// パス文字列を保持する PathItems クラスの内部表現として何を使うか？
// 特別に作った LinkedCollection か、既存の汎用的な ReadonlyCollection か

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;


namespace Tkuri2010.Fsuty
{
	/// <summary>file path parser</summary>
	public interface IFilepathParser
	{
		/// <summary>Unix style file path parser. You can set this to Filepath.CurrentParser property.</summary>
		// public static IFilepathParser Unix => Internal.UnixFilepathParser.Instance;


		/// <summary>Win32 style file path parser. You can set this to Filepath.CurrentParser property.</summary>
		// public static IFilepathParser Win32 => Internal.Win32FilepathParser.Instance;


		Filepath Parse(string? path);
	}


	/// <summary>Win32 path prefix descriptor</summary>
	public interface IPathPrefix
	{
	}


	/// <summary>Relative path items (array of string)</summary>
	public class PathItems : IReadOnlyList<string>
	{
		public static readonly PathItems Empty = new();

#if USE_LINKEDCOLLECTION
		private LinkedCollection<string> mItems = Internal.Logics.EmptyStringLinkedCollection;

		internal PathItems(IEnumerable<string> items)
		{
			mItems = new(items);
		}

		internal PathItems(LinkedCollection<string> items)
		{
			mItems = items;
		}

#else
		private IReadOnlyList<string> mItems =  Internal.Logic.EmptyStringArray;

		internal PathItems(IEnumerable<string> items)
		{
			mItems = new ReadOnlyCollection<string>(items.ToList());
		}
#endif


		internal PathItems()
		{
		}


		private string? mStringCache = null;


		override public string ToString()
		{
			if (mStringCache == null)
			{
				mStringCache = ToString(System.IO.Path.DirectorySeparatorChar);
			}
			return mStringCache;
		}


		virtual public string ToString(char directorySeparatorChar)
		{
			return Internal.Logics.JoinString(directorySeparatorChar, mItems);
		}


		public PathItems CombineItems(PathItems items)
		{
			if (items.Count == 0)
			{
				return this;
			}

			return new PathItems(Internal.Logics.Concat(mItems, items.mItems));
		}


		/// <summary>
		/// 1. マイナスのstartは末尾からの距離と考え、1ステップだけ補正する
		/// 2. マイナスのcountは、末尾からいくつ削るかの指定とみなす
		/// 3. 大きなcountは補正する
		/// 4. 補正後のstartが範囲外の場合はパスを空にする
		/// </summary>
		/// <param name="start">マイナス指定も可</param>
		/// <param name="count">マイナス指定も可</param>
		/// <returns></returns>
		public PathItems SliceItems(int start, int count = int.MaxValue) =>
				new PathItems(Internal.Logics.SliceItems(mItems, start, count));


		/// <summary>
		/// 指定した数だけディレクトリをさかのぼる。Slice(0, -level) と同じ。
		/// </summary>
		/// <param name="level">省略時1。何段階ディレクトリをさかのぼるかを指定</param>
		/// <returns></returns>
		public PathItems AscendItems(int level = 1) => SliceItems(0, -level);


		/// <summary>
		/// ディレクトリを1段階さかのぼる。Ascemd() と同じ。また、Slice(0, -1) とも同じ。
		/// </summary>
		/// <returns></returns>
		public PathItems ParentItems => AscendItems();


		/// <summary>
		/// "." は削除し、".." は解決する。
		/// 少なくとも以下の点について、Win32 PathCanonicalize() 互換。
		/// - 先頭の ".." は削除
		/// - "..." はそのまま残る
		/// </summary>
		/// <returns></returns>
		public PathItems CanonicalizeItems() => new PathItems(Internal.Logics.CanonicalizeItems(mItems));


		/// <summary>(implements IReadOnlyList)</summary>
		public int Count => mItems.Count;


		/// <summary>(implements IReadOnlyList)</summary>
		public string this[int index] => mItems[index];


		/// <summary>(implements IReadOnlyList)</summary>
		public IEnumerator<string> GetEnumerator() => mItems.GetEnumerator();


		/// <summary>(implements IReadOnlyList)</summary>
		IEnumerator IEnumerable.GetEnumerator() => mItems.GetEnumerator();
	}


	/// <summary>File path descriptor</summary>
    public class Filepath
    {
		public static class Parser
		{
			/// <summary>Unix style file path parser. You can set this to Filepath.CurrentParser property.</summary>
			public static IFilepathParser Unix => Internal.UnixFilepathParser.Instance;


			/// <summary>Win32 style file path parser. You can set this to Filepath.CurrentParser property.</summary>
			public static IFilepathParser Win32 => Internal.Win32FilepathParser.Instance;
		}


		public enum Style
		{
			_Unknown,
			Unix,
			Win32,
		}


		/// <summary>System default value, `Style.Unix` or `Style.Win32`</summary>
		public static Style DefaultStyle => Internal.FilepathParsingHelper.DetectFileSystemStyle();


		/// <summary>Gets the system default Filepath parser instance.</summary>
		public static IFilepathParser DefaultParser => Internal.FilepathParsingHelper.GetDefaultFilepathParser();


		/// <summary>Gets the Unix style file path parser instance.</summary>
		public static IFilepathParser UnixParser => Internal.UnixFilepathParser.Instance;


		/// <summary>Gets the MS-DOS/Windows style file path parser instance.</summary>
		public static IFilepathParser Win32Parser => Internal.Win32FilepathParser.Instance;


		private static IFilepathParser? mCurrentParser = null;


		/// <summary>
		/// Get or set IFilepathParser. Default = prepared instance that matches to the system (unix or win32)
		/// </summary>
		public static IFilepathParser CurrentParser
		{
			get
			{
				if (mCurrentParser is null)
				{
					mCurrentParser = Internal.FilepathParsingHelper.GetDefaultFilepathParser();
				}
				return mCurrentParser;
			}

			set
			{
				mCurrentParser = value;
			}
		}


		/// <summary>
		/// empty Filepath object
		/// </summary>
		/// <returns></returns>
		public static readonly Filepath Empty = new();


		/// <summary>
		/// <example>
		/// Traditional DOS path
		/// <code>
		/// var normal_abs  = Parse(@"C:\dir\file.txt");
		/// var normal_rel  = Parse(@"relative\dir\and\file.txt");
		/// var simple_abs  = Parse(@"\");
		/// var simple_abs2 = Parse(@"C:\");
		/// var drive_rel   = Parse(@"C:");  // specifies a drive, but relative
		/// var drive_rel_2 = Parse(@"D:drive-and-relative\dir\file");
		/// </code>
		///
		/// DOS Device path
		/// <code>Parse(@"\\?\volume/dir/more-dir/.git");</code>
		///
		/// UNC path
		/// <code>Parse(@"\\server\share-name\dir\file");</code>
		///
		/// UNIX
		/// <code>
		///   Parse("/usr/local/bin");
		///   Parse("/");  // root
		/// </code>
		/// </example>
		/// </summary>
		/// <param name="path">path string</param>
		/// <returns></returns>
		public static Filepath Parse(string? path) => CurrentParser.Parse(path);


		/// <summary>
		/// Win32向け。UNIX的なパスの場合は空を表す PathPrefix.None 固定
		/// </summary>
		/// <value></value>
		public IPathPrefix Prefix { get; internal set; } = PathPrefix.None.Instance;


		/// <summary>
		/// Represents the path is from root or not.
		/// </summary>
		public bool IsFromRoot { get; internal set; } = false;


		public PathItems Items { get; internal set; } = PathItems.Empty;


		/// <summary>
		/// Itemsの最後の部分。存在しない場合はstring.Emptyを返す
		/// </summary>
		public string LastItem => Items.LastOrDefault() ?? string.Empty;


		/// <summary>
		/// 範囲未チェックのため使用注意
		/// </summary>
		private string _LastItem => Items[Items.Count - 1];


		public bool HasExtension
				=> Items.Count <= 0
					? false
					: global::System.IO.Path.HasExtension(_LastItem);

		/// <summary>
		/// System.IO.Path.GetExtension()に準じる。拡張子が無い場合はstring.Emptyを返す
		/// </summary>
		public string Extension
				=> Items.Count <= 0
					? string.Empty
					: global::System.IO.Path.GetExtension(_LastItem);


		/// <summary>
		/// Itemsの最後の部分。存在しない場合はstring.Emptyを返す。
		///
		/// System.IO.Path.GetFileNameWithoutExtension() に依存する。
		/// 不正な文字が含まれている場合、System.ArgumentException が発生する
		/// </summary>
		public string LastItemWithoutExtension
				=> Items.Count == 0
					? string.Empty
					: global::System.IO.Path.GetFileNameWithoutExtension(_LastItem);


		internal Filepath()
		{
		}


		private string? mStringCache = null;


		override public string ToString()
		{
			if (mStringCache == null)
			{
				mStringCache = ToString(global::System.IO.Path.DirectorySeparatorChar);
			}
			return mStringCache;
		}


		public string ToString(char directorySeparatorChar)
		{
			return Prefix.ToString()
					+ (IsFromRoot ? directorySeparatorChar : "")
					+ Items.ToString(directorySeparatorChar);
		}


		public Filepath Combine(PathItems items)
		{
			if (items.Count == 0)
			{
				return this;
			}

			return new Filepath
			{
				Prefix = this.Prefix,
				IsFromRoot = this.IsFromRoot,
				Items = this.Items.CombineItems(items),
			};
		}


		/// <summary>
		///  var fp = Filepath.Parse(@"C:\d1\d2\d3\file.txt");
		///  var sub = fp.Slice(1, 2); //=> Prefix="C:",  Items= [ d2, d3 ]
		///  sub.ToString();  //=> "C:d2/d3"  (a prefix and RELATIVE paths)
		///
		/// 1. マイナスのstartは末尾からの距離と考え、1ステップだけ補正する
		/// 2. マイナスのcountは、末尾からいくつ削るかの指定とみなす
		/// 3. 大きなcountは補正する
		/// 4. 補正後のstartが範囲外の場合はパスを空にする
		/// </summary>
		/// <param name="start">マイナス指定も可</param>
		/// <param name="count">マイナス指定も可</param>
		/// <returns></returns>
		public Filepath Slice(int start, int count = int.MaxValue)
		{
			var fixedHead =  Internal.Logics._FixHeadIndex(Items.Count, start);

			return new Filepath
			{
				Prefix = this.Prefix,

				// 開始位置が 0 であれば、this.IsFromRoot を踏襲。そうでなければ常に相対パスとなる
				IsFromRoot = (fixedHead == 0) ? this.IsFromRoot : false,

				Items = Items.SliceItems(start, count),
			};
		}


		/// <summary>
		/// 指定した数だけディレクトリをさかのぼる。Slice(0, -aLevel) と同じ。
		/// </summary>
		/// <param name="level">省略時1。何段階ディレクトリをさかのぼるかを指定</param>
		/// <returns></returns>
		public Filepath Ascend(int level = 1) => Slice(0, -level);


		/// <summary>
		/// ディレクトリを1段階さかのぼる。Ascemd() と同じ。また、Slice(0, -1) とも同じ。
		/// </summary>
		/// <returns></returns>
		public Filepath Parent => Ascend();


		/// <summary>
		/// "." は削除し、".." は解決する。
		/// 少なくとも以下の点について、Win32 PathCanonicalize() 互換。
		/// - 先頭の ".." は削除
		/// - "..." はそのまま残る
		/// </summary>
		/// <returns></returns>
		public Filepath Canonicalize()
		{
			return new Filepath
			{
				Prefix = Prefix,
				IsFromRoot = IsFromRoot,
				Items = Items.CanonicalizeItems(),
			};
		}
    }
}


namespace Tkuri2010.Fsuty.PathPrefix
{
	public class None : IPathPrefix
	{
		public static readonly None Instance = new None();

		override public string ToString()
		{
			return string.Empty;
		}
	}


	public interface IDosDevice : IPathPrefix
	{
		string SignChar { get; }

		string Volume { get; }
	}


	public interface IHasDrive : IPathPrefix
	{
		/// <summary>
		/// ドライブ名。"C:" などコロン付き
		/// </summary>
		/// <value></value>
		string Drive { get; }

		/// <summary>
		/// ドライブ文字
		/// </summary>
		/// <value></value>
		string DriveLetter { get; }
	}


	public interface IShared
	{
		/// <summary>
		/// サーバ
		/// </summary>
		/// <value></value>
		string Server { get; }

		/// <summary>
		/// 共有名
		/// </summary>
		/// <value></value>
		string Share { get; }
	}


	/// <summary>
	/// - "\\.\VOLUME{ASDFASDF}\foo\bar.txt"
	/// </summary>
	public class DosDevice : IDosDevice
	{
		/// <summary>
		/// matches "//./", "//?/"
		/// </summary>
		public static readonly Regex PrefixPattern = new Regex(@"^//(\.|\?)(?=/)");


		/// <summary>
		/// matches "/volume-string", "/C:"   ~~"/C$"~~ ←これは勘違い
		/// </summary>
		public static readonly Regex VolumePattern = new Regex(@"^/+([^/]+)");


		public static bool TryParse(Internal.FilepathScanner aScan, [NotNullWhen(true)] out DosDevice? oResult)
		{
			oResult = null;

			if (! aScan.Skip(PrefixPattern, out var match))
			{
				return false;
			}

			oResult = new DosDevice();
			oResult.SignChar = match.Groups[1].Value;

			if (aScan.Skip(VolumePattern, out var volumePart))
			{
				// \\.\some-volume\...
				oResult.Volume = volumePart.Groups[1].Value;
			}

			return true;
		}


		/// <summary>
		/// 「.」または「?」
		/// </summary>
		/// <value></value>
		public string SignChar { get; private set; } = string.Empty;


		/// <summary>
		/// ボリューム文字列。"VOLUME{ASDFASDF}"など
		/// </summary>
		/// <value></value>
		public string Volume { get; private set; } = string.Empty;


		override public string ToString()
			=> $@"\\{SignChar}\{Volume}";
	}


	/// <summary>
	/// - "\\?\C:\foo\bar.txt"
	/// - "//?/C:/foo/bar.txt" (forward slash allowed)
	/// - ~~"\\?\C$\foo\bar.txt"~~  これは違う。何か勘違いしていた？
	/// </summary>
	public class DosDeviceDrive : IDosDevice, IHasDrive
	{
		/// <summary>
		/// matches "//./C:", "//?/R:"
		/// </summary>
		public static readonly Regex PrefixPattern = new Regex(
			 	(@"^//(\.|\?)"
					+ @"/(([a-zA-Z])" + Internal.FilepathParsingHelper.MsdosVolumeSeparatorChar + ")")
				.Replace(" ", ""));


		public static bool TryParse(Internal.FilepathScanner aScan, [NotNullWhen(true)] out DosDeviceDrive? oResult)
		{
			oResult = null;

			if (! aScan.Skip(PrefixPattern, out var match))
			{
				return false;
			}

			oResult = new DosDeviceDrive
			{
				SignChar = match.Groups[1].Value,
				Drive = match.Groups[2].Value,
				DriveLetter = match.Groups[3].Value,
			};

			return true;
		}


		/// <summary>
		/// 「.」または「?」
		/// </summary>
		/// <value></value>
		public string SignChar { get; private set; } = string.Empty;


		/// <summary>
		/// drive ("C:" , "Z:", and so on....)
		/// </summary>
		/// <value></value>
		public string Drive { get; private set; } = string.Empty;


		/// <summary>
		/// drive letter
		/// </summary>
		/// <value></value>
		public string DriveLetter { get; private set; } = string.Empty;


		/// <summary>
		/// ドライブ指定のパスにおける「ボリューム」とは、"C:" など
		/// </summary>
		/// <value></value>
		public string Volume => Drive;


		/// <summary>
		/// "\\.\" + drive (or volume) + volume separator
		/// </summary>
		/// <returns></returns>
		override public string ToString() => $@"\\{SignChar}\{Volume}";
	}


	/// <summary>
	/// - "\\?\UNC\server\share-name\foo\bar.txt"
	/// - "\\?\UNC\127.0.0.1\share-name\foo\bar.txt"
	/// </summary>
	public class DosDeviceUnc : IDosDevice, IShared
	{
		/// <summary>
		/// matches "//./UNC", "//./UNC/server", "//?/UNC/server/share-name"
		/// </summary>
		public static readonly Regex UncPattern = new Regex(
				@"^//(\.|\?)/UNC (/+ (W+) (/+ (W+) )? )?"
				.Replace("W", "[^/]")
				.Replace(" ", "")
				);


		public static bool TryParse(Internal.FilepathScanner aScan, [NotNullWhen(true)] out DosDeviceUnc? oResult)
		{
			oResult = null;

			if (! aScan.Skip(UncPattern, out var asUnc))
			{
				return false;
			}

			oResult = new DosDeviceUnc
			{
				SignChar = asUnc.Groups[1].Value,
				Server = asUnc.Groups[3].Value,
				Share = asUnc.Groups[5].Value,
			};
			oResult.Volume = oResult.Server + asUnc.Groups[4].Value.Replace('/', System.IO.Path.DirectorySeparatorChar);
			return true;
		}


		/// <summary>
		/// 「.」または「?」
		/// </summary>
		/// <value></value>
		public string SignChar { get; private set; } = string.Empty;

		/// <summary>
		/// サーバ
		/// </summary>
		/// <value></value>
		public string Server { get; private set; } = string.Empty;

		/// <summary>
		/// 共有名
		/// </summary>
		/// <value></value>
		public string Share { get; private set; } = string.Empty;

		/// <summary>
		///  UNC におけるボリュームは、 "Server\ShareName" のような文字列
		/// </summary>
		/// <value></value>
		public string Volume { get; private set; } = string.Empty;

		override public string ToString()
			=> $@"\\{SignChar}\UNC\{Volume}";
	}


	/// <summary>
	/// - "\\server\share-name\dir\file.txt"
	/// </summary>
	public class Unc : IPathPrefix, IShared
	{
		/// <summary>
		/// matches "//server/share-name"
		/// </summary>
		public static readonly Regex PrefixPattern = new Regex(@"^//([^/]+)/+([^/]+)");


		public static bool TryParse(Internal.FilepathScanner aScan, [NotNullWhen(true)] out Unc? oResult)
		{
			oResult = null;

			if (! aScan.Skip(PrefixPattern, out var match))
			{
				return false;
			}

			oResult = new Unc();
			oResult.Server = match.Groups[1].Value;
			oResult.Share = match.Groups[2].Value;
			return true;
		}


		/// <summary>
		/// サーバ名。「LOCALHOST」など。
		/// </summary>
		/// <value></value>
		public string Server { get; private set; } = string.Empty;


		/// <summary>
		/// 共有名。「C$」「shared」など。
		/// </summary>
		/// <value></value>
		public string Share { get; private set; } = string.Empty;


		override public string ToString()
			=> $@"\\{Server}\{Share}";
	}



	/// <summary>
	/// Traditional DOS
	/// </summary>
	public class Dos : IHasDrive
	{
		/// <summary>
		/// matches "c:", "G:" etc...
		/// </summary>
		/// <returns></returns>
		public static readonly Regex PrefixPattern = new Regex(@"^([a-zA-Z])" + Internal.FilepathParsingHelper.MsdosVolumeSeparatorChar);


		public static bool TryParse(Internal.FilepathScanner aScan, [NotNullWhen(true)] out Dos? oResult)
		{
			oResult = null;

			if (! aScan.Skip(PrefixPattern, out var match))
			{
				return false;
			}

			oResult = new Dos
			{
				Drive = match.Value,
				DriveLetter = match.Groups[1].Value,
			};

			return true;
		}


		/// <summary>
		/// drive ("C:" , "Z:", and so on....)
		/// </summary>
		/// <value></value>
		public string Drive { get; private set; } = string.Empty;


		/// <summary>
		/// drive letter
		/// </summary>
		/// <value></value>
		public string DriveLetter { get; private set; } = string.Empty;


		/// <summary>
		/// drive (or volume) + volume separator
		/// </summary>
		/// <returns></returns>
		override public string ToString() => Drive;
	}


	/// <summary>
	/// FIXME: FAKE! (since .NET Standard 2.1, System.Diagnostics.CodeAnalysis)
	/// </summary>
	internal class NotNullWhenAttribute : Attribute
	{
		/// <summary>
		/// FIXME: FAKE!
		/// </summary>
		/// <param name="_b"></param>
		internal NotNullWhenAttribute(bool _b) {}
	}

}


namespace Tkuri2010.Fsuty.Internal
{
	public static class Logics
	{
		public static readonly ReadOnlyCollection<string> EmptyStringArray = new(new string[0]{ });

		public static readonly LinkedCollection<string> EmptyStringLinkedCollection = new();


		/// <summary>
		/// 1. マイナスのstartは末尾からの距離と考え、1ステップだけ補正する
		/// 2. マイナスのcountは、末尾からいくつ削るかの指定とみなす
		/// 3. 大きなcountは補正する
		/// 4. 補正後のstartが範囲外の場合はパスを空にする
		/// </summary>
		/// <param name="items">アイテム列</param>
		/// <param name="start">マイナス指定も可</param>
		/// <param name="count">マイナス指定も可</param>
		/// <returns></returns>
		public static IReadOnlyList<string> SliceItems(IReadOnlyCollection<string> items, int start, int count = int.MaxValue)
		{
			var fixedHead = _FixHeadIndex(items.Count, start);
			var fixedTail = _FixTailIndex(items.Count, fixedHead, count);
			var fixedCount = fixedTail - fixedHead;

			return (0 <= fixedHead) && (fixedHead < items.Count) && (1 <= fixedCount)
					? items.Skip(fixedHead).Take(fixedCount).ToArray()
					: EmptyStringArray;
		}


		/// <summary>
		/// 1. マイナスのstartは末尾からの距離と考え、1ステップだけ補正する
		/// 2. マイナスのcountは、末尾からいくつ削るかの指定とみなす
		/// 3. 大きなcountは補正する
		/// 4. 補正後のstartが範囲外の場合はパスを空にする
		/// </summary>
		/// <param name="items">アイテム列</param>
		/// <param name="start">マイナス指定も可</param>
		/// <param name="count">マイナス指定も可</param>
		/// <returns></returns>
		public static LinkedCollection<string> SliceItems(LinkedCollection<string> items, int start, int count = int.MaxValue)
		{
			var fixedHead = _FixHeadIndex(items.Count, start);
			var fixedTail = _FixTailIndex(items.Count, fixedHead, count);
			var fixedCount = fixedTail - fixedHead;

			return (0 <= fixedHead) && (fixedHead < items.Count) && (1 <= fixedCount)
					? items.Slice(fixedHead, fixedCount)
					: EmptyStringLinkedCollection;
		}


		public static int _FixHeadIndex(int itemCount, int aNum)
		{
			return (aNum < 0) ? aNum + itemCount : aNum;
		}


		static int _FixTailIndex(int itemsCount, int fixedHead, int wantCount)
		{
			var bigTailIndex = (0 <= wantCount)
					? (long) fixedHead + (long) wantCount
					: (long) itemsCount + (long) wantCount;

			return _SaturateIntoInt(0, bigTailIndex, itemsCount);
		}


		static int _SaturateIntoInt(int aIntMin, long aLongValue, int aIntMax)
		{
			return (int) Math.Max((long)aIntMin, Math.Min(aLongValue, (long)aIntMax));
		}


		/// <summary>
		/// "item1/./item2"  →  "item1/item2"
		/// "itemA/dir/../itemB"  →  "itemA/itemB"
		/// </summary>
		public static IReadOnlyList<string> CanonicalizeItems(IEnumerable<string> items)
		{
			var canon = new List<string>();
			foreach (var item in items)
			{
				if (item == ".")
				{
					continue;
				}
				if (item == "..")
				{
					if (1 <= canon.Count) canon.RemoveAt(canon.Count - 1);
					continue;
				}
				canon.Add(item);
			}

			return canon;
		}


		/// <summary>IEnumerable + IEnumerable</summary>
		public static IEnumerable<string> Concat(IEnumerable<string> a, IEnumerable<string> b)
		{
			return a.Concat(b);
		}


		/// <summary>LinkedCollection + IEnumerable</summary>
		public static LinkedCollection<string> Concat(LinkedCollection<string> a, IEnumerable<string> b)
		{
			return a.AppendRange(b);
		}


		//public static string JoinString(char separator, IEnumerable<object> values)
		//{
		//	return string.Join(separator.ToString(), values);
		//}


		public static string JoinString(char separator, LinkedCollection<string> values)
		{
			var builder = new StringBuilder();

			var first = true;
			var rev = values.GetReverseEnumerator();
			rev.Reset();
			while (rev.MoveNext())
			{
				if (first) first = false;
				else
				{
					builder.Insert(0, separator);
				}
				builder.Insert(0, rev.Current);
			}

			return builder.ToString();
		}
	}


	public class FilepathScanner
	{
		public string Input { get; private set; }


		public FilepathScanner(string input)
		{
			Input = input;
		}


		public bool Skip(Regex regex)
		{
			return Skip(regex, out var _m);
		}


		public bool Skip(Regex regex, out Match oMatchResult)
		{
			oMatchResult = regex.Match(Input);

			if (oMatchResult.Success)
			{
				Input = Input.Substring(oMatchResult.Length);
			}

			return oMatchResult.Success;
		}
	}


	public static class FilepathParsingHelper
	{
		public static Filepath.Style DetectFileSystemStyle()
		{
			var c1 = System.IO.Path.DirectorySeparatorChar;
			var c2 = System.IO.Path.AltDirectorySeparatorChar;
			var v = System.IO.Path.VolumeSeparatorChar;
			if (c1 == '/' && c2 == '/' && v == '/')
			{
				return Filepath.Style.Unix;
			}

			if (((c1 == '/' && c2 == '\\') || (c1 == '\\' && c2 == '/'))
				&& v == ':')
			{
				return Filepath.Style.Win32;
			}

			return Filepath.Style._Unknown;
		}


		private static Filepath.Style _detectedFileSystemStyle = Filepath.Style._Unknown;


		private static Filepath.Style _DetectAndCacheFileSystemStyle()
		{
			if (_detectedFileSystemStyle == Filepath.Style._Unknown)
			{
				_detectedFileSystemStyle = DetectFileSystemStyle();
			}

			return _detectedFileSystemStyle;
		}


		public static bool SeemsUnixStyle => _DetectAndCacheFileSystemStyle() == Filepath.Style.Unix;


		public static bool SeemsWin32Style => _DetectAndCacheFileSystemStyle() == Filepath.Style.Win32;


		public static IFilepathParser GetDefaultFilepathParser()
		{
			if (SeemsUnixStyle)
			{
				return UnixFilepathParser.Instance;
			}
			else
			{
				return Win32FilepathParser.Instance;
			}
		}


		/// <summary>
		/// character ":"
		/// </summary>
		public static readonly char MsdosVolumeSeparatorChar = ':';


		public static readonly Regex SeparatingPattern = new Regex("^/+");


		public static readonly Regex ItemPattern = new Regex("^([^/]+)");


		public static IEnumerable<string> ParsePath(FilepathScanner aScan)
		{
			while (aScan.Skip(ItemPattern, out var match))
			{
				yield return match.Groups[1].Value;

				aScan.Skip(SeparatingPattern);
			}
		}


		public static IPathPrefix ParseWin32PathPrefix(FilepathScanner aScan)
		{
			// FIXME 以下の TryParse() の out 引数は null にはならないが、
			// .NET Standard 2.0 では NotNullWhenAttribute が存在しないため、コンパイラは warn を出してしまう。
			// .NET Standard 2.1 以降であれば解決する。
			#pragma warning disable CS8603
			if (PathPrefix.Dos.TryParse(aScan, out var traDos))
			{
				return traDos;
			}
			else if (PathPrefix.DosDeviceUnc.TryParse(aScan, out var pxDosDecUnc))
			{
				return pxDosDecUnc;
			}
			else if (PathPrefix.DosDeviceDrive.TryParse(aScan, out var pxDosDevDrive))
			{
				return pxDosDevDrive;
			}
			else if (PathPrefix.DosDevice.TryParse(aScan, out var pxDosDev))
			{
				return pxDosDev;
			}
			else if (PathPrefix.Unc.TryParse(aScan, out var justUnc))
			{
				return justUnc;
			}
			else
			{
				return PathPrefix.None.Instance;
			}
			#pragma warning restore CS8603
		}
	}


	public class UnixFilepathParser : IFilepathParser
	{
		public static readonly UnixFilepathParser Instance = new();


		public Filepath Parse(string? path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return Filepath.Empty;
			}

			// FIXME .NET Standard 2.1 以降であればコンパイラは path が null でない事を理解してくれる
			#pragma warning disable CS8604
			var scan = new Internal.FilepathScanner(path);
			#pragma warning restore CS8604

			var self = new Filepath();
			self.Prefix = PathPrefix.None.Instance;
			self.IsFromRoot = scan.Skip(FilepathParsingHelper.SeparatingPattern);
			self.Items = new(FilepathParsingHelper.ParsePath(scan));
			return self;
		}
	}


	public class Win32FilepathParser : IFilepathParser
	{
		public static readonly Win32FilepathParser Instance = new();


		public Filepath Parse(string? path)
		{
			if (string.IsNullOrEmpty(path))
			{
				return Filepath.Empty;
			}

			// FIXME .NET Standard 2.1 以降であればコンパイラは path が null でない事を理解してくれる
			#pragma warning disable CS8604
			var scan = PrepareScanner(path);
			#pragma warning restore CS8604


			var self = new Filepath();
			self.Prefix = FilepathParsingHelper.ParseWin32PathPrefix(scan);
			self.IsFromRoot = scan.Skip(FilepathParsingHelper.SeparatingPattern);
			self.Items = new(FilepathParsingHelper.ParsePath(scan));
			return self;
		}


		public static FilepathScanner PrepareScanner(string inputPath)
		{
			return new FilepathScanner(inputPath.Replace('\\', '/'));
		}
	}

}

