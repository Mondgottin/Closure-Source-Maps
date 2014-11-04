/* translated from test-source-map-generator.js
*
* Copyright 2011 Mozilla Foundation and contributors
* Licensed under the New BSD license. See LICENSE or:
* http://opensource.org/licenses/BSD-3-Clause
*/

using Microsoft.VisualStudio.TestTools.UnitTesting;
namespace ClosureSourceMaps.Tests
{
	using System.Linq;
	using Microsoft.VisualStudio.TestTools.UnitTesting;
	using Newtonsoft.Json.Linq;

	[TestClass]
	public class GeneratorTests
	{
		[TestMethod]
		public void CorrectVersion()
		{
			JObject mapJson = GenerateTestMap();
			Assert.AreEqual(3, mapJson.Value<int>("version"));
		}

		[TestMethod]
		public void CorrectFile()
		{
			JObject mapJson = GenerateTestMap();
			Assert.AreEqual("min.js", mapJson.Value<string>("file"));
		}

		[TestMethod]
		public void CorrectNames()
		{
			JObject mapJson = GenerateTestMap();
            var actual = mapJson["names"].Values<string>().ToArray();
			CollectionAssert.AreEquivalent(new[] { "bar", "baz", "n" }, actual);
		}

		[TestMethod]
		public void CorrectSources()
		{
			JObject mapJson = GenerateTestMap();
			var actual = mapJson["sources"].Values<string>().ToArray();
			CollectionAssert.AreEquivalent(new[] { "one.js", "two.js" }, actual);
		}

		[TestMethod]
		public void CorrectSourceRoot()
		{
            Assert.Inconclusive();
//			JObject mapJson = GenerateTestMap();
///			Assert.AreEqual("/the/root", mapJson["sourceRoot"].Value<string>());
		}

		[TestMethod]
		public void CorrectMappings()
		{
			JObject mapJson = GenerateTestMap();
            var result = mapJson["mappings"].Value<string>();
			Assert.AreEqual(
				"CAAC,IAAI,IAAM,SAAUA,GAClB,OAAOC,IAAID;CCDb,IAAI,IAAM,SAAUE,GAClB,OAAOA",
				result
			);
		}

		[TestMethod]
		public void CorrectMapping()
		{
			Assert.Inconclusive();
			//map = JSON.parse(mapJson);
			//util.assertEqualMaps(assert, map, util.testMap);
		}

		private static JObject GenerateTestMap()
		{
			var map = SourceMapGeneratorFactory.GetInstance();
			map.AddMapping("one.js",
				outputStartPosition: new FilePosition(1, 1),
                outputEndPosition: new FilePosition(1, 5),
				sourceStartPosition: new FilePosition(1, 1)
			);
			map.AddMapping("one.js",
				outputStartPosition: new FilePosition(1, 5),
                outputEndPosition: new FilePosition(1, 9),
				sourceStartPosition: new FilePosition(1, 5)
			);
			map.AddMapping("one.js",
				outputStartPosition: new FilePosition(1, 9),
                outputEndPosition: new FilePosition(1, 18),
				sourceStartPosition: new FilePosition(1, 11)
			);
			map.AddMapping("one.js", symbolName: "bar",
				outputStartPosition: new FilePosition(1, 18),
                outputEndPosition: new FilePosition(1, 21),
				sourceStartPosition: new FilePosition(1, 21)
			);
			map.AddMapping(
				outputStartPosition: new FilePosition(1, 21),
                outputEndPosition: new FilePosition(1, 28),
				sourceStartPosition: new FilePosition(2, 3),
				sourceName: "one.js"
			);
			map.AddMapping(
				outputStartPosition: new FilePosition(1, 28),
                outputEndPosition: new FilePosition(1, 32),
				sourceStartPosition: new FilePosition(2, 10),
				sourceName: "one.js",
				symbolName: "baz"
			);
			map.AddMapping(
				outputStartPosition: new FilePosition(1, 32),
                outputEndPosition: new FilePosition(2, 1),
				sourceStartPosition: new FilePosition(2, 14),
				sourceName: "one.js",
				symbolName: "bar"
			);
			map.AddMapping(
				outputStartPosition: new FilePosition(2, 1),
                outputEndPosition: new FilePosition(2, 5),
				sourceStartPosition: new FilePosition(1, 1),
				sourceName: "two.js"
			);
			map.AddMapping(
				outputStartPosition: new FilePosition(2, 5),
                outputEndPosition: new FilePosition(2, 9),
				sourceStartPosition: new FilePosition(1, 5),
				sourceName: "two.js"
			);
			map.AddMapping(
				outputStartPosition: new FilePosition(2, 9),
                outputEndPosition: new FilePosition(2, 18),
				sourceStartPosition: new FilePosition(1, 11),
				sourceName: "two.js"
			);
			map.AddMapping(
				outputStartPosition: new FilePosition(2, 18),
                outputEndPosition: new FilePosition(2, 21),
				sourceStartPosition: new FilePosition(1, 21),
				sourceName: "two.js",
				symbolName: "n"
			);
			map.AddMapping(
				outputStartPosition: new FilePosition(2, 21),
                outputEndPosition: new FilePosition(2, 28),
				sourceStartPosition: new FilePosition(2, 3),
				sourceName: "two.js"
			);
			map.AddMapping(
				outputStartPosition: new FilePosition(2, 28),
                outputEndPosition: new FilePosition(2, 29),
				sourceStartPosition: new FilePosition(2, 10),
				sourceName: "two.js",
				symbolName: "n"
			);

			var mapJson = map.ToJsonString("min.js");

			return JObject.Parse(mapJson);
		}
	}
}
