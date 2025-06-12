using System;
using System.Runtime.InteropServices;
using Footprinting;
using Interactables.Interobjects.DoorButtons;
using Interactables.Interobjects.DoorUtils;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

namespace Interactables.Interobjects;

public class CheckpointDoor : DoorVariant, IDamageableDoor
{
	public enum SequenceState
	{
		Idle,
		Granted,
		OpenLoop,
		ClosingWarning
	}

	[SerializeField]
	private AudioSource _loudSource;

	[SerializeField]
	private AudioSource _beepSource;

	[SerializeField]
	private AudioClip _deniedClip;

	[SerializeField]
	private AudioClip _warningClip;

	[SyncVar(hook = "SequenceHook")]
	private byte _curSequence;

	private bool _prevDestroyed;

	[field: SerializeField]
	public DoorVariant[] SubDoors { get; private set; }

	[field: SerializeField]
	public CheckpointSequenceController SequenceCtrl { get; private set; }

	public SequenceState CurSequence
	{
		get
		{
			return (SequenceState)this._curSequence;
		}
		set
		{
			byte b = (byte)value;
			if (this._curSequence != b)
			{
				this.Network_curSequence = b;
				if (NetworkServer.active)
				{
					this.OnSequenceChanged?.Invoke();
				}
			}
		}
	}

	public float MaxHealth
	{
		get
		{
			float num = 0f;
			int num2 = 0;
			DoorVariant[] subDoors = this.SubDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				if (subDoors[i] is IDamageableDoor damageableDoor)
				{
					num += damageableDoor.MaxHealth;
					num2++;
				}
			}
			return num / (float)num2;
		}
		set
		{
			DoorVariant[] subDoors = this.SubDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				if (subDoors[i] is BreakableDoor breakableDoor)
				{
					breakableDoor.MaxHealth = value;
				}
			}
		}
	}

	public float RemainingHealth
	{
		get
		{
			float num = 0f;
			int num2 = 0;
			DoorVariant[] subDoors = this.SubDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				if (subDoors[i] is IDamageableDoor damageableDoor)
				{
					num += damageableDoor.RemainingHealth;
					num2++;
				}
			}
			return num / (float)num2;
		}
		set
		{
			DoorVariant[] subDoors = this.SubDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				if (subDoors[i] is BreakableDoor breakableDoor)
				{
					breakableDoor.RemainingHealth = value;
				}
			}
		}
	}

	public bool IsDestroyed
	{
		get
		{
			DoorVariant[] subDoors = this.SubDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				if (!(subDoors[i] is IDamageableDoor damageableDoor))
				{
					return false;
				}
				if (!damageableDoor.IsDestroyed)
				{
					return false;
				}
			}
			return true;
		}
		set
		{
			DoorVariant[] subDoors = this.SubDoors;
			for (int i = 0; i < subDoors.Length; i++)
			{
				if (subDoors[i] is IDamageableDoor damageableDoor)
				{
					damageableDoor.IsDestroyed = value;
				}
			}
		}
	}

	public byte Network_curSequence
	{
		get
		{
			return this._curSequence;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._curSequence, 8uL, SequenceHook);
		}
	}

	public event Action OnDestroyedChanged;

	public event Action OnSequenceChanged;

	private void SequenceHook(byte prev, byte cur)
	{
		if (prev != cur && !NetworkServer.active)
		{
			this.OnSequenceChanged?.Invoke();
		}
	}

	protected override void Awake()
	{
		base.Awake();
		this.SequenceCtrl.Init(this);
		ButtonVariant[] buttons = base.Buttons;
		for (int i = 0; i < buttons.Length; i++)
		{
			if (buttons[i] is CheckpointKeycardButton checkpointKeycardButton)
			{
				checkpointKeycardButton.Init(this);
			}
		}
		this.OnSequenceChanged += delegate
		{
			if (this.CurSequence == SequenceState.ClosingWarning)
			{
				this.PlayWarningSound();
			}
		};
	}

	protected override void Update()
	{
		base.Update();
		if (this._prevDestroyed != this.IsDestroyed)
		{
			this.OnDestroyedChanged?.Invoke();
			this._prevDestroyed = !this._prevDestroyed;
		}
		if (NetworkServer.active)
		{
			this.CurSequence = this.SequenceCtrl.UpdateSequence();
		}
	}

	public override bool AllowInteracting(ReferenceHub ply, byte colliderId)
	{
		if (this.IsDestroyed)
		{
			return false;
		}
		if (this.CurSequence != SequenceState.Idle)
		{
			return false;
		}
		DoorVariant[] subDoors = this.SubDoors;
		for (int i = 0; i < subDoors.Length; i++)
		{
			if (!subDoors[i].AllowInteracting(null, colliderId))
			{
				return false;
			}
		}
		return true;
	}

	public override float GetExactState()
	{
		if (this.SubDoors.Length == 0)
		{
			return 0f;
		}
		float num = 0f;
		DoorVariant[] subDoors = this.SubDoors;
		foreach (DoorVariant doorVariant in subDoors)
		{
			num = Mathf.Max(num, doorVariant.GetExactState());
		}
		return num;
	}

	public override bool IsConsideredOpen()
	{
		DoorVariant[] subDoors = this.SubDoors;
		for (int i = 0; i < subDoors.Length; i++)
		{
			if (subDoors[i].IsConsideredOpen())
			{
				return true;
			}
		}
		return false;
	}

	public override bool AnticheatPassageApproved()
	{
		DoorVariant[] subDoors = this.SubDoors;
		for (int i = 0; i < subDoors.Length; i++)
		{
			if (subDoors[i].AnticheatPassageApproved())
			{
				return true;
			}
		}
		return false;
	}

	public void ToggleAllDoors(bool newState)
	{
		DoorVariant[] subDoors = this.SubDoors;
		foreach (DoorVariant doorVariant in subDoors)
		{
			if (!(doorVariant is IDamageableDoor { IsDestroyed: not false }))
			{
				doorVariant.NetworkTargetState = newState;
			}
		}
	}

	public bool ServerDamage(float hp, DoorDamageType type, Footprint attacker = default(Footprint))
	{
		bool flag = false;
		DoorVariant[] subDoors = this.SubDoors;
		for (int i = 0; i < subDoors.Length; i++)
		{
			if (subDoors[i] is IDamageableDoor damageableDoor)
			{
				flag |= damageableDoor.ServerDamage(hp, type, attacker);
			}
		}
		return flag;
	}

	public bool ServerRepair()
	{
		bool flag = false;
		DoorVariant[] subDoors = this.SubDoors;
		for (int i = 0; i < subDoors.Length; i++)
		{
			if (subDoors[i] is IDamageableDoor damageableDoor)
			{
				flag |= damageableDoor.ServerRepair();
			}
		}
		return flag;
	}

	public float GetHealthPercent()
	{
		float num = 1f;
		DoorVariant[] subDoors = this.SubDoors;
		for (int i = 0; i < subDoors.Length; i++)
		{
			if (subDoors[i] is IDamageableDoor damageableDoor)
			{
				num *= damageableDoor.GetHealthPercent();
			}
		}
		return num;
	}

	public void ClientDestroyEffects()
	{
	}

	public void ClientRepairEffects()
	{
	}

	internal override void TargetStateChanged()
	{
		base.TargetStateChanged();
		this._loudSource.Play();
	}

	public override void LockBypassDenied(ReferenceHub ply, byte colliderId)
	{
		this.RpcPlayDeniedBeep();
	}

	public override void PermissionsDenied(ReferenceHub ply, byte colliderId)
	{
		this.RpcPlayDeniedBeep();
		this.PlayDeniedButtonAnims(ply.GetCombinedPermissions(this));
	}

	[ClientRpc]
	public void RpcPlayWarningSound()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void Interactables.Interobjects.CheckpointDoor::RpcPlayWarningSound()", -1870757840, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	public void PlayWarningSound()
	{
		this._beepSource.PlayOneShot(this._warningClip);
	}

	[ClientRpc]
	private void RpcPlayDeniedBeep()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void Interactables.Interobjects.CheckpointDoor::RpcPlayDeniedBeep()", -1557414586, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	[ClientRpc]
	private void PlayDeniedButtonAnims(DoorPermissionFlags perms)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		GeneratedNetworkCode._Write_Interactables_002EInterobjects_002EDoorUtils_002EDoorPermissionFlags(writer, perms);
		this.SendRPCInternal("System.Void Interactables.Interobjects.CheckpointDoor::PlayDeniedButtonAnims(Interactables.Interobjects.DoorUtils.DoorPermissionFlags)", 534590385, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcPlayWarningSound()
	{
		this.PlayWarningSound();
	}

	protected static void InvokeUserCode_RpcPlayWarningSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayWarningSound called on server.");
		}
		else
		{
			((CheckpointDoor)obj).UserCode_RpcPlayWarningSound();
		}
	}

	protected void UserCode_RpcPlayDeniedBeep()
	{
		this._beepSource.PlayOneShot(this._deniedClip);
	}

	protected static void InvokeUserCode_RpcPlayDeniedBeep(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcPlayDeniedBeep called on server.");
		}
		else
		{
			((CheckpointDoor)obj).UserCode_RpcPlayDeniedBeep();
		}
	}

	protected void UserCode_PlayDeniedButtonAnims__DoorPermissionFlags(DoorPermissionFlags perms)
	{
		ButtonVariant[] buttons = base.Buttons;
		for (int i = 0; i < buttons.Length; i++)
		{
			if (buttons[i] is BasicDoorButton basicDoorButton)
			{
				basicDoorButton.TriggerDoorDenied(perms);
			}
		}
	}

	protected static void InvokeUserCode_PlayDeniedButtonAnims__DoorPermissionFlags(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC PlayDeniedButtonAnims called on server.");
		}
		else
		{
			((CheckpointDoor)obj).UserCode_PlayDeniedButtonAnims__DoorPermissionFlags(GeneratedNetworkCode._Read_Interactables_002EInterobjects_002EDoorUtils_002EDoorPermissionFlags(reader));
		}
	}

	static CheckpointDoor()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(CheckpointDoor), "System.Void Interactables.Interobjects.CheckpointDoor::RpcPlayWarningSound()", InvokeUserCode_RpcPlayWarningSound);
		RemoteProcedureCalls.RegisterRpc(typeof(CheckpointDoor), "System.Void Interactables.Interobjects.CheckpointDoor::RpcPlayDeniedBeep()", InvokeUserCode_RpcPlayDeniedBeep);
		RemoteProcedureCalls.RegisterRpc(typeof(CheckpointDoor), "System.Void Interactables.Interobjects.CheckpointDoor::PlayDeniedButtonAnims(Interactables.Interobjects.DoorUtils.DoorPermissionFlags)", InvokeUserCode_PlayDeniedButtonAnims__DoorPermissionFlags);
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			NetworkWriterExtensions.WriteByte(writer, this._curSequence);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 8L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this._curSequence);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this._curSequence, SequenceHook, NetworkReaderExtensions.ReadByte(reader));
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 8L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._curSequence, SequenceHook, NetworkReaderExtensions.ReadByte(reader));
		}
	}
}
