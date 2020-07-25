using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Tkuri2010.Fsuty
{
    public class Filepath
    {
		/// <summary>
		/// character ":"
		/// </summary>
		public static readonly char MSDOS_VOLUME_SEPARATOR_CHAR = ':';


		static readonly string[] _EMPTY = {};


		static readonly Regex SEPARATING_PATTERN = new Regex("^/+");


		static readonly Regex ITEM_PATTERN = new Regex("^([^/]+)");


		public static readonly Filepath Empty = new Filepath();


		/// <summary>
		/// Traditional DOS path
		///   Parse(@"C:\dir\file.txt");
		///   Parse(@"relative\dir\and\file.txt");
		///   Parse(@"\");  // root
		///   Parse(@"C:\");  // drive and root
		///   Parse(@"C:");  // specifies a drive, but relative
		///   Parse(@"D:drive-and-relative\dir\file");
		/// 
		/// DOS Device path
		///   Parse(@"\\?\volume/dir/more-dir/.git");
		/// 
		/// UNC path
		///   Parse(@"\\server\share-name\dir\file");
		/// 
		/// UNIX 
		///   Parse("/usr/local/bin");
		///   Parse("/");  // root
		/// </summary>
		/// <param name="aInput"></param>
		/// <returns></returns>
		public static Filepath Parse(string? aInput)
		{
			if (string.IsNullOrEmpty(aInput))
			{
				return Empty;
			}

			var scan = new FilepathScanner(aInput ?? "");

			var self = new Filepath();
			self.Prefix = _ParsePrefix(scan);
			self.Absolute = scan.Skip(SEPARATING_PATTERN);
			self.Items = _ParsePath(scan).ToArray();
			return self;
		}


		static IPathPrefix _ParsePrefix(FilepathScanner aScan)
		{
			if (PathPrefix.Dos.TryParse(aScan, out var traDos))
			{
				return traDos!;
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
				return PathPrefix.Empty.STATIC;
			}
		}


		static IEnumerable<string> _ParsePath(FilepathScanner aScan)
		{
			while (aScan.Skip(ITEM_PATTERN, out var match))
			{
				yield return match.Groups[1].Value;

				aScan.Skip(SEPARATING_PATTERN);
			}
		}


		/// <summary>
		/// Win32向け。UNIX的なパスの場合は空を表す PathPrefixEmpty 固定
		/// </summary>
		/// <value></value>
		public IPathPrefix Prefix { get; private set; } = PathPrefix.Empty.STATIC;


		public bool Absolute { get; private set; } = false;



		/// <summary>
		/// Directories and a file.
		/// (prefixes(e.g. drive letter, hostname) are not included)
		/// </summary>
		/// <value></value>
		public string[] Items { get; private set; } = _EMPTY;


		/// <summary>
		/// Itemsの最後の部分。存在しない場合はstring.Emptyを返す
		/// </summary>
		public string LastItem => Items.LastOrDefault() ?? string.Empty;


		/// <summary>
		/// 範囲未チェックのため使用注意
		/// </summary>
		private string _LastItem => Items[Items.Length - 1];


		public bool HasExtension
				=> Items.Length <= 0
					? false
					: System.IO.Path.HasExtension(_LastItem);

		/// <summary>
		/// System.IO.Path.GetExtension()に準じる。拡張子が無い場合はstring.Emptyを返す
		/// </summary>
		public string Extension
				=> Items.Length <= 0
					? string.Empty
					: System.IO.Path.GetExtension(_LastItem);


		/// <summary>
		/// Itemsの最後の部分。存在しない場合はstring.Emptyを返す。
		/// 
		/// System.IO.Path.GetFileNameWithoutExtension() に依存する。
		/// 不正な文字が含まれている場合、System.ArgumentException が発生する
		/// </summary>
		public string LastItemWithoutExtension
				=> Items.Length == 0
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
					+ (Absolute ? directorySeparator : "")
					+ string.Join(directorySeparator, Items);
		}


		/// <summary>
		/// - "c:/dir1" + "dir2/file.txt" => "c:/dir1/dir2/file.txt"
		/// - "c:/dir1" + "/from-root/file.txt"  => "c:/from-root/file.txt"
		/// - "c:/dir1" + "d:dir2/file.txt" (drive and relative path)  => "d:/dir1/dir2/file.txt"
		/// - "c:/dir1" + "\\.\server\share-name\dir2\file.txt" => "\\.\server\share-name\dir2\file.txt"
		/// </summary>
		/// <param name="aOther"></param>
		/// <returns>new Filepath instance.</returns>
		public Filepath Combine(string aOther)
		{
			if (string.IsNullOrEmpty(aOther))
			{
				return this;
			}
			return Combine(Filepath.Parse(aOther));
		}


		public Filepath Combine(Filepath aOtherPath)
		{
			if (aOtherPath == null)
			{
				return this;
			}

			return new Filepath
			{
				Prefix = _SelectPrefix(aOtherPath.Prefix, this.Prefix),
				Absolute = this.Absolute || aOtherPath.Absolute,
				Items = aOtherPath.Absolute
						? aOtherPath.Items
						: this.Items.Concat(aOtherPath.Items).ToArray()
			};
		}


		static IPathPrefix _SelectPrefix(IPathPrefix a, IPathPrefix b)
		{
			return (a is PathPrefix.Empty) ? b : a;
		}


		/// <summary>
		/// 1. マイナスのstartは末尾からの距離と考え、1ステップだけ補正する
		/// 2. マイナスのcountは、末尾からいくつ削るかの指定とみなす
		/// 3. 大きなcountは補正する
		/// 4. 補正後のstartが範囲外の場合はパスを空にする
		/// </summary>
		/// <param name="aStart">マイナス指定も可</param>
		/// <param name="aCount">マイナス指定も可</param>
		/// <returns></returns>
		public Filepath Slice(int aStart, int aCount = int.MaxValue)
		{
			var rv = new Filepath
			{
				Prefix = this.Prefix,
			};

			var fixedHead =  _FixHeadIndex(aStart);
			var fixedTail = _FixTailIndex(fixedHead, aCount);
			var fixedCount = fixedTail - fixedHead;

			// 開始位置が 0 であれば、this.Absolute を踏襲。そうでなければ常に相対パスとなる
			rv.Absolute = (fixedHead == 0) ? this.Absolute : false;

			// countが1以上の場合のみ、Itemsを切り出して取得
			rv.Items = (0 <= fixedHead) && (fixedHead < Items.Length) && (1 <= fixedCount)
					? this.Items.Skip(fixedHead).Take(fixedCount).ToArray()
					: _EMPTY;

			return rv;
		}


		int _FixHeadIndex(int aNum)
		{
			return (aNum < 0) ? aNum + Items.Length : aNum;
		}


		int _FixTailIndex(int aFixedHead, int aCount)
		{
			var bigTailIndex = 0 <= aCount
					? (long) aFixedHead + (long) aCount
					: (long) Items.Length + (long) aCount;

			return _SaturateIntoInt(0, bigTailIndex, Items.Length);
		}

		static int _SaturateIntoInt(int aIntMin, long aLongValue, int aIntMax)
		{
			return (int) Math.Max((long)aIntMin, Math.Min(aLongValue, (long)aIntMax));
		}


		/// <summary>
		/// 指定した数だけディレクトリをさかのぼる。Slice(0, -aLevel) と同じ。
		/// </summary>
		/// <param name="aLevel">省略時1。何段階ディレクトリをさかのぼるかを指定</param>
		/// <returns></returns>
		public Filepath Ascend(int aLevel = 1) => Slice(0, -aLevel);


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
			var canon = new List<string>();
			foreach (var item in Items)
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

			return new Filepath
			{
				Prefix = Prefix,
				Absolute = Absolute,
				Items = canon.ToArray(),
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
	public class Empty : IPathPrefix
	{
		public static readonly Empty STATIC = new Empty();

		override public string ToString()
		{
			return string.Empty;
		}
	}


	/// <summary>
	/// "\\.\VOLUME{ASDFASDF}\foo\bar.txt"
	/// "\\?\C$\foo\bar.txt"
	/// "\\?\UNC\server\share-name\foo\bar.txt"
	/// </summary>
	public class DosDevice : IPathPrefix
	{
		/// <summary>
		/// matches "//./", "//?/"
		/// </summary>
		public static readonly Regex PREFIX_PATTERN = new Regex(@"^//(\.|\?)(?=/)");


		/// <summary>
		/// matches "/UNC", "/UNC/server", "/UNC/server/share-name"
		/// </summary>
		public static readonly Regex UNC_PATTERN = new Regex(
				@"^/+UNC (/+ ([^/]+) (/+ ([^/]+) )? )?"
				.Replace(" ", "")
				);


		/// <summary>
		/// matches "/volume-string", "/C$"
		/// </summary>
		public static readonly Regex VOLUME_PATTERN = new Regex(@"^/+([^/]+)");


		public static bool TryParse(FilepathScanner aScan, out DosDevice? oResult)
		{
			oResult = null;

			if (!aScan.Skip(PREFIX_PATTERN, out var match))
			{
				return false;
			}

			oResult = new DosDevice();
			oResult.SignChar = match.Groups[1].Value;

			if (aScan.Skip(UNC_PATTERN, out var asUnc))
			{
				// \\.\UNC\serever\share-name\....
				oResult.IsUnc = true;
				oResult.Server = asUnc.Groups[2].Value;
				oResult.Share = asUnc.Groups[4].Value;
				oResult.Volume = oResult.Server + asUnc.Groups[3].Value.Replace('/', System.IO.Path.DirectorySeparatorChar);
			}
			else if (aScan.Skip(VOLUME_PATTERN, out var volumePart))
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
		public string SignChar { get; private set; } = "";


		/// <summary>
		/// UNCの場合true
		/// </summary>
		/// <value></value>
		public bool IsUnc { get; private set; }


		/// <summary>
		/// サーバ名。UNCの場合のみ存在。「LOCALHOST」など。
		/// </summary>
		/// <value></value>
		public string? Server { get; private set; }


		/// <summary>
		/// 共有名。UNCの場合のみ存在。「C$」「shared」など。
		/// </summary>
		/// <value></value>
		public string? Share { get; private set; }


		/// <summary>
		/// UNCの場合は Server\Share のような文字列。
		/// そうでない場合はボリュームまたはドライブ(「C:」など)
		/// </summary>
		/// <value></value>
		public string Volume { get; private set; } = "";


		override public string ToString()
			=> IsUnc ? $@"\\{SignChar}\UNC\{Volume}" : $@"\\{SignChar}\{Volume}";
	}


	public class Unc : IPathPrefix
	{
		/// <summary>
		/// matches "//server/share-name"
		/// </summary>
		public static readonly Regex PREFIX_PATTERN = new Regex(@"^//([^/]+)/+([^/]+)");


		public static bool TryParse(FilepathScanner aScan, out Unc? oResult)
		{
			oResult = null;

			if (! aScan.Skip(PREFIX_PATTERN, out var match))
			{
				return false;
			}

			oResult = new Unc();
			oResult.Server = match.Groups[1].Value;
			oResult.Share = match.Groups[2].Value;
			return true;
		}


		/// <summary>
		/// サーバ名。UNCの場合のみ存在。「LOCALHOST」など。
		/// </summary>
		/// <value></value>
		public string? Server { get; private set; }


		/// <summary>
		/// 共有名。UNCの場合のみ存在。「C$」「shared」など。
		/// </summary>
		/// <value></value>
		public string? Share { get; private set; }


		override public string ToString()
			=> $@"\\{Server}\{Share}";
	}



	/// <summary>
	/// Traditional DOS
	/// </summary>
	public class Dos : IPathPrefix
	{
		/// <summary>
		/// matches "c:", "G:" etc...
		/// </summary>
		/// <returns></returns>
		public static readonly Regex PREFIX_PATTERN = new Regex(@"^([a-zA-Z])" + Filepath.MSDOS_VOLUME_SEPARATOR_CHAR);


		public static bool TryParse(FilepathScanner aScan, out Dos? oResult)
		{
			oResult = null;

			if (! aScan.Skip(PREFIX_PATTERN, out var match))
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
		public string Drive { get; private set; } = "";


		/// <summary>
		/// drive letter
		/// </summary>
		/// <value></value>
		public string DriveLetter { get; private set; } = "";


		/// <summary>
		/// drive (or volume) + volume separator
		/// </summary>
		/// <returns></returns>
		override public string ToString() => Drive;
	}
}
