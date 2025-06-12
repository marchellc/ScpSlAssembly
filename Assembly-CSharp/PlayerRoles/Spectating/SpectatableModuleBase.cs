using System;
using System.Collections.Generic;
using GameObjectPools;
using UnityEngine;

namespace PlayerRoles.Spectating;

public abstract class SpectatableModuleBase : MonoBehaviour, IPoolSpawnable, IPoolResettable
{
	public delegate void Added(SpectatableModuleBase module);

	public delegate void Removed(SpectatableModuleBase module);

	public static readonly HashSet<SpectatableModuleBase> AllInstances = new HashSet<SpectatableModuleBase>();

	public SpectatableListElementType ListElementType;

	private PlayerRoleBase _cachedRole;

	private bool _roleCacheSet;

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
			if (!this.MainRole.TryGetOwner(out var hub))
			{
				throw new InvalidOperationException("SpectatableModuleBase of name '" + base.name + "' does not have an owner!");
			}
			return hub;
		}
	}

	public static event Added OnAdded;

	public static event Removed OnRemoved;

	public virtual void ResetObject()
	{
		SpectatableModuleBase.AllInstances.Remove(this);
		SpectatableModuleBase.OnRemoved?.Invoke(this);
	}

	public virtual void SpawnObject()
	{
		SpectatableModuleBase.AllInstances.Add(this);
		SpectatableModuleBase.OnAdded?.Invoke(this);
	}

	internal virtual void OnBeganSpectating()
	{
	}

	internal virtual void OnStoppedSpectating()
	{
	}
}
