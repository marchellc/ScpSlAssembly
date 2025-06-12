using System;
using System.Runtime.InteropServices;
using Footprinting;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using UnityEngine;

namespace Interactables.Interobjects;

public class BreakableDoor : BasicDoor, IDamageableDoor, INonInteractableDoor, IScp106PassableDoor
{
	[SyncVar]
	private bool _destroyed;

	private bool _prevDestroyed;

	[Header("Breakable Door Settings")]
	[SerializeField]
	private float _maxHealth = 80f;

	[SerializeField]
	private BrokenDoor _brokenPrefab;

	[SerializeField]
	private DoorDamageType _ignoredDamageSources;

	[SerializeField]
	private GameObject _objectToReplace;

	[SerializeField]
	private bool _nonInteractable;

	[SerializeField]
	[SyncVar]
	private bool _restrict106WhileLocked;

	public float RemainingHealth { get; set; }

	public float MaxHealth
	{
		get
		{
			return this._maxHealth;
		}
		set
		{
			this._maxHealth = value;
		}
	}

	public DoorDamageType IgnoredDamageSources
	{
		get
		{
			return this._ignoredDamageSources;
		}
		set
		{
			this._ignoredDamageSources = value;
		}
	}

	public bool IsDestroyed
	{
		get
		{
			return this._destroyed;
		}
		set
		{
			if (value != this._destroyed)
			{
				if (value)
				{
					this.ServerDamage(this._maxHealth, DoorDamageType.ServerCommand);
				}
				else
				{
					this.ServerRepair();
				}
			}
		}
	}

	public bool IgnoreLockdowns => this._nonInteractable;

	public bool IgnoreRemoteAdmin => this._nonInteractable;

	public bool IsScp106Passable
	{
		get
		{
			if (this._restrict106WhileLocked && base.ActiveLocks != 0)
			{
				return base.TargetState;
			}
			return true;
		}
		set
		{
			this.Network_restrict106WhileLocked = !value;
		}
	}

	public bool Network_destroyed
	{
		get
		{
			return this._destroyed;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._destroyed, 8uL, null);
		}
	}

	public bool Network_restrict106WhileLocked
	{
		get
		{
			return this._restrict106WhileLocked;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._restrict106WhileLocked, 16uL, null);
		}
	}

	public event Action OnDestroyedChanged;

	[Server]
	public bool ServerDamage(float hp, DoorDamageType type, Footprint attacker = default(Footprint))
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean Interactables.Interobjects.BreakableDoor::ServerDamage(System.Single,Interactables.Interobjects.DoorUtils.DoorDamageType,Footprinting.Footprint)' called when server was not active");
			return default(bool);
		}
		if (this._destroyed)
		{
			return false;
		}
		if (this._ignoredDamageSources.HasFlagFast(type))
		{
			return false;
		}
		if (this._brokenPrefab == null || this._objectToReplace == null)
		{
			return false;
		}
		this.RemainingHealth -= hp;
		if (this.RemainingHealth <= 0f)
		{
			this.Network_destroyed = true;
			DoorEvents.TriggerAction(this, DoorAction.Destroyed, null);
			if (!attacker.IsSet)
			{
				return true;
			}
			ServerLogs.AddLog(ServerLogs.Modules.Door, string.Format("{0} destroyed {1} door using {2}.", attacker.LoggedNameFromFootprint(), base.DoorName ?? "unnamed", type), ServerLogs.ServerLogType.GameEvent);
		}
		return true;
	}

	[Server]
	public bool ServerRepair()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean Interactables.Interobjects.BreakableDoor::ServerRepair()' called when server was not active");
			return default(bool);
		}
		if (!this._destroyed)
		{
			return false;
		}
		this.Network_destroyed = false;
		this.RemainingHealth = this._maxHealth;
		return true;
	}

	public override float GetExactState()
	{
		if (!this._destroyed)
		{
			return base.GetExactState();
		}
		return 1f;
	}

	public override bool AllowInteracting(ReferenceHub ply, byte colliderId)
	{
		if (!this._destroyed)
		{
			return base.AllowInteracting(ply, colliderId);
		}
		return false;
	}

	internal override void TargetStateChanged()
	{
		if (!this._destroyed)
		{
			base.TargetStateChanged();
		}
	}

	protected override void Update()
	{
		base.Update();
		if (!this._prevDestroyed && this._destroyed)
		{
			this._prevDestroyed = true;
			this.ClientDestroyEffects();
			this.OnDestroyedChanged?.Invoke();
		}
		if (this._prevDestroyed && !this._destroyed)
		{
			this._prevDestroyed = false;
			this.ClientRepairEffects();
			this.OnDestroyedChanged?.Invoke();
		}
	}

	protected override void Awake()
	{
		base.Awake();
		this.RemainingHealth = this._maxHealth;
	}

	public void ClientDestroyEffects()
	{
		this._objectToReplace.SetActive(value: false);
	}

	public void ClientRepairEffects()
	{
		this._objectToReplace.SetActive(value: true);
	}

	public float GetHealthPercent()
	{
		return Mathf.Clamp01(this.RemainingHealth / this._maxHealth);
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
			writer.WriteBool(this._destroyed);
			writer.WriteBool(this._restrict106WhileLocked);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			writer.WriteBool(this._destroyed);
		}
		if ((base.syncVarDirtyBits & 0x10L) != 0L)
		{
			writer.WriteBool(this._restrict106WhileLocked);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._destroyed, null, reader.ReadBool());
			base.GeneratedSyncVarDeserialize(ref this._restrict106WhileLocked, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._destroyed, null, reader.ReadBool());
		}
		if ((num & 0x10L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._restrict106WhileLocked, null, reader.ReadBool());
		}
	}
}
