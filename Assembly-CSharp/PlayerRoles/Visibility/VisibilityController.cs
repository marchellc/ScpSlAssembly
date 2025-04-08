using System;
using GameObjectPools;
using UnityEngine;

namespace PlayerRoles.Visibility
{
	public class VisibilityController : MonoBehaviour, IPoolSpawnable
	{
		public virtual InvisibilityFlags IgnoredFlags
		{
			get
			{
				return InvisibilityFlags.None;
			}
		}

		private protected ReferenceHub Owner { protected get; private set; }

		private protected PlayerRoleBase Role { protected get; private set; }

		public virtual InvisibilityFlags GetActiveFlags(ReferenceHub observer)
		{
			return InvisibilityFlags.None;
		}

		public virtual bool ValidateVisibility(ReferenceHub hub)
		{
			ICustomVisibilityRole customVisibilityRole = hub.roleManager.CurrentRole as ICustomVisibilityRole;
			return customVisibilityRole == null || (customVisibilityRole.VisibilityController.GetActiveFlags(this.Owner) & ~this.IgnoredFlags) == InvisibilityFlags.None;
		}

		public virtual void SpawnObject()
		{
			this.Role = base.GetComponentInParent<PlayerRoleBase>();
			if (this.Role == null)
			{
				throw new InvalidOperationException("VisibilityController " + base.name + " does not have a parent role set!");
			}
			ReferenceHub referenceHub;
			if (!this.Role.TryGetOwner(out referenceHub))
			{
				throw new InvalidOperationException("VisibilityController " + base.name + " does not have an owner assigned!");
			}
			this.Owner = referenceHub;
		}
	}
}
