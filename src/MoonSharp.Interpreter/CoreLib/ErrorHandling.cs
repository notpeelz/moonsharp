﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MoonSharp.Interpreter.Execution;
using MoonSharp.Interpreter.Execution.VM;

namespace MoonSharp.Interpreter.CoreLib
{
	[MoonSharpModule]
	public class ErrorHandling
	{
		[MoonSharpMethod]
		public static DynValue pcall(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			DynValue v = args[0];
			DynValue[] a = new DynValue[args.Count - 1];

			for (int i = 1; i < args.Count; i++)
				a[i - 1] = args[i];

			return DynValue.NewTailCallReq(new TailCallData()
			{
				Args = a,
				Function = v,
				Continuation = new CallbackFunction(pcall_continuation),
				ErrorHandler = new CallbackFunction(pcall_onerror)
			});
		}

		public static DynValue pcall_continuation(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewTupleNested(DynValue.True, args[0]);
		}

		public static DynValue pcall_onerror(ScriptExecutionContext executionContext, CallbackArguments args)
		{
			return DynValue.NewTupleNested(DynValue.True, args[0]);
		}



	}
}