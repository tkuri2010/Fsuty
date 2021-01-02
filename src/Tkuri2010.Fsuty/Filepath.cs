using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tkuri2010.Fsuty
{
	public static class PathLogics
	{
		public static readonly ReadOnlyCollection<string> EmptyStringArray = new(new string[0]{ });


		/// <summary>
		/// 1. マイナスのstartは末尾からの距離と考え、1ステップだけ補正する
		/// 2. マイナスのcountは、末尾からいくつ削るかの指定とみなす
		/// 3. 大きなcountは補正する
		/// 4. 補正後のstartが範囲外の場合はパスを空にする
		/// </summary>
		/// <param name="start">マイナス指定も可</param>
		/// <param name="count">マイナス指定も可</param>
		/// <returns></returns>
		public static IReadOnlyList<string> SliceItems(IEnumerable<string> items, int start, int count = int.MaxValue)
		{
			var fixedHead =  _FixHeadIndex(items.Count(), start);
			var fixedTail = _FixTailIndex(items.Count(), fixedHead, count);
			var fixedCount = fixedTail - fixedHead;

			return (0 <= fixedHead) && (fixedHead < items.Count()) && (1 <= fixedCount)
					? items.Skip(fixedHead).Take(fixedCount).ToArray()
					: EmptyStringArray;
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
	}


	public class PathItems : IReadOnlyList<string>
	{
		public static readonly PathItems Empty = new();

		private IReadOnlyList<string> _items =  PathLogics.EmptyStringArray;


		internal PathItems()
		{
		}


		internal PathItems(IEnumerable<string> items)
		{
			this._items = new ReadOnlyCollection<string>(items.ToList());
		}


		private string? mStringCache = null;


		override public string ToString()
		{
			if (mStringCache == null)
			{
				mStringCache = ToString(System.IO.Path.DirectorySeparatorChar.ToString());
			}
			return mStringCache;
		}


		virtual public string ToString(string directorySeparator)
		{
			return string.Join(directorySeparator, _items);
		}


		public PathItems CombineItems(PathItems items)
		{
			if (items.Count == 0)
			{
				return this;
			}

			return new PathItems(_items.Concat(items._items));
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
				new PathItems(PathLogics.SliceItems(_items, start, count));


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
		public PathItems CanonicalizeItems() => new PathItems(PathLogics.CanonicalizeItems(_items));


		/// <summary>(implements IReadOnlyList)</summary>
		public int Count => _items.Count;


		/// <summary>(implements IReadOnlyList)</summary>
		public string this[int index] => _items[index];


		/// <summary>(implements IReadOnlyList)</summary>
		public IEnumerator<string> GetEnumerator() => _items.GetEnumerator();


		/// <summary>(implements IReadOnlyList)</summary>
		IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
	}


    public class Filepath
    {
		/// <summary>
		/// character ":"
		/// </summary>
		public static readonly char MsdosVolumeSeparatorChar = ':';


		static readonly Regex SeparatingPattern = new Regex("^/+");


		static readonly Regex ItemPattern = new Regex("^([^/]+)");


		public static readonly Filepath Empty = new Filepath();


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
		/// <param name="aInput"></param>
		/// <returns></returns>
		public static Filepath Parse(string? aInput)
		{
			if (string.IsNullOrEmpty(aInput))
			{
				return Empty;
			}

			var scan = new FilepathScanner(aInput);

			var self = new Filepath();
			self.Prefix = _ParsePrefix(scan);
			self.IsAbsolute = scan.Skip(SeparatingPattern);
			self.Items = new(_ParsePath(scan));
			return self;
		}


		static IPathPrefix _ParsePrefix(FilepathScanner aScan)
		{
			if (PathPrefix.Dos.TryParse(aScan, out var traDos))
			{
				return traDos!;
			}
			else if (PathPrefix.DosDeviceUnc.TryParse(aScan, out var pxDosDecUnc))
			{
				return pxDosDecUnc!;
			}
			else if (PathPrefix.DosDeviceDrive.TryParse(aScan, out var pxDosDevDrive))
			{
				return pxDosDevDrive!;
			}
			else if (PathPrefix.DosDevice.TryParse(aScan, out var pxDosDev))
			{
				return pxDosDev!;
			}
			else if (PathPrefix.Unc.TryParse(aScan, out var justUnc))
			{
				return justUnc!;
			}
			else
			{
				return PathPrefix.None.Instance;
			}
		}


		static IEnumerable<string> _ParsePath(FilepathScanner aScan)
		{
			while (aScan.Skip(ItemPattern, out var match))
			{
				yield return match.Groups[1].Value;

				aScan.Skip(SeparatingPattern);
			}
		}


		/// <summary>
		/// Win32向け。UNIX的なパスの場合は空を表す PathPrefixEmpty 固定
		/// </summary>
		/// <value></value>
		public IPathPrefix Prefix { get; private set; } = PathPrefix.None.Instance;


		public bool IsAbsolute { get; private set; } = false;


		public PathItems Items { get; private set; } = PathItems.Empty;


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
					: System.IO.Path.HasExtension(_LastItem);

		/// <summary>
		/// System.IO.Path.GetExtension()に準じる。拡張子が無い場合はstring.Emptyを返す
		/// </summary>
		public string Extension
				=> Items.Count <= 0
					? string.Empty
					: System.IO.Path.GetExtension(_LastItem);


		/// <summary>
		/// Itemsの最後の部分。存在しない場合はstring.Emptyを返す。
		///
		/// System.IO.Path.GetFileNameWithoutExtension() に依存する。
		/// 不正な文字が含まれている場合、System.ArgumentException が発生する
		/// </summary>
		public string LastItemWithoutExtension
				=> Items.Count == 0
					? string.Empty
					: System.IO.Path.GetFileNameWithoutExtension(_LastItem);


		private Filepath()
		{
		}


		private string? mStringCache = null;


		override public string ToString()
		{
			if (mStringCache == null)
			{
				mStringCache = ToString(System.IO.Path.DirectorySeparatorChar.ToString());
			}
			return mStringCache;
		}


		public string ToString(string directorySeparator)
		{
			return Prefix.ToString()
					+ (IsAbsolute ? directorySeparator : "")
					+ Items.ToString(directorySeparator);
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
				IsAbsolute = this.IsAbsolute,
				Items = this.Items.CombineItems(items),
			};
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
		public Filepath Slice(int start, int count = int.MaxValue)
		{
			var fixedHead =  PathLogics._FixHeadIndex(Items.Count, start);

			return new Filepath
			{
				Prefix = this.Prefix,

				// 開始位置が 0 であれば、this.Absolute を踏襲。そうでなければ常に相対パスとなる
				IsAbsolute = (fixedHead == 0) ? this.IsAbsolute : false,

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
				IsAbsolute = IsAbsolute,
				Items = Items.CanonicalizeItems(),
			};
		}
    }


	public class FilepathScanner
	{
		public string Input { get; private set; }


		public static string _Prepare(string aInput)
		{
			return aInput.Replace('\\', '/');
		}


		public FilepathScanner(string aInput)
		{
			Input = _Prepare(aInput);
		}


		public bool Skip(Regex aReg)
		{
			Match m;
			return Skip(aReg, out m);
		}


		public bool Skip(Regex aReg, out Match oMatchResult)
		{
			oMatchResult = aReg.Match(Input);

			if (oMatchResult.Success)
			{
				Input = Input.Substring(oMatchResult.Length);
			}

			return oMatchResult.Success;
		}
	}


	public interface IPathPrefix
	{
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


		public static bool TryParse(FilepathScanner aScan, out DosDevice? oResult)
		{
			oResult = null;

			if (!aScan.Skip(PrefixPattern, out var match))
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
					+ @"/(([a-zA-Z])" + Filepath.MsdosVolumeSeparatorChar + ")")
				.Replace(" ", ""));


		public static bool TryParse(FilepathScanner aScan, out DosDeviceDrive? oResult)
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


		public static bool TryParse(FilepathScanner aScan, out DosDeviceUnc? oResult)
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


		public static bool TryParse(FilepathScanner aScan, out Unc? oResult)
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
		public static readonly Regex PrefixPattern = new Regex(@"^([a-zA-Z])" + Filepath.MsdosVolumeSeparatorChar);


		public static bool TryParse(FilepathScanner aScan, out Dos? oResult)
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
}
