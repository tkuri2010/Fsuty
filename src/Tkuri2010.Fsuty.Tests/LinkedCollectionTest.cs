using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace Tkuri2010.Fsuty.Tests
{
	[TestClass]
	public class LinkedCollectionTest
	{
		[TestMethod]
		public void Test_EmptyInstance_ManyMethods()
		{
			var empty = new LinkedCollection<string>();

			Assert.AreEqual(0, empty.Count);

			foreach (var e in empty)
			{
				Assert.Fail("The empty must has no items.");
			}

			var empty2 = empty.Slice(0, 0);
			var empty3 = empty.SkipItems(0);
			var empty4 = empty.TakeItems(0);
			var them = new[]{ empty2, empty3, empty4 };
			foreach (var it in them)
			{
				Assert.AreEqual(0, it.Count);
				foreach (var e in it) Assert.Fail("empty!!");
			}
		}


		[TestMethod]
		public void Test_Empty_Append1()
		{
			var empty = new LinkedCollection<string>();

			var has1 = empty.AppendElement("e");
			Assert.AreEqual(1, has1.Count);
			Assert.AreEqual("e", has1[0]);
			var count = 0;
			foreach (var e in has1)
			{
				Assert.AreEqual("e", e);
				count++;
			}
			Assert.AreEqual(1, count);
		}


		[TestMethod]
		public void Test_Empty_AppendMany()
		{
			var empty = new LinkedCollection<string>();

			var hasMany = empty.AppendRange(Tens);

			Assert.AreEqual(10, hasMany.Count);

			for (var i = 0; i < hasMany.Count; i++)
			{
				Assert.AreEqual($"{i}", hasMany[i]);
			}

			var i2 = 0;
			foreach (var e in hasMany)
			{
				Assert.AreEqual($"{i2}", e);
				i2++;
			}
		}


		static readonly string[] Tens = new[]{ "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };

		[TestMethod]
		public void Test_Slice_1()
		{
			var c = new LinkedCollection<string>(Tens);

			var slice = c.Slice(0, 10);

			Assert.AreEqual(10, c.Count);
		}


		[TestMethod]
		public void Test_Slice_2()
		{
			var c = new LinkedCollection<string>(Tens);

			var slice = c.Slice(1, 9);

			Assert.AreEqual(9, slice.Count);
			Assert.AreEqual("1", slice[0]);
			Assert.AreEqual("9", slice[8]);
		}


		[TestMethod]
		public void Test_Slice_3()
		{
			var c = new LinkedCollection<string>(Tens);

			var slice = c.Slice(6, 2);

			Assert.AreEqual(2, slice.Count);
			Assert.AreEqual("6", slice[0]);
			Assert.AreEqual("7", slice[1]);
		}


		[TestMethod]
		public void Test_Slice_4()
		{
			var c = new LinkedCollection<string>(Tens);

			var slice = c.Slice(-1, 7);  // -1 is assumed 0

			Assert.AreEqual(7, slice.Count);
			Assert.AreEqual("0", slice[0]);
			Assert.AreEqual("6", slice[6]);
		}


		[TestMethod]
		public void Test_Slice_5()
		{
			var c = new LinkedCollection<string>(Tens);

			var slice = c.Slice(1, 100);

			Assert.AreEqual(9, slice.Count);
			Assert.AreEqual("1", slice[0]);
			Assert.AreEqual("9", slice[8]);
		}


		/// <summary>
		/// LinkedCollection は immutable なので、Append などの操作で元オブジェクトが変化しない事を確認
		/// </summary>
		[TestMethod]
		public void Test_Append_1()
		{
			var o0 = new LinkedCollection<string>(new []{"0"});

			var o1 = o0.AppendElement("1");
			Action testo1 = () =>
			{
				Assert.AreEqual(2, o1.Count);
				Assert.AreEqual("1", o1[1]);
			};
			testo1();

			var o1_2 = o0.AppendElement("another1");
			Action testo1_2 = () =>
			{
				Assert.AreEqual(2, o1_2.Count);
				Assert.AreEqual("another1", o1_2[1]);
			};
			testo1();
			testo1_2();

			var o2 = o1.AppendElement("2");
			Action testo2 = () =>
			{
				Assert.AreEqual(3, o2.Count);
				Assert.AreEqual("2", o2[2]);
			};
			testo1();
			testo1_2();
			testo2();
		}


		[TestMethod]
		public void Test_ReverseEnumerator_Empty()
		{
			var o = new LinkedCollection<string>();

			var count = 0;
			var rev = o.GetReverseEnumerator();
			rev.Reset();
			while (rev.MoveNext())
			{
				_ = rev.Current;
				count++;
			}

			Assert.AreEqual(0, count);
		}


		[TestMethod]
		public void Test_ReverseEnumerator_1()
		{
			var o = new LinkedCollection<string>(new[]{"5", "4", "3", "2", "1", "0"});

			var count = 0;
			var rev = o.GetReverseEnumerator();
			rev.Reset();
			while (rev.MoveNext())
			{
				Assert.AreEqual($"{count}", rev.Current);
				count++;
			}

			Assert.AreEqual(6, count);
		}
	}
}
