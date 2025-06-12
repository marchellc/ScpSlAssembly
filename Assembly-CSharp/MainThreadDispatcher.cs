using System;
using NorthwoodLib;
using UnityEngine;

public class MainThreadDispatcher : MonoBehaviour
{
	public enum DispatchTime
	{
		Update,
		LateUpdate,
		FixedUpdate
	}

	private static readonly ActionDispatcher UpdateDispatcher = new ActionDispatcher();

	private static readonly ActionDispatcher LateUpdateDispatcher = new ActionDispatcher();

	private static readonly ActionDispatcher FixedUpdateDispatcher = new ActionDispatcher();

	public static void Dispatch(Action action, DispatchTime dispatchTime = DispatchTime.Update)
	{
		switch (dispatchTime)
		{
		case DispatchTime.Update:
			MainThreadDispatcher.UpdateDispatcher.Dispatch(action);
			break;
		case DispatchTime.LateUpdate:
			MainThreadDispatcher.LateUpdateDispatcher.Dispatch(action);
			break;
		case DispatchTime.FixedUpdate:
			MainThreadDispatcher.FixedUpdateDispatcher.Dispatch(action);
			break;
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
}
