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
