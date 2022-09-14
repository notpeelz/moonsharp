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

		private struct MyStruct { }

		private class StructTest
		{
			public void TakesNonNullableStruct(MyStruct s = default) { }
		}

		[Test]
		public void TakesOptionalNonNullableStruct()
		{
			var s = new Script();
			UserData.RegisterType<MyStruct>();
			UserData.RegisterType<StructTest>(InteropAccessMode.LazyOptimized);
			s.Globals["Test"] = UserData.CreateStatic<StructTest>();
			s.DoString("Test.__new().TakesNonNullableStruct()");
		}
	}
}
