using MoonSharp.Interpreter.Interop;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace MoonSharp.Interpreter.Tests.EndToEnd
{
	[TestFixture]
	public class PrimitiveTypeWrapperTests
	{
		private static readonly Type[] PrimitiveTypes = new[]
		{
			typeof(sbyte),
			typeof(byte),
			typeof(short),
			typeof(ushort),
			typeof(int),
			typeof(uint),
			typeof(long),
			typeof(ulong),
			typeof(float),
			typeof(decimal),
		};

		[Test]
		public void Operator_Add_Mixed_A() => TestAddition(1, (i, operand, t) => $"{t.Name}({i}) + {operand}");

		[Test]
		public void Operator_Add_Mixed_B() => TestAddition(1, (i, operand, t) => $"{i} + {t.Name}({operand})");

		[Test]
		public void Operator_Add_UserData() => TestAddition(1, (i, operand, t) => $"{t.Name}({i}) + {t.Name}({operand})");

		private static void TestAddition(int operand, Func<int, int, Type, object> func)
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					string FormatMessage(object o) => $"add_{type.Name}: {i} add {operand} = {o}";

					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var luaValue = s.DoString(@$"
						return string.format('{FormatMessage("%d")}', {func(i, operand, type)})
					");
					Assert.AreEqual(DataType.String, luaValue.Type);
					Assert.AreEqual(FormatMessage(i + operand), luaValue.String);
				});
			}
		}

		[Test]
		public void Operator_Sub_Mixed_A() => TestSubtraction(1, (i, operand, t) => $"{t.Name}({i}) - {operand}");

		[Test]
		public void Operator_Sub_Mixed_B() => TestSubtraction(1, (i, operand, t) => $"{i} - {t.Name}({operand})");

		[Test]
		public void Operator_Sub_UserData() => TestSubtraction(1, (i, operand, t) => $"{t.Name}({i}) - {t.Name}({operand})");

		private static void TestSubtraction(int operand, Func<int, int, Type, object> func)
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					string FormatMessage(object o) => $"sub_{type.Name}: {i} sub {operand} = {o}";

					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var luaValue = s.DoString(@$"
						return string.format('{FormatMessage("%d")}', {func(i, operand, type)})
					");
					Assert.AreEqual(DataType.String, luaValue.Type);
					Assert.AreEqual(FormatMessage(i - operand), luaValue.String);
				});
			}
		}

		[Test]
		public void Operator_Mul_Mixed_A() => TestMultiplication(2m, (i, multiplier, t) => $"{t.Name}({i}) * {multiplier}");

		[Test]
		public void Operator_Mul_Mixed_B() => TestMultiplication(2m, (i, multiplier, t) => $"{i} * {t.Name}({multiplier})");

		[Test]
		public void Operator_Mul_UserData() => TestMultiplication(2m, (i, multiplier, t) => $"{t.Name}({i}) * {t.Name}({multiplier})");

		private static void TestMultiplication(decimal multiplier, Func<int, decimal, Type, object> func)
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					string FormatMessage(object o) => $"div_{type.Name}: {i} mult {multiplier} = {o}";

					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var luaValue = s.DoString(@$"
						return string.format('{FormatMessage("%0.4f")}', {func(i, multiplier, type)})
					");
					Assert.AreEqual(DataType.String, luaValue.Type);
					Assert.AreEqual(FormatMessage((i * multiplier).ToString("0.0000")), luaValue.String);
				});
			}
		}

		[Test]
		public void Operator_Div_Mixed_A() => TestDivision(2m, (i,divisor, t) => $"{t.Name}({i}) / {divisor}");

		[Test]
		public void Operator_Div_Mixed_B() => TestDivision(2m, (i, divisor, t) => $"{i} / {t.Name}({divisor})");

		[Test]
		public void Operator_Div_UserData() => TestDivision(2m, (i, divisor, t) => $"{t.Name}({i}) / {t.Name}({divisor})");

		private static void TestDivision(decimal divisor, Func<int, decimal, Type, object> func)
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					string FormatMessage(object o) => $"div_{type.Name}: {i} div {divisor} = {o}";

					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var luaValue = s.DoString(@$"
						return string.format('{FormatMessage("%0.4f")}', {func(i, divisor, type)})
					");
					Assert.AreEqual(DataType.String, luaValue.Type);
					Assert.AreEqual(FormatMessage((i / divisor).ToString("0.0000")), luaValue.String);
				});
			}
		}

		[Test]
		public void Operator_Mod_Mixed() => TestModulo(3, (x, t) => x);

		[Test]
		public void Operator_Mod_UserData() => TestModulo(3, (x, t) => $"{t.Name}({x})");

		private static void TestModulo(int modulus, Func<int, Type, object> func)
		{
			// C#'s `%` operator doesn't work with negative numbers
			static decimal RealModulo(decimal a, decimal b) => a - b * Math.Floor(a / b);

			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					string FormatMessage(object o) => $"mod_{type.Name}: {i} mod {modulus} = {o}";

					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var luaValue = s.DoString(@$"
						local value = {type.Name}({i})
						return string.format('{FormatMessage("%d")}', value % {func(modulus, type)})
					");
					Assert.AreEqual(DataType.String, luaValue.Type);
					Assert.AreEqual(FormatMessage(RealModulo(i, modulus)), luaValue.String);
				});
			}
		}

		[Test]
		public void Operator_Eq_A() => TestEquality(i => $"value == {i}");

		[Test]
		public void Operator_Eq_B() => TestEquality(i => $"{i} == value");

		[Test]
		public void Operator_Gt_A() => TestEquality(i => $"value > {i - 1}");

		[Test]
		public void Operator_Gt_B() => TestEquality(i => $"{i + 1} > value");

		[Test]
		public void Operator_Gte_A() => TestEquality(i => $"value >= {i - 1} and value >= {i}");

		[Test]
		public void Operator_Gte_B() => TestEquality(i => $"{i + 1} >= value and {i} >= value");

		[Test]
		public void Operator_Lt_A() => TestEquality(i => $"value < {i + 1}");

		[Test]
		public void Operator_Lt_B() => TestEquality(i => $"{i - 1} < value");

		[Test]
		public void Operator_Lte_A() => TestEquality(i => $"value <= {i + 1} and value <= {i}");

		[Test]
		public void Operator_Lte_B() => TestEquality(i => $"{i - 1} <= value and {i} <= value");

		private static void TestEquality(Func<int, object> func)
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var luaValue = s.DoString(@$"
						local value = {type.Name}({i})
						return value == value and ({func(i)})
					");
					Assert.AreEqual(DataType.Boolean, luaValue.Type);
					Assert.True(luaValue.Boolean);
				});
			}
		}

		[Test]
		public void Operator_Pow_Mixed_A() => TestExponentiation(2, (i, exponent, t) => $"({t.Name}({i})) ^ {exponent}");

		[Test]
		public void Operator_Pow_Mixed_B() => TestExponentiation(2, (i, exponent, t) => $"({i}) ^ {t.Name}({exponent})");

		[Test]
		public void Operator_Pow_UserData() => TestExponentiation(2, (i, exponent, t) => $"({t.Name}({i})) ^ {t.Name}({exponent})");

		private static void TestExponentiation(int operand, Func<int, int, Type, object> func)
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					string FormatMessage(object o) => $"pow_{type.Name}: {i} pow {operand} = {o}";
					var luaValue = s.DoString(@$"
						return string.format('{FormatMessage("%d")}', {func(i, operand, type)})
					");
					Assert.AreEqual(DataType.String, luaValue.Type);
					Assert.AreEqual(FormatMessage(Math.Pow(i, operand)), luaValue.String);
				});
			}
		}

		[Test]
		public void Operator_Unm()
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var luaValue = s.DoString(@$"
						return -({type.Name}({i}))
					");
					Assert.AreEqual(DataType.Number, luaValue.Type);
					Assert.AreEqual(-i, luaValue.Number);
				});
			}
		}

		[Test]
		public void Operator_Concat()
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var luaValue = s.DoString(@$"
						return 'test' .. {type.Name}({i})
					");
					Assert.AreEqual(DataType.String, luaValue.Type);
					Assert.AreEqual($"test{i}", luaValue.String);
				});
			}
		}

		[Test]
		public void ToNumber()
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var luaValue = s.DoString(@$"
						local value = {type.Name}({i})
						return string.format('%d', tonumber(value))
					");
					Assert.AreEqual(DataType.String, luaValue.Type);
					Assert.AreEqual(i.ToString(), luaValue.String);
				});
			}
		}

		[Test]
		public void ToNumber_Implicit_StringFormat()
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var luaValue = s.DoString(@$"
						local value = {type.Name}({i})
						return string.format('%d', value)
					");
					Assert.AreEqual(DataType.String, luaValue.Type);
					Assert.AreEqual(i.ToString(), luaValue.String);
				});
			}
		}

		[Test]
		public void ToNumber_Implicit_MathAbs()
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -5 : 0, 5, i =>
				{
					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var luaValue = s.DoString(@$"return math.abs({type.Name}({i}))");
					Assert.AreEqual(DataType.Number, luaValue.Type);
					Assert.AreEqual(Math.Abs(i), luaValue.Number);
				});
			}
		}

		[Test]
		public void TypeCheck()
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestContext.WriteLine($"Testing type {type.Name}");

				var luaValue = s.DoString(@$"return type({type.Name}(5))");
				Assert.AreEqual(DataType.String, luaValue.Type);
				Assert.AreEqual("number", luaValue.String);
			}
		}

		public class TakesNumber<T>
		{
			public double GetValue(T number) => (double)Convert.ChangeType(number, typeof(double));
		}

		[Test]
		public void Ctor_Cast()
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				UserData.RegisterType(typeof(TakesNumber<>).MakeGenericType(type));
				s.Globals[$"Test{type.Name}"] = UserData.CreateStatic(typeof(TakesNumber<>).MakeGenericType(type));

				foreach (var wrapperType in PrimitiveTypes)
				{
					TestWithNumbers(CanRepresentNegativeNumbers(wrapperType) ? -5 : 0, 5, i =>
					{
						TestContext.WriteLine($"Testing conversion of {wrapperType.Name}({i}) to {type.Name}");
						if (CanRepresentNegativeNumbers(wrapperType) && !CanRepresentNegativeNumbers(type) && i < 0)
						{
							var runtimeException = Assert.Throws<NetRuntimeException>(() =>
							{
								var luaValue = s.DoString($@"
									local test = Test{type.Name}.__new()
									local x = {wrapperType.Name}({i})
									return test.GetValue({type.Name}(x))
								");
							});
							Assert.IsInstanceOf<TargetInvocationException>(runtimeException.InnerException);
							Assert.IsInstanceOf<OverflowException>(runtimeException.InnerException.InnerException);
						}
						else
						{
							var luaValue = s.DoString($@"
								local test = Test{type.Name}.__new()
								local x = {wrapperType.Name}({i})
								return test.GetValue({type.Name}(x))
							");
							Assert.AreEqual(DataType.Number, luaValue.Type);
							Assert.AreEqual(i, luaValue.Number);
						}
					});
				}
			}
		}

		public class ReturnsNumber<T>
		{
			public T GetValue() => default;
		}

		[Test]
		public void DowngradeToLuaNumber()
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestContext.WriteLine($"Testing type {type.Name}");

				UserData.RegisterType(typeof(ReturnsNumber<>).MakeGenericType(type));
				s.Globals[$"Test{type.Name}"] = UserData.CreateStatic(typeof(ReturnsNumber<>).MakeGenericType(type));

				var luaValue = s.DoString($@"
					local test = Test{type.Name}.__new()
					return test.GetValue() + 1
				");
				Assert.AreEqual(DataType.Number, luaValue.Type);
			}
		}

		[Test]
		public void ClrTypeToWrapper_ReturnValue()
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestContext.WriteLine($"Testing type {type.Name}");

				UserData.RegisterType(typeof(ReturnsNumber<>).MakeGenericType(type));
				s.Globals[$"Test{type.Name}"] = UserData.CreateStatic(typeof(ReturnsNumber<>).MakeGenericType(type));

				var luaValue = s.DoString($@"
					local test = Test{type.Name}.__new()
					return test.GetValue()
				");
				var primitive = AssertValidPrimitiveTypeWrapper(luaValue);
				Assert.AreEqual(0, primitive.ToClrPrimitive());
			}
		}

		[Test]
		public void Ctor_Literal()
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestContext.WriteLine($"Testing type {type.Name}");

				var expectedValue = Convert.ChangeType(5, type);
				var luaValue = s.DoString($"return {type.Name}({expectedValue})");
				var primitive = AssertValidPrimitiveTypeWrapper(luaValue);
				Assert.AreEqual(expectedValue, primitive.ToClrPrimitive());
			}
		}

		[Test]
		public void Ctor_HiLo()
		{
			var s = new Script();

			{
				var luaValue = s.DoString($"return Int64({uint.MaxValue}, {uint.MaxValue})");
				var primitive = AssertValidPrimitiveTypeWrapper(luaValue);
				Assert.AreEqual(-1L, primitive.ToClrPrimitive());
			}

			{
				var luaValue = s.DoString($"return UInt64({uint.MaxValue}, {uint.MaxValue})");
				var primitive = AssertValidPrimitiveTypeWrapper(luaValue);
				Assert.AreEqual(0xFF_FF_FF_FF__FF_FF_FF_FFUL, primitive.ToClrPrimitive());
			}
		}

		[Test]
		public void Ctor_Radix()
		{
			var s = new Script();
			foreach (var type in PrimitiveTypes)
			{
				TestWithNumbers(CanRepresentNegativeNumbers(type) ? -16 : 0, 32, i =>
				{
					if (!CanFormatWithRadix(type)) return;

					TestContext.WriteLine($"Testing type {type.Name} with {i}");

					var expectedValue = Convert.ChangeType(i, type);
					var luaValue = s.DoString($"return {type.Name}('{expectedValue:X}', 16)");
					var primitive = AssertValidPrimitiveTypeWrapper(luaValue);
					Assert.AreEqual(expectedValue, primitive.ToClrPrimitive());
				});
			}
		}

		private static IPrimitiveTypeWrapper AssertValidPrimitiveTypeWrapper(DynValue luaValue)
		{
			Assert.AreEqual(DataType.UserData, luaValue.Type);
			Assert.NotNull(luaValue.UserData.Object);
			Assert.IsInstanceOf<IPrimitiveTypeWrapper>(luaValue.UserData.Object);
			Assert.NotNull(luaValue.UserData.MetaTable);
			Assert.NotNull(luaValue.UserData.MetaTable["__lt"]);
			Assert.NotNull(luaValue.UserData.MetaTable["__le"]);
			Assert.NotNull(luaValue.UserData.MetaTable["__eq"]);
			return (IPrimitiveTypeWrapper)luaValue.UserData.Object;
		}

		private static void TestWithNumbers(int start, int end, Action<int> action)
		{
			var numbers = Enumerable.Range(start, end - start + 1);
			TestWithNumbers(numbers, action);
		}

		private static void TestWithNumbers(IEnumerable<int> numbers, Action<int> action)
		{
			foreach (var n in numbers)
			{
				action(n);
			}
		}

		private static bool CanRepresentNegativeNumbers(Type type)
		{
			return Type.GetTypeCode(type) switch
			{
				TypeCode.Byte => false,
				TypeCode.SByte => true,
				TypeCode.Int16 => true,
				TypeCode.UInt16 => false,
				TypeCode.Int32 => true,
				TypeCode.UInt32 => false,
				TypeCode.Int64 => true,
				TypeCode.UInt64 => false,
				TypeCode.Single => true,
				TypeCode.Double => true,
				TypeCode.Decimal => true,
				_ => throw new NotImplementedException(),
			};
		}

		private static bool CanFormatWithRadix(Type type)
		{
			return Type.GetTypeCode(type) switch
			{
				TypeCode.Byte => true,
				TypeCode.SByte => true,
				TypeCode.Int16 => true,
				TypeCode.UInt16 => true,
				TypeCode.Int32 => true,
				TypeCode.UInt32 => true,
				TypeCode.Int64 => true,
				TypeCode.UInt64 => true,
				TypeCode.Single => false,
				TypeCode.Double => false,
				TypeCode.Decimal => false,
				_ => throw new NotImplementedException(),
			};
		}
	}
}
