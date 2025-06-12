using System.Runtime.InteropServices;
using Interactables;
using Interactables.Interobjects.DoorUtils;
using Interactables.Verification;
using Mirror;
using Mirror.RemoteCalls;
using UnityEngine;

public class AlphaWarheadNukesitePanel : NetworkBehaviour, IServerInteractable, IInteractable
{
	private enum DiodeType
	{
		InProgress,
		BlastDoor,
		OutsideDoor
	}

	private enum PanelColliderId
	{
		Cancel = 1,
		Lever
	}

	public Transform lever;

	[SerializeField]
	private MeshRenderer _ledRenderer;

	[SerializeField]
	private Material _onMat;

	[SerializeField]
	private Material _offMat;

	[SerializeField]
	private Quaternion _enabledLeverRotation;

	[SerializeField]
	private Quaternion _disabledLeverRotation;

	[SyncVar]
	public new bool enabled;

	private float _leverStatus;

	private Material[] _matSet;

	private bool[] _prevMats;

	private bool _anyModified;

	private const string OutsideDoorName = "SURFACE_NUKE";

	public static AlphaWarheadNukesitePanel Singleton;

	private bool _doorFound;

	private DoorNametagExtension _outsideDoor;

	public bool AnyBlastdoorClosed
	{
		get
		{
			foreach (BlastDoor instance in BlastDoor.Instances)
			{
				if (!instance.IsOpen)
				{
					return true;
				}
			}
			return false;
		}
	}

	private bool OutsideDoorOpen
	{
		get
		{
			if (!this._doorFound)
			{
				this._doorFound = DoorNametagExtension.NamedDoors.TryGetValue("SURFACE_NUKE", out this._outsideDoor);
			}
			if (this._doorFound)
			{
				return this._outsideDoor.TargetDoor.TargetState;
			}
			return false;
		}
	}

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public bool Networkenabled
	{
		get
		{
			return this.enabled;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.enabled, 1uL, null);
		}
	}

	private void Awake()
	{
		AlphaWarheadNukesitePanel.Singleton = this;
		int num = this._ledRenderer.sharedMaterials.Length;
		this._matSet = new Material[num];
		this._prevMats = new bool[num];
		for (int i = 0; i < num; i++)
		{
			this._matSet[i] = this._offMat;
		}
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		switch ((PanelColliderId)colliderId)
		{
		case PanelColliderId.Cancel:
			AlphaWarheadController.Singleton.CancelDetonation(ply);
			ServerLogs.AddLog(ServerLogs.Modules.Warhead, ply.LoggedNameFromRefHub() + " cancelled the Alpha Warhead detonation.", ServerLogs.ServerLogType.GameEvent);
			break;
		case PanelColliderId.Lever:
			if (!(this._leverStatus > 0f) || !(this._leverStatus < 1f))
			{
				this.Networkenabled = !this.enabled;
				this.RpcLeverSound();
				ServerLogs.AddLog(ServerLogs.Modules.Warhead, ply.LoggedNameFromRefHub() + " set the Alpha Warhead status to " + this.enabled + ".", ServerLogs.ServerLogType.GameEvent);
			}
			break;
		}
	}

	private void SetDiode(DiodeType diode, bool status)
	{
		if (this._prevMats[(int)diode] != status)
		{
			this._matSet[(int)diode] = (status ? this._onMat : this._offMat);
			this._prevMats[(int)diode] = status;
			this._anyModified = true;
		}
	}

	private void Update()
	{
		this._anyModified = false;
		this.SetDiode(DiodeType.InProgress, AlphaWarheadController.InProgress);
		this.SetDiode(DiodeType.OutsideDoor, this.OutsideDoorOpen);
		this.SetDiode(DiodeType.BlastDoor, this.AnyBlastdoorClosed);
		if (this._anyModified)
		{
			this._ledRenderer.sharedMaterials = this._matSet;
		}
		this._leverStatus += (this.enabled ? 0.04f : (-0.04f));
		this._leverStatus = Mathf.Clamp01(this._leverStatus);
		this.lever.localRotation = Quaternion.Slerp(this._disabledLeverRotation, this._enabledLeverRotation, this._leverStatus);
	}

	[ClientRpc]
	private void RpcLeverSound()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void AlphaWarheadNukesitePanel::RpcLeverSound()", 791711933, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcLeverSound()
	{
	}

	protected static void InvokeUserCode_RpcLeverSound(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcLeverSound called on server.");
		}
		else
		{
			((AlphaWarheadNukesitePanel)obj).UserCode_RpcLeverSound();
		}
	}

	static AlphaWarheadNukesitePanel()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(AlphaWarheadNukesitePanel), "System.Void AlphaWarheadNukesitePanel::RpcLeverSound()", InvokeUserCode_RpcLeverSound);
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteBool(this.enabled);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteBool(this.enabled);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.enabled, null, reader.ReadBool());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.enabled, null, reader.ReadBool());
		}
	}
}
