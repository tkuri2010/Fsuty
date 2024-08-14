namespace Tkuri2010.Fsuty.Xmp
{
	public class Win32Labo
	{

#if false
		[DllImport("shlwapi.dll", CharSet = CharSet.Auto, SetLastError = true)]
		private static extern bool PathCanonicalize(
				[Out] StringBuilder lpszDest,
				string lpszSrc);

		static void Try_PathCanonicalize(string input)
		{
			var buf = new StringBuilder(256);
			PathCanonicalize(buf, input);
			Console.WriteLine($"|`{input}` | `{buf}` |");
		}

		static void Try_PathCanonicalize()
		{
			Try_PathCanonicalize( @"" );       //=> @"\"
			Try_PathCanonicalize( @"." );      //=> @"\"
			Try_PathCanonicalize( @".." );     //=> @"\"
			Try_PathCanonicalize( @".\" );     //=> @"\"
			Try_PathCanonicalize( @"c:." );    //=> @"c:\"

			Try_PathCanonicalize( @"a.." );    //=> @"a"
			Try_PathCanonicalize( @"..a" );    //=> @"..a"
			Try_PathCanonicalize( @"a..b" );   //=> @"a..b"
			Try_PathCanonicalize( @"./" );     //=> @"./"
			Try_PathCanonicalize( @"./.\." );  //=> @"./"
			Try_PathCanonicalize( @".a.\." );  //=> @".a"
			Try_PathCanonicalize( @".\./." );  //=> @"./"

			Try_PathCanonicalize( @".\..\dir\...\xxx\..\file.txt"); //=> @"dir\...\file.txt"
		}
#endif


#if false
		static void Try_Path_Combine(string path1, string path2)
		{
			var rv = System.IO.Path.Combine(path1, path2);
			Console.WriteLine($"| `{path1}` | `{path2}` | `{rv}` |");
		}

		static void Try_Path_Combine()
		{
			Try_Path_Combine(@"c:/base", @"dir"); // 普通に期待通り、ディレクトリのセパレータを補って連結
			Try_Path_Combine(@"c:/base/", @"dir"); // こちらも期待通り、ディレクトリのセパレータは補わずに連結
			Try_Path_Combine(@"c:\base", @"."); // ディレクトリセパレータは補うが、単純に文字列を連結するだけ
			Try_Path_Combine(@"c:\base", @"..\..\dir1");  // 同じく

			// 以下は path1 が完全になくなり、戻り値としては単純に path2 がそのまま返る
			Try_Path_Combine(@"c:\base", @"/."); // => /.
			Try_Path_Combine(@"c:\base", @"\."); // => \.
			Try_Path_Combine(@"c:\base", @"/dir1");
			Try_Path_Combine(@"c:\base", @"d:dir1");
			Try_Path_Combine(@"c:\base", @"\\?\server\share-name\dir1");
		}
#endif
	}
}