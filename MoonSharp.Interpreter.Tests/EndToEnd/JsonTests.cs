using System.Globalization;
using System.Threading;
using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd {
	public class JsonTests
	{
		// German uses , as a decimal separator, we don't want that
		private static readonly CultureInfo _badCulture = new CultureInfo("de-DE");

		[Test]
		public void SerializeNoLocalize()
		{
			Thread.CurrentThread.CurrentCulture = _badCulture;
			Assert.AreEqual("{\"test\":0.01}", Script.RunString("return json.serialize({test = 0.01})").String);
		}

		[Test]
		public void ParseNoLocalize()
		{
			Thread.CurrentThread.CurrentCulture = _badCulture;
			Assert.AreEqual(1.23, Script.RunString("return json.parse('{\"test\":1.23}').test").Number);
		}
	}
}
