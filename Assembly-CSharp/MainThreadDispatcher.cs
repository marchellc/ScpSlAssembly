using System;
using NorthwoodLib;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
	public static void Dispatch(Action action, MainThreadDispatcher.DispatchTime dispatchTime = MainThreadDispatcher.DispatchTime.Update)
	{
		switch (dispatchTime)
		{
		case MainThreadDispatcher.DispatchTime.Update:
			MainThreadDispatcher.UpdateDispatcher.Dispatch(action);
			return;
		case MainThreadDispatcher.DispatchTime.LateUpdate:
			MainThreadDispatcher.LateUpdateDispatcher.Dispatch(action);
			return;
		case MainThreadDispatcher.DispatchTime.FixedUpdate:
			MainThreadDispatcher.FixedUpdateDispatcher.Dispatch(action);
			return;
		default:
			return;
		}
	}

	private void Update()
	{
		MainThreadDispatcher.UpdateDispatcher.Invoke();
	}

	private void LateUpdate()
	{
		MainThreadDispatcher.LateUpdateDispatcher.Invoke();
	}

	private void FixedUpdate()
	{
		MainThreadDispatcher.FixedUpdateDispatcher.Invoke();
	}

	private static readonly ActionDispatcher UpdateDispatcher = new ActionDispatcher();

	private static readonly ActionDispatcher LateUpdateDispatcher = new ActionDispatcher();

	private static readonly ActionDispatcher FixedUpdateDispatcher = new ActionDispatcher();

	public enum DispatchTime
	{
		Update,
		LateUpdate,
		FixedUpdate
	}
}
