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
		if (this.Target is IInteractable key)
		{
			if (!InteractableCollider.AllInstances.ContainsKey(key))
			{
				InteractableCollider.AllInstances[key] = new Dictionary<byte, InteractableCollider>();
			}
			InteractableCollider.AllInstances[key][this.ColliderId] = this;
		}
		else
		{
			Debug.LogError("Fatal error: '" + this.Target.name + "' is not IInteractable.", base.gameObject);
		}
	}

	public static bool TryGetCollider(IInteractable target, byte colliderId, out InteractableCollider res)
	{
		if (InteractableCollider.AllInstances.TryGetValue(target, out var value) && value.TryGetValue(colliderId, out res))
		{
			return true;
		}
		res = null;
		return false;
	}

	protected virtual void OnDestroy()
	{
		if (this.Target is IInteractable key && InteractableCollider.AllInstances.TryGetValue(key, out var value))
		{
			value.Remove(this.ColliderId);
			if (value.Count == 0)
			{
				InteractableCollider.AllInstances.Remove(key);
			}
		}
	}
}
