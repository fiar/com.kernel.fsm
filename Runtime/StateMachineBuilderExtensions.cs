using UnityEngine;
using System.Collections;

namespace Kernel.FSM
{
	public static class StateMachineBuilderExtensions
	{

		public static IStateBuilder<T, TParent> Concrete<T, TParent>(this IStateBuilder<T, TParent> builder, object concreteState)
			where T : AbstractState, new()
		{
			if (concreteState == null) return builder;
			
			var info = ConcreteStateDeclarations.ResolveStateInfo(concreteState.GetType());
			bool isEntered = false;

			return builder
				///
				/// Awake State
				.Awake(state =>
				{
					if (info.Awake != null && concreteState != null)
					{
						info.Awake.Invoke(concreteState, null);
					}
				})
				///
				/// Destroy State
				.Destroy(state =>
				{
					if (info.OnDestroy != null && concreteState != null)
					{
						info.OnDestroy.Invoke(concreteState, null);
					}
				})
				///
				/// Enter State
				.Enter(state =>
				{
					isEntered = true;
					if (info.OnEnter != null && concreteState != null)
					{
						info.OnEnter.Invoke(concreteState, null);
					}
				})
				///
				/// Exit State
				.Exit(state =>
				{
					if (isEntered)
					{
						isEntered = false;
						if (info.OnExit != null && concreteState != null)
						{
							info.OnExit.Invoke(concreteState, null);
						}
					}
				})
				///
				/// Update State
				.Update((state, deltaTime) =>
				{
					if (info.Update != null && concreteState != null)
					{
						info.Update.Invoke(concreteState, null);
					}
				});
		}
	}
}
