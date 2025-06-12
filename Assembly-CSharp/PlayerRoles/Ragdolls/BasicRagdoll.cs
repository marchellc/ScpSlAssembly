using System.Runtime.InteropServices;
using DeathAnimations;
using Mirror;
using Mirror.RemoteCalls;
using RoundRestarting;
using UnityEngine;

namespace PlayerRoles.Ragdolls;

public class BasicRagdoll : NetworkBehaviour
{
	[SyncVar(hook = "OnSyncDataChanged")]
	public RagdollData Info;

	public DeathAnimation[] AllDeathAnimations;

	private float _existenceTime;

	private bool _roundRestartEventSet;

	[SerializeField]
	private Transform _originPoint;

	public virtual Transform CenterPoint
	{
		get
		{
			if (!(this._originPoint != null))
			{
				return base.transform;
			}
			return this._originPoint;
		}
	}

	public bool Frozen { get; private set; }

	public RagdollData NetworkInfo
	{
		get
		{
			return this.Info;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.Info, 1uL, OnSyncDataChanged);
		}
	}

	public virtual void FreezeRagdoll()
	{
		this.Frozen = true;
	}

	public virtual BasicRagdoll ServerInstantiateSelf(ReferenceHub owner, RoleTypeId targetRole)
	{
		return Object.Instantiate(this);
	}

	public virtual GameObject ClientHandleSpawn(SpawnMessage msg)
	{
		return Object.Instantiate(base.gameObject, msg.position, msg.rotation);
	}

	public virtual void ClientHandleDespawn(GameObject spawned)
	{
		Object.Destroy(spawned);
	}

	[ClientRpc]
	public void ClientFreezeRpc()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void PlayerRoles.Ragdolls.BasicRagdoll::ClientFreezeRpc()", 866326727, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private void OnRestartTriggered()
	{
		NetworkServer.Destroy(base.gameObject);
	}

	private void OnSyncDataChanged(RagdollData old, RagdollData newer)
	{
		if (!(base.gameObject == null))
		{
			if (old.StartPosition != newer.StartPosition)
			{
				base.transform.position = newer.StartPosition;
			}
			if (old.StartRotation != newer.StartRotation)
			{
				base.transform.rotation = newer.StartRotation;
			}
			base.transform.localScale = newer.Scale;
		}
	}

	protected virtual void Start()
	{
		base.transform.SetPositionAndRotation(this.Info.StartPosition, this.Info.StartRotation);
		this.Info.Handler.ProcessRagdoll(this);
		RagdollManager.OnSpawnedRagdoll(this);
		if (NetworkServer.active)
		{
			this._roundRestartEventSet = true;
			RoundRestart.OnRestartTriggered += OnRestartTriggered;
		}
	}

	protected virtual void OnDestroy()
	{
		RagdollManager.OnRemovedRagdoll(this);
		if (this._roundRestartEventSet)
		{
			RoundRestart.OnRestartTriggered -= OnRestartTriggered;
		}
	}

	protected virtual void Update()
	{
		this.UpdateFreeze();
	}

	private void UpdateFreeze()
	{
		if (!this.Frozen)
		{
			this._existenceTime += Time.deltaTime;
			if (!(this._existenceTime < (float)RagdollManager.FreezeTime))
			{
				this.FreezeRagdoll();
			}
		}
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_ClientFreezeRpc()
	{
		this.FreezeRagdoll();
	}

	protected static void InvokeUserCode_ClientFreezeRpc(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC ClientFreezeRpc called on server.");
		}
		else
		{
			((BasicRagdoll)obj).UserCode_ClientFreezeRpc();
		}
	}

	static BasicRagdoll()
	{
		RemoteProcedureCalls.RegisterRpc(typeof(BasicRagdoll), "System.Void PlayerRoles.Ragdolls.BasicRagdoll::ClientFreezeRpc()", InvokeUserCode_ClientFreezeRpc);
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteRagdollData(this.Info);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteRagdollData(this.Info);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.Info, OnSyncDataChanged, reader.ReadRagdollData());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.Info, OnSyncDataChanged, reader.ReadRagdollData());
		}
	}
}
