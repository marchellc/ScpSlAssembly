using System;
using System.Collections.Generic;
using GameObjectPools;
using UnityEngine;

namespace PlayerRoles.Spectating
{
	public abstract class SpectatableModuleBase : MonoBehaviour, IPoolSpawnable, IPoolResettable
	{
		public static event SpectatableModuleBase.Added OnAdded;

		public static event SpectatableModuleBase.Removed OnRemoved;

		public abstract Vector3 CameraPosition { get; }

		public abstract Vector3 CameraRotation { get; }

		public PlayerRoleBase MainRole
		{
			get
			{
				if (!this._roleCacheSet)
				{
					if (!base.TryGetComponent<PlayerRoleBase>(out this._cachedRole))
					{
						throw new InvalidOperationException("SpectatableModuleBase of name '" + base.name + "' is not assigned to any role!");
					}
					this._roleCacheSet = true;
				}
				return this._cachedRole;
			}
		}

		public ReferenceHub TargetHub
		{
			get
			{
				ReferenceHub referenceHub;
				if (!this.MainRole.TryGetOwner(out referenceHub))
				{
					throw new InvalidOperationException("SpectatableModuleBase of name '" + base.name + "' does not have an owner!");
				}
				return referenceHub;
			}
		}

		public virtual void ResetObject()
		{
			SpectatableModuleBase.AllInstances.Remove(this);
			SpectatableModuleBase.Removed onRemoved = SpectatableModuleBase.OnRemoved;
			if (onRemoved == null)
			{
				return;
			}
			onRemoved(this);
		}

		public virtual void SpawnObject()
		{
			SpectatableModuleBase.AllInstances.Add(this);
			SpectatableModuleBase.Added onAdded = SpectatableModuleBase.OnAdded;
			if (onAdded == null)
			{
				return;
			}
			onAdded(this);
		}

		internal virtual void OnBeganSpectating()
		{
		}

		internal virtual void OnStoppedSpectating()
		{
		}

		public static readonly HashSet<SpectatableModuleBase> AllInstances = new HashSet<SpectatableModuleBase>();

		public SpectatableListElementType ListElementType;

		private PlayerRoleBase _cachedRole;

		private bool _roleCacheSet;

		public delegate void Added(SpectatableModuleBase module);

		public delegate void Removed(SpectatableModuleBase module);
	}
}
