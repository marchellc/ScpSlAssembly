using System;
using System.Collections.Generic;
using UnityEngine;

namespace Interactables
{
	public class InteractableCollider : MonoBehaviour
	{
		protected virtual void Awake()
		{
			IInteractable interactable = this.Target as IInteractable;
			if (interactable != null)
			{
				if (!InteractableCollider.AllInstances.ContainsKey(interactable))
				{
					InteractableCollider.AllInstances[interactable] = new Dictionary<byte, InteractableCollider>();
				}
				InteractableCollider.AllInstances[interactable][this.ColliderId] = this;
				return;
			}
			Debug.LogError("Fatal error: '" + this.Target.name + "' is not IInteractable.");
		}

		public static bool TryGetCollider(IInteractable target, byte colliderId, out InteractableCollider res)
		{
			Dictionary<byte, InteractableCollider> dictionary;
			if (InteractableCollider.AllInstances.TryGetValue(target, out dictionary) && dictionary.TryGetValue(colliderId, out res))
			{
				return true;
			}
			res = null;
			return false;
		}

		protected virtual void OnDestroy()
		{
			IInteractable interactable = this.Target as IInteractable;
			if (interactable == null)
			{
				return;
			}
			Dictionary<byte, InteractableCollider> dictionary;
			if (!InteractableCollider.AllInstances.TryGetValue(interactable, out dictionary))
			{
				return;
			}
			dictionary.Remove(this.ColliderId);
			if (dictionary.Count == 0)
			{
				InteractableCollider.AllInstances.Remove(interactable);
			}
		}

		public MonoBehaviour Target;

		public byte ColliderId;

		public Vector3 VerificationOffset;

		public static readonly Dictionary<IInteractable, Dictionary<byte, InteractableCollider>> AllInstances = new Dictionary<IInteractable, Dictionary<byte, InteractableCollider>>();
	}
}
