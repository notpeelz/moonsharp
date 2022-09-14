using NUnit.Framework;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	[TestFixture]
	public class ScriptToClrTests
	{
		private enum MyEnum
		{
			A = 1,
		}

		private class EnumTest
		{
			public void TakesNullableEnum(MyEnum? myEnum) { }
		}

		[Test]
		public void TakesNullableEnum()
		{
			var s = new Script();
			UserData.RegisterType<EnumTest>(InteropAccessMode.Reflection);
			s.Globals["Test"] = UserData.CreateStatic<EnumTest>();
			s.DoString("Test.__new().TakesNullableEnum(1)");
		}
	}
}
