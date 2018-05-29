using System;
using UnityEngine;
using System.Collections;
using System.Reflection;

namespace Kernel.FSM
{
	public static class ConcreteStateDeclarations
	{
		public class ConcreteStateInfo
		{
			public MethodInfo Awake { get; set; }
			public MethodInfo OnDestroy { get; set; }
			public MethodInfo OnEnter { get; set; }
			public MethodInfo OnExit { get; set; }
			public MethodInfo Update { get; set; }
		}

		public static ConcreteStateInfo ResolveStateInfo(Type type)
		{
			var flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

			return new ConcreteStateInfo
			{
				Awake = type.GetMethod("Awake", flags),
				OnDestroy = type.GetMethod("OnDestroy", flags),
				OnEnter = type.GetMethod("OnEnter", flags),
				OnExit = type.GetMethod("OnExit", flags),
				Update = type.GetMethod("Update", flags)
			};
		}
	}
}
