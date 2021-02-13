using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tkuri2010.Fsuty.Tests
{
	[TestClass]
	public class FilepathTest
	{
		[TestMethod]
		public void Test_Empty()
		{
			var path = Filepath.Parse("");
			Assert.IsTrue(path.Prefix is PathPrefix.None);
			Assert.AreEqual("", path.ToString());
		}


		[TestMethod]
		public void Test_Simple_0()
		{
			var path = Filepath.Parse("/");
			Assert.IsTrue(path.IsAbsolute);
			Assert.AreEqual("/", path.ToString("/"));
			Assert.AreEqual("\\", path.ToString("\\"));
		}


		[TestMethod]
		public void Test_Simple_Afile()
		{
			var path = Filepath.Parse("file.txt");
			Assert.IsFalse(path.IsAbsolute);
			Assert.AreEqual("file.txt", path.ToString("/"));
		}


		[TestMethod]
		public void Test_Simple_1()
		{
			var path = Filepath.Parse("a/b/c.tar.gz");
			Assert.IsTrue(path.Prefix is PathPrefix.None);
			Assert.IsFalse(path.IsAbsolute);
			Assert.AreEqual(3, path.Items.Count);
			Assert.AreEqual("a", path.Items[0]);
			Assert.AreEqual("b", path.Items[1]);
			Assert.AreEqual("c.tar.gz", path.Items[2]);
			Assert.AreEqual("a/b/c.tar.gz", path.ToString("/"));
		}


		[TestMethod]
		public void Test_Simple_Absolute_1()
		{
			var path = Filepath.Parse("/");
			Assert.IsTrue(path.Prefix is PathPrefix.None);
			Assert.IsTrue(path.IsAbsolute);
			Assert.AreEqual(0, path.Items.Count);
			Assert.AreEqual("/", path.ToString("/"));
		}


		[TestMethod]
		public void Test_Simple_Absolute_2()
		{
			Action<string> test_ = pathStr =>
			{
				var path = Filepath.Parse(pathStr);
				Assert.IsTrue(path.Prefix is PathPrefix.None);
				Assert.IsTrue(path.IsAbsolute);
				Assert.AreEqual(3, path.Items.Count);
				Assert.AreEqual("/usr/local/bin", path.ToString("/"));
			};

			test_("/usr/local/bin");
			test_("/usr/local/bin/"); // ends with "/"
		}


		[TestMethod]
		public void Test_LastItem()
		{
			{
				var path = Filepath.Parse("");
				Assert.AreEqual(string.Empty, path.LastItem);
			}

			{
				var path = Filepath.Parse("file.txt");
				Assert.AreEqual("file.txt", path.LastItem);
			}

			{
				var path = Filepath.Parse("dir/file.txt");
				Assert.AreEqual("file.txt", path.LastItem);
			}

			{
				var path = Filepath.Parse("dir1/dir2/file.txt");
				Assert.AreEqual("file.txt", path.LastItem);
			}
		}


		[TestMethod]
		public void Test_Extension()
		{
			{
				var path = Filepath.Parse("a/b/c");
				Assert.IsFalse(path.HasExtension);
				Assert.AreEqual("", path.Extension);
			}

			{
				var path = Filepath.Parse("a/b/c.tar.gz");
				Assert.IsTrue(path.HasExtension);
				Assert.AreEqual(".gz", path.Extension);
			}

			{
				var path = Filepath.Parse("a/b/.git");
				Assert.IsTrue(path.HasExtension);
				Assert.AreEqual(".git", path.Extension);
			}

			{
				var path = Filepath.Parse("a/b/.");
				Assert.IsFalse(path.HasExtension);
				Assert.AreEqual("", path.Extension);
			}

			{
				var path = Filepath.Parse("a/b/..");
				Assert.IsFalse(path.HasExtension);
				Assert.AreEqual("", path.Extension);
			}

			{
				var path = Filepath.Parse("a/b/...");
				Assert.IsFalse(path.HasExtension);
				Assert.AreEqual("", path.Extension);
			}
		}


		[TestMethod]
		public void Test_LastItemWithoutExtension()
		{
			{
				var path = Filepath.Parse("");
				Assert.AreEqual(string.Empty, path.LastItemWithoutExtension);
			}

			{
				var path = Filepath.Parse("/");
				Assert.AreEqual(string.Empty, path.LastItemWithoutExtension);
			}

			{
				var path = Filepath.Parse(".svn");
				Assert.AreEqual(string.Empty, path.LastItemWithoutExtension);
			}

			{
				var path = Filepath.Parse("/dir/.tar.gz");
				Assert.AreEqual(".tar", path.LastItemWithoutExtension);
			}

			{
				var path = Filepath.Parse("file");
				Assert.AreEqual("file", path.LastItemWithoutExtension);
			}

			{
				var path = Filepath.Parse("file.txt");
				Assert.AreEqual("file", path.LastItemWithoutExtension);
			}

			{
				var path = Filepath.Parse("/usr/bin/file.txt");
				Assert.AreEqual("file", path.LastItemWithoutExtension);
			}

			{
				var path = Filepath.Parse("/dir/.git");
				Assert.AreEqual(string.Empty, path.LastItemWithoutExtension);
			}

			if (PathLogics.SeemsWin32FileSystem)
			{
				var path = Filepath.Parse("c:/dir/.git");
				Assert.AreEqual(string.Empty, path.LastItemWithoutExtension);
			}
		}

#if false  // 文字列を指定できる Combine() は一旦廃止
		[TestMethod]
		public void Test_Combine()
		{
			var basepath = Filepath.Parse(@"c:\dir1/dir2");

			{
				var path = basepath.Combine("dir3");

				Assert.AreEqual(@"c:\dir1\dir2\dir3", path.ToString("\\"));

				#region detail

				// 以下、詳細にチェック
				var prefix = basepath.Prefix as Dos;
				Assert.AreEqual("c", prefix!.DriveLetter);

				Assert.AreEqual(3, path.Items.Count);
				Assert.AreEqual("dir1", path.Items[0]);
				Assert.AreEqual("dir2", path.Items[1]);
				Assert.AreEqual("dir3", path.Items[2]);

				#endregion
			}

			{
				var path = basepath.Combine("dir3/file.txt");
				Assert.AreEqual(4, path.Items.Count);
				Assert.AreEqual("file.txt", path.Items[3]);
			}

			{
				var path = basepath.Combine(@"d:\");
				Assert.AreEqual("d", (path.Prefix as Dos)!.DriveLetter);
				Assert.AreEqual(0, path.Items.Count);
			}

			{
				var path = basepath.Combine(@"d:\dirX\dirY");

				Assert.AreEqual(@"d:\dirX\dirY", path.ToString("\\"));

				#region detail
				Assert.AreEqual("d", (path.Prefix as Dos)!.DriveLetter);
				Assert.AreEqual(2, path.Items.Count);
				Assert.AreEqual("dirX", path.Items[0]);
				Assert.AreEqual("dirY", path.Items[1]);
				#endregion
			}

			{ // ちょっと珍しい形式 1
				var path = basepath.Combine(@"d:");

				Assert.AreEqual(@"d:\dir1\dir2", path.ToString("\\"));

				#region detail
				Assert.AreEqual("d", (path.Prefix as Dos)!.DriveLetter);
				Assert.AreEqual(2, path.Items.Count);
				Assert.AreEqual("dir1", path.Items[0]);
				Assert.AreEqual("dir2", path.Items[1]);
				#endregion
			}

			{ // ちょっと珍しい形式 2
				var path = basepath.Combine(@"d:dir3\dir4");

				Assert.AreEqual(@"d:\dir1\dir2\dir3\dir4", path.ToString("\\"));

				#region detail
				Assert.AreEqual("d", (path.Prefix as Dos)!.DriveLetter);
				Assert.AreEqual(4, path.Items.Count);
				Assert.AreEqual("dir3", path.Items[2]);
				Assert.AreEqual("dir4", path.Items[3]);
				#endregion
			}
		}
#endif


		[TestMethod]
		public void Test_Combine_0()
		{
			var rv = Filepath.Empty.Combine(Filepath.Empty.Items);

			Assert.IsInstanceOfType(rv.Prefix, typeof(PathPrefix.None));
			Assert.AreEqual(0, rv.Items.Count);
			Assert.AreEqual("", rv.ToString("/"));
		}


		[TestMethod]
		public void Test_Combine_1()
		{
			var basepath = Filepath.Parse("/home/dir");
			var other = Filepath.Parse("rel/file.txt");

			var rv = basepath.Combine(other.Items);

			Assert.IsInstanceOfType(rv.Prefix, typeof(PathPrefix.None));
			Assert.IsTrue(basepath.IsAbsolute);
			Assert.AreEqual(4, rv.Items.Count);
			Assert.AreEqual("home", rv.Items[0]);
			Assert.AreEqual("file.txt", rv.Items[3]);
		}


		[TestMethod]
		public void Test_Combine_2()
		{
			var basepath = Filepath.Parse("relative/dir");
			var other = Filepath.Parse("/abs/file.txt");

			var rv = basepath.Combine(other.Items);

			Assert.IsInstanceOfType(rv.Prefix, typeof(PathPrefix.None));
			Assert.IsFalse(basepath.IsAbsolute);
			Assert.AreEqual(4, rv.Items.Count);
			Assert.AreEqual("relative", rv.Items[0]);
			Assert.AreEqual("file.txt", rv.Items[3]);
		}


		[TestMethod]
		public void Test_Slice_1()
		{
			var basepath = Filepath.Parse("/home/kuma/foo/bar/baz.txt");

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

			{
				var path = basepath.Slice(0);
				Assert.IsTrue(path.IsAbsolute);
				Assert.AreEqual("/home/kuma/foo/bar/baz.txt", path.ToString("/"));
			}

			{
				var path = basepath.Slice(1);
				Assert.IsFalse(path.IsAbsolute);
				Assert.AreEqual("kuma/foo/bar/baz.txt", path.ToString("/"));
			}

			{
				var path = basepath.Slice(-1);
				Assert.IsFalse(path.IsAbsolute);
				Assert.AreEqual(1, path.Items.Count);
				Assert.AreEqual("baz.txt", path.ToString("/"));
			}

			{
				var path = basepath.Slice(-2);
				Assert.IsFalse(path.IsAbsolute);
				Assert.AreEqual(2, path.Items.Count);
				Assert.AreEqual("bar/baz.txt", path.ToString("/"));
			}

			// 2引数
			{
				var path = basepath.Slice(0, 0);
				Assert.IsTrue(path.IsAbsolute);
				Assert.AreEqual("/", path.ToString("/"));
			}

			{
				var path = basepath.Slice(0, 1);
				Assert.IsTrue(path.IsAbsolute);
				Assert.AreEqual("/home", path.ToString("/"));
			}

			{
				var path = basepath.Slice(0, 2);
				Assert.IsTrue(path.IsAbsolute);
				Assert.AreEqual("/home/kuma", path.ToString("/"));
			}

			{
				var path = basepath.Slice(1, 2);
				Assert.IsFalse(path.IsAbsolute);
				Assert.AreEqual("kuma/foo", path.ToString("/"));
			}

			{
				var path = basepath.Slice(2, 2);
				Assert.IsFalse(path.IsAbsolute);
				Assert.AreEqual("foo/bar", path.ToString("/"));
			}

			{
				var path = basepath.Slice(0, -1);
				Assert.IsTrue(path.IsAbsolute);
				Assert.AreEqual("/home/kuma/foo/bar", path.ToString("/"));
			}

			{
				var path = basepath.Slice(0, -2);
				Assert.IsTrue(path.IsAbsolute);
				Assert.AreEqual("/home/kuma/foo", path.ToString("/"));
			}

			{
				var path = basepath.Slice(1, -2);
				Assert.IsFalse(path.IsAbsolute);
				Assert.AreEqual("kuma/foo", path.ToString("/"));
			}

			{
				var path = basepath.Slice(2, -2);
				Assert.IsFalse(path.IsAbsolute);
				Assert.AreEqual("foo", path.ToString("/"));
			}
		}


		[TestMethod]
		public void Test_Canonicalize()
		{
			{
				var path = Filepath.Parse("./../dir/.../xxx/../file.txt");
				Assert.AreEqual("dir/.../file.txt", path.Canonicalize().ToString("/"));
			}
		}
	}
}
