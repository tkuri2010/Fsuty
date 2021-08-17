#if Windows

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using Tkuri2010.Fsuty.PathPrefix;

namespace Tkuri2010.Fsuty.Tests
{
	[TestClass]
	public class FilepathWin32Test
	{
		[TestMethod]
		public void Test_Simple_WithBackSlashes()
		{
			var path = Filepath.Parse(@"a/\/b///c/\/\/");
			Assert.IsFalse(path.IsAbsolute);
			Assert.AreEqual(3, path.Items.Count);
			Assert.AreEqual("c", path.Items[2]);
		}


		[TestMethod]
		public void Test_TraditionalDos_1()
		{
			{ // relative
				var path = Filepath.Parse(@"c:");
				var prefix = path.Prefix as Dos;
				Assert.IsNotNull(prefix);
				Assert.AreEqual("c:", prefix!.Drive);
				Assert.AreEqual("C", prefix!.DriveLetter.ToUpper());
				Assert.IsFalse(path.IsAbsolute);
				Assert.AreEqual(0, path.Items.Count);
			}

			{ // absolute
				var path = Filepath.Parse(@"z:\");

				var prefix = path.Prefix as Dos;
				Assert.IsNotNull(prefix);
				Assert.AreEqual("z:", prefix!.Drive);
				Assert.AreEqual("Z", prefix!.DriveLetter.ToUpper());
				Assert.IsTrue(path.IsAbsolute);
				Assert.AreEqual(0, path.Items.Count);
			}
		}


		[TestMethod]
		public void Test_TraditionalDos_2()
		{
			Action<string> test_ = pathStr =>
			{
				var path = Filepath.Parse(pathStr);
				Assert.IsTrue(path.IsAbsolute);
				Assert.AreEqual(1, path.Items.Count);
			};

			test_(@"C:\a");
			test_(@"C:\a\");
		}


		/// <summary>
		/// win32 drive letter and RELATIVE path
		/// </summary>
		[TestMethod]
		public void Test_TraditionalDos_3()
		{
			{
				var path = Filepath.Parse(@"c:a\b\");
				Assert.IsFalse(path.IsAbsolute);
				Assert.AreEqual(2, path.Items.Count);
			}

			{
				var path = Filepath.Parse(@"c:a\b.txt");
				Assert.IsFalse(path.IsAbsolute);
				Assert.AreEqual(2, path.Items.Count);
			}
		}


		[TestMethod]
		public void Test_DosDevice_1()
		{
			Action<string> test_ = pathStr =>
			{
				var path = Filepath.Parse(pathStr);

				var prefix = path.Prefix as DosDevice;
				Assert.IsNotNull(prefix);
				Assert.AreEqual("foo", prefix!.Volume);
				Assert.AreEqual(2, path.Items.Count);
				Assert.AreEqual("bar", path.Items[0]);
				Assert.AreEqual("baz", path.Items[1]);
			};

			test_(@"\\.\foo\bar\baz");
			test_(@"\\?\foo\bar\baz");
			test_(@"//./foo/bar/baz");
			test_(@"//?/foo/bar/baz");
		}


		[TestMethod]
		public void Test_DosDevice_2_Drive()
		{
			Action<string> test_ = pathStr =>
			{
				var path = Filepath.Parse(pathStr);

				var prefix = path.Prefix as DosDeviceDrive;
				Assert.IsNotNull(prefix);
				Assert.AreEqual("C", prefix!.DriveLetter);
				Assert.AreEqual("C:", prefix!.Drive);
				Assert.AreEqual("C:", prefix!.Volume);
				Assert.AreEqual(2, path.Items.Count);
				Assert.AreEqual("bar", path.Items[0]);
				Assert.AreEqual("baz", path.Items[1]);
			};

			test_(@"\\.\C:\bar\baz");
			test_(@"\\?\C:\bar\baz");
			test_(@"//./C:/bar/baz");
			test_(@"//?/C:/bar/baz");
		}


		[TestMethod]
		public void Test_DosDevice_2_UNC()
		{
			Action<string> test_ = pathStr =>
			{
				var path = Filepath.Parse(pathStr);

				var prefix = path.Prefix as DosDeviceUnc;
				Assert.IsNotNull(prefix);
				Assert.AreEqual("foo", prefix!.Server);
				Assert.AreEqual("bar", prefix!.Share);
				Assert.AreEqual(@"foo\bar", prefix.Volume);
				Assert.AreEqual(1, path.Items.Count);
				Assert.AreEqual("baz", path.Items[0]);
			};

			test_(@"\\.\UNC\foo\bar\baz");
			test_(@"\\?\UNC\foo\bar\baz");
		}


		[TestMethod]
		public void Test_UNC_1()
		{
			{
				var path = Filepath.Parse(@"\\system7\C$");

				var prefix = path.Prefix as Unc;
				Assert.IsNotNull(prefix);
				Assert.AreEqual("system7", prefix!.Server);
				Assert.AreEqual("C$", prefix!.Share);
				Assert.AreEqual(0, path.Items.Count);
			}

			{
				var path = Filepath.Parse(@"\\localhost\share-name\dir\file.txt");
				Assert.AreEqual(2, path.Items.Count);
				Assert.AreEqual("file.txt", path.Items[1]);
			}
		}


		[TestMethod]
		public void Test_Combine()
		{
			var basepath = Filepath.Parse(@"d:\dir1/dir2");

			{
				var subpath = Filepath.Empty;

				var path = basepath.Combine(subpath.Items);

				Assert.AreEqual("d", (path.Prefix as Dos)!.DriveLetter);
				Assert.AreEqual(2, path.Items.Count);
				Assert.AreEqual("dir2", path.LastItem);
			}

			{
				var subpath = Filepath.Parse("//server/share-name");
				Assert.IsTrue(subpath.Prefix is PathPrefix.Unc);

				var path = basepath.Combine(subpath.Items);

				Assert.AreEqual("d", (path.Prefix as Dos)!.DriveLetter);
				Assert.AreEqual(2, path.Items.Count);
				Assert.AreEqual("dir2", path.LastItem);
			}

			{
				var subpath = Filepath.Parse("dir3/dir4");

				var path = basepath.Combine(subpath.Items);

				Assert.AreEqual("d", (path.Prefix as Dos)!.DriveLetter);
				Assert.AreEqual(4, path.Items.Count);
				Assert.AreEqual("dir3", path.Items[2]);
				Assert.AreEqual("dir4", path.Items[3]);
			}
		}


		[TestMethod]
		public void Test_Slice_2_WithPrefix()
		{
			var basepath = Filepath.Parse(@"C:\home\kuma\foo\bar\baz.txt");

			// 色々なパラメータを入力しても
			// へんな例外が発生しなければOK
			{
				for (var c = -20; c < 20; c++)
				{
					for (var s = -20; s < 20; s++)
					{
						var path = basepath.Slice(s, c);
						Assert.IsTrue(true);
					}
				}
			}

			{ // Slice(0) ... 実質、何も変わらないはず
				var path = basepath.Slice(0);
				Assert.IsInstanceOfType(path.Prefix, typeof(PathPrefix.Dos));
				Assert.AreEqual((path.Prefix as PathPrefix.Dos)!.DriveLetter.ToLower(), "c");

				Assert.IsTrue(path.IsAbsolute);
				Assert.AreEqual("home", path.Items[0]);
				Assert.AreEqual("baz.txt", path.Items[4]);
			}

			{ // Slice(1) ... 相対パスを表すようになるはず
				var path = basepath.Slice(1);
				Assert.IsInstanceOfType(path.Prefix, typeof(PathPrefix.Dos));
				Assert.AreEqual((path.Prefix as PathPrefix.Dos)!.DriveLetter.ToLower(), "c");

				Assert.IsFalse(path.IsAbsolute);
				Assert.AreEqual("kuma", path.Items[0]);
				Assert.AreEqual("baz.txt", path.Items[3]);
			}
		}


		[TestMethod]
		public void Test_PrefixOfDosDevice_Regex()
		{
			{
				var src1 = FilepathScanner._Prepare(@"\\.\foo");
				var m1 = DosDevice.PrefixPattern.Match(src1);
				Assert.IsTrue(m1.Success);
				Assert.AreEqual(@".", m1.Groups[1].Value);
				Assert.AreEqual(@"/foo", src1.Substring(m1.Length));
			}

			{
				var src2 = FilepathScanner._Prepare(@"\\?\foo");
				var m2 = DosDevice.PrefixPattern.Match(src2);
				Assert.IsTrue(m2.Success);
				Assert.AreEqual(@"?", m2.Groups[1].Value);
				Assert.AreEqual(@"/foo", src2.Substring(m2.Length));
			}
		}


		[TestMethod]
		public void Test_PrefixDosDevice_1()
		{
			var src = @"\\.\";
			var scan = new FilepathScanner(src);

			Assert.IsTrue(DosDevice.TryParse(scan, out var prefix));
		}


		[TestMethod]
		public void Test_PrefixDosDevice_Drive_1()
		{
			Action<string> test_ = srcStr =>
			{
				var scan = new FilepathScanner(srcStr);
				Assert.IsTrue(DosDeviceDrive.TryParse(scan, out var prefix));
			};

			test_(@"\\.\C:");
			test_(@"\\.\C:");
			test_(@"//?/C:");
			test_(@"//?/C:");
		}


		[TestMethod]
		public void Test_PrefixDosDevice_UNC1()
		{
			var src = @"\\.\UNC";
			var scan = new FilepathScanner(src);

			Assert.IsTrue(DosDeviceUnc.TryParse(scan, out var prefix));
		}


		[TestMethod]
		public void Test_PrefixDosDevice_UNC2()
		{
			var src = @"\\.\UNC\127.0.0.1";
			var scan = new FilepathScanner(src);

			Assert.IsTrue(DosDeviceUnc.TryParse(scan, out var prefix));
			Assert.AreEqual(@"127.0.0.1", prefix!.Server);
			Assert.IsTrue(string.IsNullOrEmpty(prefix!.Share));
			Assert.AreEqual(@"127.0.0.1", prefix!.Volume);
		}


		[TestMethod]
		public void Test_PrefixDosDevice_UNC3()
		{
			var src = @"\\?\UNC\127.0.0.1\share-name";
			var scan = new FilepathScanner(src);

			Assert.IsTrue(DosDeviceUnc.TryParse(scan, out var prefix));
			Assert.AreEqual(@"127.0.0.1", prefix!.Server);
			Assert.AreEqual(@"share-name", prefix!.Share);
			Assert.AreEqual(@"127.0.0.1\share-name", prefix!.Volume);
		}


		[TestMethod]
		public void Test_PrefixDosDevice_Normal1()
		{
			var src = @"\\.\C:\dir\file.txt";
			var scan = new FilepathScanner(src);

			Assert.IsTrue(DosDeviceDrive.TryParse(scan, out var prefix));
			Assert.AreEqual(@"C:", prefix!.Volume);
		}


		[TestMethod]
		public void Test_PrefixDosDevice_Normal2()
		{
			var src = @"\\.\Volume{xxx-xxx-xxx}\dir\file.txt";
			var scan = new FilepathScanner(src);

			Assert.IsTrue(DosDevice.TryParse(scan, out var prefix));
			Assert.AreEqual(@"Volume{xxx-xxx-xxx}", prefix!.Volume);
		}


		[TestMethod]
		public void Test_PrefixJustUnc_1()
		{
			var src = @"\\server\C$";
			var scan = new FilepathScanner(src);

			Assert.IsTrue(Unc.TryParse(scan, out var prefix));
			Assert.AreEqual("server", prefix!.Server);
			Assert.AreEqual("C$", prefix!.Share);
		}


		[TestMethod]
		public void Test_PrefixJustUnc_2()
		{
			var src = @"\\server\share-name\dir\file.txt";
			var scan = new FilepathScanner(src);

			Assert.IsTrue(Unc.TryParse(scan, out var prefix));
			Assert.AreEqual("server", prefix!.Server);
			Assert.AreEqual("share-name", prefix!.Share);
		}


		[TestMethod]
		public void Test_PrefixTraditionalDos()
		{
			var src = @"c:\";
			var scan = new FilepathScanner(src);

			Assert.IsTrue(Dos.TryParse(scan, out var prefix));
			Assert.AreEqual("c:", prefix!.Drive);
			Assert.AreEqual("c", prefix!.DriveLetter);
		}
	}
}

#endif // Windows
