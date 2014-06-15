using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ClosureSourceMaps.Tests
{
	[TestClass]
	public class Base64Tests
	{
		[TestMethod]
		public void IdentityTransform()
		{
			for (int i = 0; i < 64; i++) {
				var encoded = Base64.ToBase64(i);
				var decoded = Base64.FromBase64(encoded);
				Assert.AreEqual(i, decoded);
			}
		}

		[TestMethod]
		public void EncodeInt()
		{
			Assert.AreEqual("AAAAAA", Base64.Base64EncodeInt(0));
			Assert.AreEqual("AAAAAQ", Base64.Base64EncodeInt(1));
			Assert.AreEqual("AAAAKg", Base64.Base64EncodeInt(42));
			Assert.AreEqual("////nA", Base64.Base64EncodeInt(-100));
			Assert.AreEqual("/////w", Base64.Base64EncodeInt(-1));
		}
	}
}
