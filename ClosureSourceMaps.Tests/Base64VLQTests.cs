using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClosureSourceMaps.Tests
{
	using System.Text;

	[TestClass]
	public class Base64VLQTests
	{
		[TestMethod]
		public void Test0To63()
		{
			for (int i = 0; i < 63; i++) {
				TestEncodeDecode(i);
			}
		}

		[TestMethod]
		public void TestPower2EvenAndOdd()
		{
			int @base = 1;
			for (int i = 0; i < 30; i++) {
				TestEncodeDecode(@base - 1);
				TestEncodeDecode(@base);
				@base *= 2;
			}
		}

		[TestMethod]
		public void AroundZeroUpTo64Sq()
		{
			for (int i = -(64 * 64 - 1); i < (64 * 64 - 1); i++) {
				TestEncodeDecode(i);
			}
		}

		[TestMethod]
		public void SignedPower2EvenOdd()
		{
			int @base = 1;
			for (int i = 0; i < 30; i++) {
				TestEncodeDecode(@base - 1);
				TestEncodeDecode(@base);
				@base *= 2;
			}
			@base = -1;
			for (int i = 0; i < 30; i++) {
				TestEncodeDecode(@base - 1);
				TestEncodeDecode(@base);
				@base *= 2;
			}
		}

		private void TestEncodeDecode(int value)
		{
			try {
				var sb = new StringBuilder();
				Base64Vlq.Encode(sb, value);
				int result = Base64Vlq.Decode(sb.ToString());
				Assert.AreEqual(value, result);
			} catch (Exception e) {
				throw new Exception("failed for value " + value, e);
			}
		}
	}
}
