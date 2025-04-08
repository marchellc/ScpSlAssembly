using System;
using Mirror;
using UnityEngine;

public class StaticUnityMethods : MonoBehaviour
{
	public static event Action OnUpdate;

	public static event Action OnLateUpdate;

	public static event Action OnFixedUpdate;

	public static bool IsPlaying
	{
		get
		{
			ReferenceHub referenceHub;
			return ReferenceHub.TryGetLocalHub(out referenceHub) && (NetworkClient.active || NetworkServer.active);
		}
	}

	private void Update()
	{
		Action onUpdate = StaticUnityMethods.OnUpdate;
		if (onUpdate == null)
		{
			return;
		}
		onUpdate();
	}

	private void FixedUpdate()
	{
		Action onFixedUpdate = StaticUnityMethods.OnFixedUpdate;
		if (onFixedUpdate == null)
		{
			return;
		}
		onFixedUpdate();
	}

	private void LateUpdate()
	{
		Action onLateUpdate = StaticUnityMethods.OnLateUpdate;
		if (onLateUpdate == null)
		{
			return;
		}
		onLateUpdate();
	}
}
