using System.Collections.Generic;
using MapGeneration.StaticHelpers;
using UnityEngine;

namespace Interactables;

public class InteractableCollider : MonoBehaviour, IBlockStaticBatching
{
	public MonoBehaviour Target;

	public byte ColliderId;

	public Vector3 VerificationOffset;

	public static readonly Dictionary<IInteractable, Dictionary<byte, InteractableCollider>> AllInstances = new Dictionary<IInteractable, Dictionary<byte, InteractableCollider>>();

	protected virtual void Awake()
	{
		if (Target is IInteractable key)
		{
			if (!AllInstances.ContainsKey(key))
			{
				AllInstances[key] = new Dictionary<byte, InteractableCollider>();
			}
			AllInstances[key][ColliderId] = this;
		}
		else
		{
			Debug.LogError("Fatal error: '" + Target.name + "' is not IInteractable.");
		}
	}

	public static bool TryGetCollider(IInteractable target, byte colliderId, out InteractableCollider res)
	{
		if (AllInstances.TryGetValue(target, out var value) && value.TryGetValue(colliderId, out res))
		{
			return true;
		}
		res = null;
		return false;
	}

	protected virtual void OnDestroy()
	{
		if (Target is IInteractable key && AllInstances.TryGetValue(key, out var value))
		{
			value.Remove(ColliderId);
			if (value.Count == 0)
			{
				AllInstances.Remove(key);
			}
		}
	}
}
