using System.Text;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Tkuri2010.Fsuty.Text.Std
{
	[TestClass]
	public class LargeFileLineProcessorTest
	{
		/// <summary>
		/// 文字列を行に分解した時、期待した行数になる事をテスト
		/// </summary>
		[TestMethod]
		public void Test_LineEnumerator()
		{
			//Assert.AreEqual(2, Repeat("a", 2).Length);
			//Assert.AreEqual(133, Repeat("a", 133).Length);
			//Assert.AreEqual(266, Repeat("ab", 133).Length);
			//Assert.AreEqual(399, Repeat("abc", 133).Length);

			var str65k = Repeat("a", 65 * 1024);

			var dataArray = new (string, int)[]
			{
				// 文字列と、行数の期待値
				("", 0),
				("a", 1),
				("\n", 1),
				("a\n", 1),
				("\na", 2),
				("\n\n", 2),
				("a\na", 2),
				("a\na\n", 2),
				(Repeat(str65k + "\n", 6), 6),
			};

			foreach (var testData in dataArray)
			{
				_TestLineEnumerator(testData.Item1, testData.Item2);
			}
		}


		void _TestLineEnumerator(string lines, int expectedLinesCount)
		{
			var bytes = System.Text.Encoding.UTF8.GetBytes(lines);
			var readable = new Lfdetail.SimpleReadableBytes(bytes);
			var enumerator = new Lfdetail.BasicLineEnumerator();

			int count = 0;
			foreach (var buf in enumerator.Enumerate(readable))
			{
				count++;
			}

			Assert.AreEqual(expectedLinesCount, count);
		}


		/// <summary>
		/// 文字列を行に分解した時、それぞれの行のサイズをテスト
		/// </summary>
		[TestMethod]
		public void Test_LineEnumerator2()
		{
			var str = Repeat("a", 65 * 1024) + "\n"
					+ Repeat("a", 66 * 1024) + "\n"
					+ Repeat("a", 67 * 1024) + "\n"
					;
			var bytes = System.Text.Encoding.UTF8.GetBytes(str);
			var readable = new Lfdetail.SimpleReadableBytes(bytes);
			var enumerator = new Lfdetail.BasicLineEnumerator();

			var expectedSizes = new int[] {
					65 * 1024 + 1,
					66 * 1024 + 1,
					67 * 1024 + 1,
			};
			var i = 0;
			foreach (var buf in enumerator.Enumerate(readable))
			{
				Assert.AreEqual(expectedSizes[i++], buf.Count);
			}
		}


		static string Repeat(string str, int count)
		{
			if (count == 0) return string.Empty;
			if (count == 1) return str;

			var buf = new StringBuilder(str);
			int repeated = 1;

			while (repeated * 2 < count)
			{
				buf.Append(buf);
				repeated *= 2;
			}

			while (repeated < count)
			{
				buf.Append(str);
				repeated++;
			}

			return buf.ToString();
		}

	}
}