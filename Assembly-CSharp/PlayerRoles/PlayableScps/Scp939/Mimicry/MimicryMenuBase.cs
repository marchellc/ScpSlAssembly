using System;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public abstract class MimicryMenuBase : MonoBehaviour
	{
		protected virtual void Awake()
		{
			ReferenceHub referenceHub;
			if (ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				Scp939Role scp939Role = referenceHub.roleManager.CurrentRole as Scp939Role;
				if (scp939Role != null)
				{
					this.Setup(scp939Role);
					return;
				}
			}
			base.gameObject.SetActive(false);
		}

		protected virtual void Setup(Scp939Role role)
		{
		}
	}
}
