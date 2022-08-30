using System;
using System.Linq.Expressions;
using System.Reflection;

namespace MoonSharp.Interpreter
{
	internal static class TypeExtensions
	{
		public static MethodInfo GetImplicitOperatorMethod(this Type baseType, Type targetType)
		{
			try
			{
				return Expression.Convert(Expression.Parameter(baseType, null), targetType).Method;
			}
			catch
			{
				if (baseType.BaseType != null)
				{
					return GetImplicitOperatorMethod(baseType.BaseType, targetType);
				}

				if (targetType.BaseType != null)
				{
					return GetImplicitOperatorMethod(baseType, targetType.BaseType);
				}

				return null;
			}
		}
	}
}
