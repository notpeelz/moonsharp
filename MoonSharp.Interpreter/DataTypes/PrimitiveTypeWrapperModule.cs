using System;
using System.Collections.Generic;
using System.Reflection;

namespace MoonSharp.Interpreter.Interop
{
	[MoonSharpModule]
	public class PrimitiveTypeWrapperModule
	{
		internal const int DEFAULT_RADIX = 10;

		internal static readonly HashSet<Type> Types;

		private static readonly Dictionary<Type, WrapperDefinition> TypeWrappers;

		static PrimitiveTypeWrapperModule()
		{
			TypeWrappers = new Dictionary<Type, WrapperDefinition>()
			{
				{ typeof(sbyte), new WrapperDefinition<LuaSByte, sbyte>() },
				{ typeof(byte), new WrapperDefinition<LuaByte, byte>() },
				{ typeof(short), new WrapperDefinition<LuaInt16, short>() },
				{ typeof(ushort), new WrapperDefinition<LuaUInt16, ushort>() },
				{ typeof(int), new WrapperDefinition<LuaInt32, int>() },
				{ typeof(uint), new WrapperDefinition<LuaUInt32, uint>() },
				{ typeof(long), new WrapperDefinition<LuaInt64, long>() },
				{ typeof(ulong), new WrapperDefinition<LuaUInt64, ulong>() },
				{ typeof(float), new WrapperDefinition<LuaSingle, float>() },
				{ typeof(decimal), new WrapperDefinition<LuaDecimal, decimal>() },
			};
			Types = new HashSet<Type>(TypeWrappers.Keys);
		}

		private abstract class WrapperDefinition
		{
			public abstract Type WrapperType { get; }

			public abstract Type PrimitiveType { get; }

			public abstract object CreateWrapperFromClrPrimitive(object v);

			public abstract DynValue CreateDynValueFromLuaCall(ScriptExecutionContext e, CallbackArguments args);

			public abstract DynValue CreateDynValue(Script owner, object o);
		}

		private class WrapperDefinition<W, P> : WrapperDefinition
		{
			private readonly ConstructorInfo fromPrimitiveCtor;
			private readonly MethodInfo createFromLuaCbMethod;

			public WrapperDefinition()
			{
				fromPrimitiveCtor = typeof(W).GetConstructor(BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance, null, new[] { typeof(P) }, null)
					?? throw new InvalidOperationException($"Wrapper {typeof(W)} doesn't have a ctor with exactly 1 parameter of type {typeof(P)}");
				createFromLuaCbMethod = typeof(W).GetMethod("__call", BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static)
					?? throw new InvalidOperationException($"Wrapper {typeof(W)} doesn't have a __call method");
			}

			public override Type WrapperType => typeof(W);

			public override Type PrimitiveType => typeof(P);

			public override object CreateWrapperFromClrPrimitive(object v) => (W)fromPrimitiveCtor.Invoke(new object[] { v });

			private static double GetOperandImpl(string fnName, CallbackArguments args, int idx)
			{
				if (args.RawGet(idx, true) is DynValue luaValue && luaValue.Type == DataType.UserData)
				{
					if (luaValue.UserData.Object is IPrimitiveTypeWrapper primitiveType)
					{
						return primitiveType.ToDouble();
					}

					throw ScriptRuntimeException.BadArgumentUserData(idx, fnName, typeof(IPrimitiveTypeWrapper), luaValue.UserData.Object?.GetType(), false);
				}

				return args.AsType(idx, fnName, DataType.Number, false).Number;
			}

			public override DynValue CreateDynValueFromLuaCall(ScriptExecutionContext e, CallbackArguments args)
			{
				var luaValue = (DynValue)createFromLuaCbMethod.Invoke(null, new object[] { e, args });
				SetMetaTable(e.OwnerScript, luaValue);
				return luaValue;
			}

			public override DynValue CreateDynValue(Script owner, object o)
			{
				var luaValue = UserData.Create(CreateWrapperFromClrPrimitive(o));
				SetMetaTable(owner, luaValue);
				return luaValue;
			}

			private void SetMetaTable(Script owner, DynValue luaValue)
			{
				luaValue.UserData.MetaTable ??= new Table(owner);
				luaValue.UserData.MetaTable["__eq"] ??= DynValue.NewCallback((_, args) =>
				{
					double GetOperand(int idx) => GetOperandImpl($"{WrapperType}__eq", args, idx);
					var a = GetOperand(0);
					var b = GetOperand(1);
					return DynValue.NewBoolean(a == b);

				}, $"{WrapperType.Name}__eq");
				luaValue.UserData.MetaTable["__lt"] ??= DynValue.NewCallback((_, args) =>
				{
					double GetOperand(int idx) => GetOperandImpl($"{WrapperType}__lt", args, idx);
					var a = GetOperand(0);
					var b = GetOperand(1);
					return DynValue.NewBoolean(a < b);
				}, $"{WrapperType.Name}__lt");
				luaValue.UserData.MetaTable["__le"] ??= DynValue.NewCallback((_, args) =>
				{
					double GetOperand(int idx) => GetOperandImpl($"{WrapperType}__le", args, idx);
					var a = GetOperand(0);
					var b = GetOperand(1);
					return DynValue.NewBoolean(a <= b);
				}, $"{WrapperType.Name}__le");
			}
		}

		public static void MoonSharpInit(Table _, Table moduleTable)
		{
			RegisterWrapperTypes();
			foreach (var (clrType, wrapper) in TypeWrappers)
			{
				var type = UserData.CreateStatic(wrapper.WrapperType);

				type.UserData.MetaTable = new Table(moduleTable.OwnerScript)
				{
					["__call"] = DynValue.NewCallback(wrapper.CreateDynValueFromLuaCall, $"{wrapper.WrapperType}__call"),
				};
				moduleTable.Set(clrType.Name, type);
			}
		}

		internal static void RegisterWrapperTypes()
		{
			foreach (var (_, wrapper) in TypeWrappers)
			{
				UserData.RegisterType(wrapper.WrapperType);
			}
		}

		public static DynValue CreateDynValue(Script owner, object obj)
		{
			if (TypeWrappers.TryGetValue(obj?.GetType(), out var e))
			{
				return e.CreateDynValue(owner, obj);
			}

			throw new ArgumentException("Invalid primitive type", nameof(obj));
		}
	}

	public interface IPrimitiveTypeWrapper
	{
		double ToDouble();

		object ToClrPrimitive();

		string ToString(string format);

		string ToString(IFormatProvider provider);

		string ToString(string format, IFormatProvider provider);
	}

	public readonly struct LuaSByte : IPrimitiveTypeWrapper
	{
		private readonly sbyte value;

		public LuaSByte(double v)
		{
			value = (sbyte)v;
		}

		public LuaSByte(IPrimitiveTypeWrapper w)
		{
			value = (sbyte)Convert.ChangeType(w.ToClrPrimitive(), typeof(sbyte));
		}

		[MoonSharpHidden]
		internal LuaSByte(sbyte v)
		{
			value = v;
		}

		public LuaSByte(string v, int radix = PrimitiveTypeWrapperModule.DEFAULT_RADIX)
		{
			value = Convert.ToSByte(v, radix);
		}

		public double ToDouble() => value;

		public object ToClrPrimitive() => value;

		public static implicit operator sbyte(LuaSByte luaValue) => luaValue.value;

		public static explicit operator LuaSByte(sbyte clrValue) => new LuaSByte(clrValue);

		public override string ToString() => value.ToString();

		public string ToString(string format) => value.ToString(format);

		public string ToString(IFormatProvider provider) => value.ToString(provider);

		public string ToString(string format, IFormatProvider provider) => value.ToString(format, provider);

		public override bool Equals(object obj) => obj is sbyte v && value == v
			|| obj is LuaSByte o && value == o.value;

		public override int GetHashCode() => value.GetHashCode();

		private static DynValue __call(ScriptExecutionContext _, CallbackArguments args)
		{
			if (args.Count < 2) throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaSByte));

			switch (args[1].Type)
			{
				case DataType.Number:
				{
					var n = args.AsType(1, nameof(LuaSByte), DataType.Number, false).Number;
					return UserData.Create(new LuaSByte(n));
				}
				case DataType.UserData when args[1].UserData.Object is IPrimitiveTypeWrapper primitive:
				{
					return UserData.Create(new LuaSByte(primitive));
				}
				case DataType.String:
				{
					var str = args[1].CastToString();
					var radix = (int?)(args.AsType(2, nameof(LuaSByte), DataType.Number, true)?.Number) ?? PrimitiveTypeWrapperModule.DEFAULT_RADIX;
					return UserData.Create(new LuaSByte(str, radix));
				}
				default: throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaSByte));
			}
		}
	}

	public readonly struct LuaByte : IPrimitiveTypeWrapper
	{
		private readonly byte value;

		public LuaByte(double v)
		{
			value = (byte)v;
		}

		public LuaByte(IPrimitiveTypeWrapper w)
		{
			value = (byte)Convert.ChangeType(w.ToClrPrimitive(), typeof(byte));
		}

		[MoonSharpHidden]
		internal LuaByte(byte v)
		{
			value = v;
		}

		public LuaByte(string v, int radix = PrimitiveTypeWrapperModule.DEFAULT_RADIX)
		{
			value = Convert.ToByte(v, radix);
		}

		public double ToDouble() => value;

		public object ToClrPrimitive() => value;

		public static implicit operator byte(LuaByte luaValue) => luaValue.value;

		public static explicit operator LuaByte(byte clrValue) => new LuaByte(clrValue);

		public override string ToString() => value.ToString();

		public string ToString(string format) => value.ToString(format);

		public string ToString(IFormatProvider provider) => value.ToString(provider);

		public string ToString(string format, IFormatProvider provider) => value.ToString(format, provider);

		public override bool Equals(object obj) => obj is byte v && value == v
			|| obj is LuaByte o && value == o.value;

		public override int GetHashCode() => value.GetHashCode();

		private static DynValue __call(ScriptExecutionContext _, CallbackArguments args)
		{
			if (args.Count < 2) throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaByte));

			switch (args[1].Type)
			{
				case DataType.Number:
				{
					var n = args.AsType(1, nameof(LuaByte), DataType.Number, false).Number;
					return UserData.Create(new LuaByte(n));
				}
				case DataType.UserData when args[1].UserData.Object is IPrimitiveTypeWrapper primitive:
				{
					return UserData.Create(new LuaByte(primitive));
				}
				case DataType.String:
				{
					var str = args[1].CastToString();
					var radix = (int?)(args.AsType(2, nameof(LuaByte), DataType.Number, true)?.Number) ?? PrimitiveTypeWrapperModule.DEFAULT_RADIX;
					return UserData.Create(new LuaByte(str, radix));
				}
				default: throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaByte));
			}
		}
	}

	public readonly struct LuaInt16 : IPrimitiveTypeWrapper
	{
		private readonly short value;

		public LuaInt16(double v)
		{
			value = (short)v;
		}

		public LuaInt16(IPrimitiveTypeWrapper w)
		{
			value = (short)Convert.ChangeType(w.ToClrPrimitive(), typeof(short));
		}

		[MoonSharpHidden]
		internal LuaInt16(short v)
		{
			value = v;
		}

		public LuaInt16(string v, int radix = PrimitiveTypeWrapperModule.DEFAULT_RADIX)
		{
			value = Convert.ToInt16(v, radix);
		}

		public double ToDouble() => value;

		public object ToClrPrimitive() => value;

		public static implicit operator short(LuaInt16 luaValue) => luaValue.value;

		public static explicit operator LuaInt16(short clrValue) => new LuaInt16(clrValue);

		public override string ToString() => value.ToString();

		public string ToString(string format) => value.ToString(format);

		public string ToString(IFormatProvider provider) => value.ToString(provider);

		public string ToString(string format, IFormatProvider provider) => value.ToString(format, provider);

		public override bool Equals(object obj) => obj is short v && value == v
			|| obj is LuaInt16 o && value == o.value;

		public override int GetHashCode() => value.GetHashCode();

		private static DynValue __call(ScriptExecutionContext _, CallbackArguments args)
		{
			if (args.Count < 2) throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaInt16));

			switch (args[1].Type)
			{
				case DataType.Number:
				{
					var n = args.AsType(1, nameof(LuaInt16), DataType.Number, false).Number;
					return UserData.Create(new LuaInt16(n));
				}
				case DataType.UserData when args[1].UserData.Object is IPrimitiveTypeWrapper primitive:
				{
					return UserData.Create(new LuaInt16(primitive));
				}
				case DataType.String:
				{
					var str = args[1].CastToString();
					var radix = (int?)(args.AsType(2, nameof(LuaInt16), DataType.Number, true)?.Number) ?? PrimitiveTypeWrapperModule.DEFAULT_RADIX;
					return UserData.Create(new LuaInt16(str, radix));
				}
				default: throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaInt16));
			}
		}
	}

	public readonly struct LuaUInt16 : IPrimitiveTypeWrapper
	{
		private readonly ushort value;

		public LuaUInt16(double v)
		{
			value = (ushort)v;
		}

		public LuaUInt16(IPrimitiveTypeWrapper w)
		{
			value = (ushort)Convert.ChangeType(w.ToClrPrimitive(), typeof(ushort));
		}

		[MoonSharpHidden]
		internal LuaUInt16(ushort v)
		{
			value = v;
		}

		public LuaUInt16(string v, int radix = PrimitiveTypeWrapperModule.DEFAULT_RADIX)
		{
			value = Convert.ToUInt16(v, radix);
		}

		public double ToDouble() => value;

		public static implicit operator ushort(LuaUInt16 luaValue) => luaValue.value;

		public static explicit operator LuaUInt16(ushort clrValue) => new LuaUInt16(clrValue);

		public object ToClrPrimitive() => value;

		public override string ToString() => value.ToString();

		public string ToString(string format) => value.ToString(format);

		public string ToString(IFormatProvider provider) => value.ToString(provider);

		public string ToString(string format, IFormatProvider provider) => value.ToString(format, provider);

		public override bool Equals(object obj) => obj is ushort v && value == v
			|| obj is LuaUInt16 o && value == o.value;

		public override int GetHashCode() => value.GetHashCode();

		private static DynValue __call(ScriptExecutionContext _, CallbackArguments args)
		{
			if (args.Count < 2) throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaUInt16));

			switch (args[1].Type)
			{
				case DataType.Number:
				{
					var n = args.AsType(1, nameof(LuaUInt16), DataType.Number, false).Number;
					return UserData.Create(new LuaUInt16(n));
				}
				case DataType.UserData when args[1].UserData.Object is IPrimitiveTypeWrapper primitive:
				{
					return UserData.Create(new LuaUInt16(primitive));
				}
				case DataType.String:
				{
					var str = args[1].CastToString();
					var radix = (int?)(args.AsType(2, nameof(LuaUInt16), DataType.Number, true)?.Number) ?? PrimitiveTypeWrapperModule.DEFAULT_RADIX;
					return UserData.Create(new LuaUInt16(str, radix));
				}
				default: throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaUInt16));
			}
		}
	}

	public readonly struct LuaInt32 : IPrimitiveTypeWrapper
	{
		private readonly int value;

		public LuaInt32(double v)
		{
			value = (int)v;
		}

		public LuaInt32(IPrimitiveTypeWrapper w)
		{
			value = (int)Convert.ChangeType(w.ToClrPrimitive(), typeof(int));
		}

		[MoonSharpHidden]
		internal LuaInt32(int v)
		{
			value = v;
		}

		public LuaInt32(string v, int radix = PrimitiveTypeWrapperModule.DEFAULT_RADIX)
		{
			value = Convert.ToInt32(v, radix);
		}

		public double ToDouble() => value;

		public object ToClrPrimitive() => value;

		public static implicit operator int(LuaInt32 luaValue) => luaValue.value;

		public static explicit operator LuaInt32(int clrValue) => new LuaInt32(clrValue);

		public override string ToString() => value.ToString();

		public string ToString(string format) => value.ToString(format);

		public string ToString(IFormatProvider provider) => value.ToString(provider);

		public string ToString(string format, IFormatProvider provider) => value.ToString(format, provider);

		public override bool Equals(object obj) => obj is int v && value == v
			|| obj is LuaInt32 o && value == o.value;

		public override int GetHashCode() => value.GetHashCode();

		private static DynValue __call(ScriptExecutionContext _, CallbackArguments args)
		{
			if (args.Count < 2) throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaInt32));

			switch (args[1].Type)
			{
				case DataType.Number:
				{
					var n = args.AsType(1, nameof(LuaInt32), DataType.Number, false).Number;
					return UserData.Create(new LuaInt32(n));
				}
				case DataType.UserData when args[1].UserData.Object is IPrimitiveTypeWrapper primitive:
				{
					return UserData.Create(new LuaInt32(primitive));
				}
				case DataType.String:
				{
					var str = args[1].CastToString();
					var radix = (int?)(args.AsType(2, nameof(LuaInt32), DataType.Number, true)?.Number) ?? PrimitiveTypeWrapperModule.DEFAULT_RADIX;
					return UserData.Create(new LuaInt32(str, radix));
				}
				default: throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaInt32));
			}
		}
	}

	public readonly struct LuaUInt32 : IPrimitiveTypeWrapper
	{
		private readonly uint value;

		public LuaUInt32(double v)
		{
			value = (uint)v;
		}

		public LuaUInt32(IPrimitiveTypeWrapper w)
		{
			value = (uint)Convert.ChangeType(w.ToClrPrimitive(), typeof(uint));
		}

		[MoonSharpHidden]
		internal LuaUInt32(uint v)
		{
			value = v;
		}

		public LuaUInt32(string v, int radix = PrimitiveTypeWrapperModule.DEFAULT_RADIX)
		{
			value = Convert.ToUInt32(v, radix);
		}

		public double ToDouble() => value;

		public object ToClrPrimitive() => value;

		public static implicit operator uint(LuaUInt32 luaValue) => luaValue.value;

		public static explicit operator LuaUInt32(uint clrValue) => new LuaUInt32(clrValue);

		public override string ToString() => value.ToString();

		public string ToString(string format) => value.ToString(format);

		public string ToString(IFormatProvider provider) => value.ToString(provider);

		public string ToString(string format, IFormatProvider provider) => value.ToString(format, provider);

		public override bool Equals(object obj) => obj is uint v && value == v
			|| obj is LuaUInt32 o && value == o.value;

		public override int GetHashCode() => value.GetHashCode();

		private static DynValue __call(ScriptExecutionContext _, CallbackArguments args)
		{
			if (args.Count < 2) throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaUInt32));

			switch (args[1].Type)
			{
				case DataType.Number:
				{
					var n = args.AsType(1, nameof(LuaUInt32), DataType.Number, false).Number;
					return UserData.Create(new LuaUInt32(n));
				}
				case DataType.UserData when args[1].UserData.Object is IPrimitiveTypeWrapper primitive:
				{
					return UserData.Create(new LuaUInt32(primitive));
				}
				case DataType.String:
				{
					var str = args[1].CastToString();
					var radix = (int?)(args.AsType(2, nameof(LuaUInt32), DataType.Number, true)?.Number) ?? PrimitiveTypeWrapperModule.DEFAULT_RADIX;
					return UserData.Create(new LuaUInt32(str, radix));
				}
				default: throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaUInt32));
			}
		}
	}

	public readonly struct LuaInt64 : IPrimitiveTypeWrapper
	{
		private readonly long value;

		public LuaInt64(double v)
		{
			value = (long)v;
		}

		public LuaInt64(IPrimitiveTypeWrapper w)
		{
			value = (long)Convert.ChangeType(w.ToClrPrimitive(), typeof(long));
		}

		[MoonSharpHidden]
		internal LuaInt64(long v)
		{
			value = v;
		}

		public LuaInt64(string v, int radix = PrimitiveTypeWrapperModule.DEFAULT_RADIX)
		{
			value = Convert.ToInt64(v, radix);
		}

		public LuaInt64(double lo, double hi)
		{
			value = Convert.ToUInt32(lo) | (long)Convert.ToUInt32(hi) << 32;
		}

		public double ToDouble() => value;

		public object ToClrPrimitive() => value;

		public static implicit operator long(LuaInt64 luaValue) => luaValue.value;

		public static explicit operator LuaInt64(long clrValue) => new LuaInt64(clrValue);

		public override string ToString() => value.ToString();

		public string ToString(string format) => value.ToString(format);

		public string ToString(IFormatProvider provider) => value.ToString(provider);

		public string ToString(string format, IFormatProvider provider) => value.ToString(format, provider);

		public override bool Equals(object obj) => obj is long v && value == v
			|| obj is LuaInt64 o && value == o.value;

		public override int GetHashCode() => value.GetHashCode();

		private static DynValue __call(ScriptExecutionContext _, CallbackArguments args)
		{
			if (args.Count < 2) throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaInt64));

			switch (args[1].Type)
			{
				case DataType.Number:
				{
					var n = args.AsType(1, nameof(LuaInt64), DataType.Number, false).Number;
					if (args.Count > 2)
					{
						var lo = n;
						var hi = args.AsType(2, nameof(LuaInt64), DataType.Number, true).Number;
						return UserData.Create(new LuaInt64(lo, hi));
					}
					return UserData.Create(new LuaInt64(n));
				}
				case DataType.UserData when args[1].UserData.Object is IPrimitiveTypeWrapper primitive:
				{
					return UserData.Create(new LuaInt64(primitive));
				}
				case DataType.String:
				{
					var str = args[1].CastToString();
					var radix = (int?)(args.AsType(2, nameof(LuaInt64), DataType.Number, true)?.Number) ?? PrimitiveTypeWrapperModule.DEFAULT_RADIX;
					return UserData.Create(new LuaInt64(str, radix));
				}
				default: throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaInt64));
			}
		}
	}

	public readonly struct LuaUInt64 : IPrimitiveTypeWrapper
	{
		private readonly ulong value;

		public LuaUInt64(double v)
		{
			value = (ulong)v;
		}

		public LuaUInt64(IPrimitiveTypeWrapper w)
		{
			value = (ulong)Convert.ChangeType(w.ToClrPrimitive(), typeof(ulong));
		}

		[MoonSharpHidden]
		internal LuaUInt64(ulong v)
		{
			value = v;
		}

		public LuaUInt64(string v, int radix = PrimitiveTypeWrapperModule.DEFAULT_RADIX)
		{
			value = Convert.ToUInt64(v, radix);
		}

		public LuaUInt64(double lo, double hi)
		{
			value = Convert.ToUInt32(lo) | (ulong)Convert.ToUInt32(hi) << 32;
		}

		public double ToDouble() => value;

		public object ToClrPrimitive() => value;

		public static implicit operator ulong(LuaUInt64 luaValue) => luaValue.value;

		public static explicit operator LuaUInt64(ulong clrValue) => new LuaUInt64(clrValue);

		public override string ToString() => value.ToString();

		public string ToString(string format) => value.ToString(format);

		public string ToString(IFormatProvider provider) => value.ToString(provider);

		public string ToString(string format, IFormatProvider provider) => value.ToString(format, provider);

		public override bool Equals(object obj) => obj is ulong v && value == v
			|| obj is LuaUInt64 o && value == o.value;

		public override int GetHashCode() => value.GetHashCode();

		private static DynValue __call(ScriptExecutionContext _, CallbackArguments args)
		{
			if (args.Count < 2) throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaUInt64));

			switch (args[1].Type)
			{
				case DataType.Number:
				{
					var n = args.AsType(1, nameof(LuaUInt64), DataType.Number, false).Number;
					if (args.Count > 2)
					{
						var lo = n;
						var hi = args.AsType(2, nameof(LuaUInt64), DataType.Number, true).Number;
						return UserData.Create(new LuaUInt64(lo, hi));
					}
					return UserData.Create(new LuaUInt64(n));
				}
				case DataType.UserData when args[1].UserData.Object is IPrimitiveTypeWrapper primitive:
				{
					return UserData.Create(new LuaUInt64(primitive));
				}
				case DataType.String:
				{
					var str = args[1].CastToString();
					var radix = (int?)(args.AsType(2, nameof(LuaUInt64), DataType.Number, true)?.Number) ?? PrimitiveTypeWrapperModule.DEFAULT_RADIX;
					return UserData.Create(new LuaUInt64(str, radix));
				}
				default: throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaUInt64));
			}
		}
	}

	public readonly struct LuaSingle : IPrimitiveTypeWrapper
	{
		private readonly float value;

		public LuaSingle(double v)
		{
			value = (float)v;
		}

		public LuaSingle(IPrimitiveTypeWrapper w)
		{
			value = (float)Convert.ChangeType(w.ToClrPrimitive(), typeof(float));
		}

		[MoonSharpHidden]
		internal LuaSingle(float v)
		{
			value = v;
		}

		public LuaSingle(string v)
		{
			value = float.Parse(v);
		}

		public double ToDouble() => value;

		public object ToClrPrimitive() => value;

		public static implicit operator float(LuaSingle luaValue) => luaValue.value;

		public static explicit operator LuaSingle(float clrValue) => new LuaSingle(clrValue);

		public override string ToString() => value.ToString();

		public string ToString(string format) => value.ToString(format);

		public string ToString(IFormatProvider provider) => value.ToString(provider);

		public string ToString(string format, IFormatProvider provider) => value.ToString(format, provider);

		public override bool Equals(object obj) => obj is float v && value == v
			|| obj is LuaSingle o && value == o.value;

		public override int GetHashCode() => value.GetHashCode();

		private static DynValue __call(ScriptExecutionContext _, CallbackArguments args)
		{
			if (args.Count < 2) throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaSingle));

			switch (args[1].Type)
			{
				case DataType.Number:
				{
					var n = args.AsType(1, nameof(LuaSingle), DataType.Number, false).Number;
					return UserData.Create(new LuaSingle(n));
				}
				case DataType.UserData when args[1].UserData.Object is IPrimitiveTypeWrapper primitive:
				{
					return UserData.Create(new LuaSingle(primitive));
				}
				case DataType.String:
				{
					var str = args[1].CastToString();
					return UserData.Create(new LuaSingle(str));
				}
				default: throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaSingle));
			}
		}
	}

	public readonly struct LuaDecimal : IPrimitiveTypeWrapper
	{
		private readonly decimal value;

		public LuaDecimal(double v)
		{
			value = (decimal)v;
		}

		public LuaDecimal(IPrimitiveTypeWrapper w)
		{
			value = (decimal)Convert.ChangeType(w.ToClrPrimitive(), typeof(decimal));
		}

		[MoonSharpHidden]
		internal LuaDecimal(decimal v)
		{
			value = v;
		}

		public LuaDecimal(string v)
		{
			value = decimal.Parse(v);
		}

		public double ToDouble() => (double)value;

		public object ToClrPrimitive() => value;

		// TODO: add 4x uint ctor? i.e. (uint, uint, uint, uint)

		public static implicit operator decimal(LuaDecimal luaValue) => luaValue.value;

		public static explicit operator LuaDecimal(decimal clrValue) => new LuaDecimal(clrValue);

		public override string ToString() => value.ToString();

		public string ToString(string format) => value.ToString(format);

		public string ToString(IFormatProvider provider) => value.ToString(provider);

		public string ToString(string format, IFormatProvider provider) => value.ToString(format, provider);

		public override bool Equals(object obj) => obj is decimal v && value == v
			|| obj is LuaDecimal o && value == o.value;

		public override int GetHashCode() => value.GetHashCode();

		private static DynValue __call(ScriptExecutionContext _, CallbackArguments args)
		{
			if (args.Count < 2) throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaDecimal));

			switch (args[1].Type)
			{
				case DataType.Number:
				{
					var n = args.AsType(1, nameof(LuaDecimal), DataType.Number, false).Number;
					return UserData.Create(new LuaDecimal(n));
				}
				case DataType.UserData when args[1].UserData.Object is IPrimitiveTypeWrapper primitive:
				{
					return UserData.Create(new LuaDecimal(primitive));
				}
				case DataType.String:
				{
					var str = args[1].CastToString();
					return UserData.Create(new LuaDecimal(str));
				}
				default: throw ScriptRuntimeException.BadArgumentValueExpected(0, nameof(LuaDecimal));
			}
		}
	}
}
