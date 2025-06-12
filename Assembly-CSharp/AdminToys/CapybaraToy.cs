using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Mirror;
using NorthwoodLib.Pools;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace AdminToys;

public class CapybaraToy : AdminToyBase
{
	[SyncVar(hook = "SetCollisionsEnabled")]
	private bool _collisionsEnabled;

	private List<Collider> _colliders;

	public override string CommandName => "Capybara";

	public bool CollisionsEnabled
	{
		get
		{
			return this._collisionsEnabled;
		}
		set
		{
			this.SetCollidersState(value);
			this.Network_collisionsEnabled = value;
		}
	}

	public bool Network_collisionsEnabled
	{
		get
		{
			return this._collisionsEnabled;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._collisionsEnabled, 32uL, SetCollisionsEnabled);
		}
	}

	public override void OnSpawned(ReferenceHub admin, ArraySegment<string> arguments)
	{
		base.OnSpawned(admin, arguments);
		Vector3 position;
		Quaternion rotation;
		if (admin.roleManager.CurrentRole is IFpcRole fpcRole)
		{
			position = ((!Physics.Raycast(fpcRole.FpcModule.Position, Vector3.down, out var hitInfo, 2f)) ? fpcRole.FpcModule.Position : hitInfo.point);
			rotation = Quaternion.Euler(0f, fpcRole.FpcModule.MouseLook.CurrentHorizontal, 0f);
		}
		else
		{
			admin.transform.GetPositionAndRotation(out position, out rotation);
		}
		base.transform.SetPositionAndRotation(position, Quaternion.Euler(0f, rotation.eulerAngles.y, 0f));
		base.transform.localScale = Vector3.one;
	}

	private void SetCollidersState(bool active)
	{
		foreach (Collider collider in this._colliders)
		{
			collider.enabled = active;
		}
	}

	private void SetCollisionsEnabled(bool oldValue, bool newValue)
	{
		this.SetCollidersState(newValue);
	}

	private void Awake()
	{
		this._colliders = ListPool<Collider>.Shared.Rent();
		base.GetComponentsInChildren(includeInactive: true, this._colliders);
	}

	private new void OnDestroy()
	{
		ListPool<Collider>.Shared.Return(this._colliders);
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this._collisionsEnabled);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 0x20L) != 0L)
		{
			writer.WriteBool(this._collisionsEnabled);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._collisionsEnabled, SetCollisionsEnabled, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 0x20L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._collisionsEnabled, SetCollisionsEnabled, reader.ReadBool());
		}
	}
}
