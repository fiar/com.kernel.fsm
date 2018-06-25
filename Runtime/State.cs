using System;
using System.Collections.Generic;

namespace Kernel.FSM
{
	/// <summary>
	/// Non-generic state interface.
	/// </summary>
	public interface IState
	{
		/// <summary>
		/// Root state.
		/// </summary>
		IState Root { get; set; }

		/// <summary>
		/// Parent state, or null if this is the root level state.
		/// </summary>
		IState Parent { get; set; }

		/// <summary>
		/// Change to the state with the specified name.
		/// </summary>
		void ChangeState(string stateName);

		/// <summary>
		/// Push another state above the current one, so that popping it will return to the
		/// current state.
		/// </summary>
		void PushState(string stateName);

		/// <summary>
		/// Exit out of the current state and enter whatever state is below it in the stack.
		/// </summary>
		void PopState();

		/// <summary>
		/// Update this state and its children with a specified delta time.
		/// </summary>
		void Update(float deltaTime);

		/// <summary>
		/// Triggered when we awake the state.
		/// </summary>
		void Awake();

		/// <summary>
		/// Triggered when we enter the state.
		/// </summary>
		void Enter();

		/// <summary>
		/// Triggered when we exit the state.
		/// </summary>
		void Exit();

		/// <summary>
		/// Triggered when we destroy the state.
		/// </summary>
		void Destroy();

		/// <summary>
		/// Trigger an event on this state or one of its children.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		bool TriggerEvent(string name);

		/// <summary>
		/// Triggered when and event occurs. Executes the event's action if the 
		/// current state is at the top of the stack, otherwise triggers it on 
		/// the next state down.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		/// <param name="eventArgs">Arguments to send to the event</param>
		bool TriggerEvent(string name, EventArgs eventArgs);

		/// <summary>
		/// Trigger an event on this state and upwards.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		bool TriggerEventUpwards(string name);

		/// <summary>
		/// Triggered when and event occurs. Executes the event's action if the 
		/// current state is at the top of the stack.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		/// <param name="eventArgs">Arguments to send to the event</param>
		bool TriggerEventUpwards(string name, EventArgs eventArgs);

		/// <summary>
		/// Trigger an event on this state and upwards to all states.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		bool BroadcastEvent(string name);

		/// <summary>
		/// Triggered when and event occurs. Executes the event's action if the 
		/// current state is at the top of the stack.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		/// <param name="eventArgs">Arguments to send to the event</param>
		bool BroadcastEvent(string name, EventArgs eventArgs);
	}

	/// <summary>
	/// State with a specified handler type.
	/// </summary>
	public abstract class AbstractState : IState
	{
		/// <summary>
		/// Action called when we awake the state.
		/// </summary>
		private Action onAwake;

		/// <summary>
		/// Action called when we enter the state.
		/// </summary>
		private Action onEnter;

		/// <summary>
		/// Action called when the state gets updated.
		/// </summary>
		private Action<float> onUpdate;

		/// <summary>
		/// Action called when we exit the state.
		/// </summary>
		private Action onExit;

		/// <summary>
		/// Action called when we destroy the state.
		/// </summary>
		private Action onDestroy;

		private bool isEntered;

		private readonly IList<Condition> conditions = new List<Condition>();

		/// <summary>
		/// Root state.
		/// </summary>
		public IState Root { get; set; }

		/// <summary>
		/// Parent state, or null if this is the root level state.
		/// </summary>
		public IState Parent { get; set; }

		/// <summary>
		/// Stack of active child states.
		/// </summary>
		private readonly Stack<IState> activeChildren = new Stack<IState>();

		/// <summary>
		/// Dictionary of all children (active and inactive), and their names.
		/// </summary>
		private readonly IDictionary<string, IState> children = new Dictionary<string, IState>();

		/// <summary>
		/// Dictionary of all actions associated with this state.
		/// </summary>
		private readonly IDictionary<string, Action<EventArgs>> events = new Dictionary<string, Action<EventArgs>>();

		/// <summary>
		/// Pops the current state from the stack and pushes the specified one on.
		/// </summary>
		public void ChangeState(string stateName)
		{
			// Try to find the specified state.
			IState newState;
			if (!children.TryGetValue(stateName, out newState))
			{
				throw new ApplicationException("Tried to change to state \"" + stateName + "\", but it is not in the list of children.");
			}

			// Exit and pop the current state
			if (activeChildren.Count > 0)
			{
				activeChildren.Pop().Exit();
			}

			// Activate the new state
			activeChildren.Push(newState);
			newState.Enter();
		}

		/// <summary>
		/// Push another state from the existing dictionary of children to the top of the state stack.
		/// </summary>
		public void PushState(string stateName)
		{
			// Find the new state and add it
			IState newState;
			if (!children.TryGetValue(stateName, out newState))
			{
				throw new ApplicationException("Tried to change to state \"" + stateName + "\", but it is not in the list of children.");
			}
			activeChildren.Push(newState);
			newState.Enter();
		}

		/// <summary>
		/// Remove the current state from the active state stack and activate the state immediately beneath it.
		/// </summary>
		public void PopState()
		{
			// Exit and pop the current state
			if (activeChildren.Count > 0)
			{
				activeChildren.Pop().Exit();
			}
			else
			{
				throw new ApplicationException("PopState called on state with no active children to pop.");
			}
		}

		/// <summary>
		/// Update this state and its children with a specified delta time.
		/// </summary>
		public void Update(float deltaTime)
		{
			// Only update the child at the end of the tree
			if (activeChildren.Count > 0)
			{
				activeChildren.Peek().Update(deltaTime);
				return;
			}

			if (onUpdate != null)
			{
				onUpdate(deltaTime);
			}

			// Update conditions
			for (var i = 0; i < conditions.Count; i++)
			{
				if (conditions[i].Predicate())
				{
					conditions[i].Action();
				}
			}
		}

		/// <summary>
		/// Create a new state as a child of the current state.
		/// </summary>
		public void AddChild(IState newState, string stateName)
		{
			try
			{
				children.Add(stateName, newState);
				newState.Parent = this;
				newState.Root = Root;
			}
			catch (ArgumentException)
			{
				throw new ApplicationException("State with name \"" + stateName + "\" already exists in list of children.");
			}
		}

		/// <summary>
		/// Create a new state as a child of the current state and automatically derive 
		/// its name from its handler type.
		/// </summary>
		public void AddChild(IState newState)
		{
			var name = newState.GetType().Name;
			AddChild(newState, name);
		}

		/// <summary>
		/// Data structure for associating a condition with an action.
		/// </summary>
		private struct Condition
		{
			public Func<bool> Predicate;
			public Action Action;
		}

		/// <summary>
		/// Set an action to be called when the state is updated an a specified 
		/// predicate is true.
		/// </summary>
		public void SetCondition(Func<bool> predicate, Action action)
		{
			conditions.Add(new Condition()
			{
				Predicate = predicate,
				Action = action
			});
		}


		/// <summary>
		/// Action triggered on awake the state.
		/// </summary>
		public void SetAwakeAction(Action onAwake)
		{
			this.onAwake += onAwake;
		}

		/// <summary>
		/// Action triggered on entering the state.
		/// </summary>
		public void SetEnterAction(Action onEnter)
		{
			this.onEnter += onEnter;
		}

		/// <summary>
		/// Action triggered on exiting the state.
		/// </summary>
		public void SetExitAction(Action onExit)
		{
			this.onExit += onExit;
		}

		/// <summary>
		/// Action triggered on destroying the state.
		/// </summary>
		public void SetDestroyAction(Action onDestroy)
		{
			this.onDestroy += onDestroy;
		}

		/// <summary>
		/// Action which passes the current state object and the delta time since the 
		/// last update to a function.
		/// </summary>
		public void SetUpdateAction(Action<float> onUpdate)
		{
			this.onUpdate += onUpdate;
		}

		/// <summary>
		/// Sets an action to be associated with an identifier that can later be used
		/// to trigger it.
		/// Convenience method that uses default event args intended for events that 
		/// don't need any arguments.
		/// </summary>
		public void SetEvent(string identifier, Action<EventArgs> eventTriggeredAction)
		{
			SetEvent<EventArgs>(identifier, eventTriggeredAction);
		}

		/// <summary>
		/// Sets an action to be associated with an identifier that can later be used
		/// to trigger it.
		/// </summary>
		public void SetEvent<TEvent>(string identifier, Action<TEvent> eventTriggeredAction)
			where TEvent : EventArgs
		{
			events.Add(identifier, args => eventTriggeredAction(CheckEventArgs<TEvent>(identifier, args)));
		}

		/// <summary>
		/// Cast the specified EventArgs to a specified type, throwing a descriptive exception if this fails.
		/// </summary>
		private static TEvent CheckEventArgs<TEvent>(string identifier, EventArgs args)
			where TEvent : EventArgs
		{
			try
			{
				return (TEvent)args;
			}
			catch (InvalidCastException ex)
			{
				throw new ApplicationException("Could not invoke event \"" + identifier + "\" with argument of type " +
					args.GetType().Name + ". Expected " + typeof(TEvent).Name, ex);
			}
		}

		/// <summary>
		/// Triggered when we awake the state.
		/// </summary>
		public void Awake()
		{
			if (onAwake != null)
			{
				onAwake();
			}

			foreach (var kvp in children)
			{
				kvp.Value.Awake();
			}
		}

		/// <summary>
		/// Triggered when we enter the state.
		/// </summary>
		public void Enter()
		{
			isEntered = true;

			if (onEnter != null)
			{
				onEnter();
			}
		}

		/// <summary>
		/// Triggered when we exit the state.
		/// </summary>
		public void Exit()
		{
			if (isEntered)
			{
				isEntered = false;

				while (activeChildren.Count > 0)
				{
					activeChildren.Pop().Exit();
				}

				if (onExit != null)
				{
					onExit();
				}
			}
		}

		/// <summary>
		/// Triggered when we destroy the state.
		/// </summary>
		public void Destroy()
		{
			foreach (var kvp in children)
			{
				kvp.Value.Exit();
				kvp.Value.Destroy();
			}
			children.Clear();

			if (onDestroy != null)
			{
				onDestroy();
			}
		}

		/// <summary>
		/// Triggered when and event occurs. Executes the event's action if the 
		/// current state is at the top of the stack, otherwise triggers it on 
		/// the next state down.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		public bool TriggerEvent(string name)
		{
			return TriggerEvent(name, EventArgs.Empty);
		}

		/// <summary>
		/// Triggered when and event occurs. Executes the event's action if the 
		/// current state is at the top of the stack, otherwise triggers it on 
		/// the next state down.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		/// <param name="eventArgs">Arguments to send to the event</param>
		public bool TriggerEvent(string name, EventArgs eventArgs)
		{
			// Only update the child at the end of the tree
			if (activeChildren.Count > 0)
			{
				return activeChildren.Peek().TriggerEvent(name, eventArgs);
			}

			Action<EventArgs> myEvent;
			if (events.TryGetValue(name, out myEvent))
			{
				myEvent(eventArgs);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Triggered when and event occurs. Executes the event's action if the 
		/// current state is at the top of the stack.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		public bool TriggerEventUpwards(string name)
		{
			return TriggerEventUpwards(name, EventArgs.Empty);
		}

		/// <summary>
		/// Triggered when and event occurs. Executes the event's action if the 
		/// current state is at the top of the stack.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		/// <param name="eventArgs">Arguments to send to the event</param>
		public bool TriggerEventUpwards(string name, EventArgs eventArgs)
		{
			// Only update the child at the end of the tree
			if (activeChildren.Count > 0)
			{
				if (activeChildren.Peek().TriggerEventUpwards(name, eventArgs)) return true;
			}

			Action<EventArgs> myEvent;
			if (events.TryGetValue(name, out myEvent))
			{
				myEvent(eventArgs);
				return true;
			}
			return false;
		}

		/// <summary>
		/// Triggered when and event occurs. Executes the event's action if the 
		/// current state is at the top of the stack.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		public bool BroadcastEvent(string name)
		{
			return BroadcastEvent(name, EventArgs.Empty);
		}

		/// <summary>
		/// Triggered when and event occurs. Executes the event's action if the 
		/// current state is at the top of the stack.
		/// </summary>
		/// <param name="name">Name of the event to trigger</param>
		/// <param name="eventArgs">Arguments to send to the event</param>
		public bool BroadcastEvent(string name, EventArgs eventArgs)
		{
			// Only update the child at the end of the tree
			if (activeChildren.Count > 0)
			{
				activeChildren.Peek().BroadcastEvent(name, eventArgs);
			}

			Action<EventArgs> myEvent;
			if (events.TryGetValue(name, out myEvent))
			{
				myEvent(eventArgs);
				return true;
			}
			return false;
		}
	}

	/// <summary>
	/// State with no extra functionality used for root of state hierarchy.
	/// </summary>
	public class State : AbstractState
	{
	}
}
