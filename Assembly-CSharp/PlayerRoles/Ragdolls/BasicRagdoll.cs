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
			if (!(_originPoint != null))
			{
				return base.transform;
			}
			return _originPoint;
		}
	}

	public bool Frozen { get; private set; }

	public RagdollData NetworkInfo
	{
		get
		{
			return Info;
		}
		[param: In]
		set
		{
			GeneratedSyncVarSetter(value, ref Info, 1uL, OnSyncDataChanged);
		}
	}

	public virtual void FreezeRagdoll()
	{
		Frozen = true;
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
		SendRPCInternal("System.Void PlayerRoles.Ragdolls.BasicRagdoll::ClientFreezeRpc()", 866326727, writer, 0, includeOwner: true);
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
		base.transform.SetPositionAndRotation(Info.StartPosition, Info.StartRotation);
		Info.Handler.ProcessRagdoll(this);
		RagdollManager.OnSpawnedRagdoll(this);
		if (NetworkServer.active)
		{
			_roundRestartEventSet = true;
			RoundRestart.OnRestartTriggered += OnRestartTriggered;
		}
	}

	protected virtual void OnDestroy()
	{
		RagdollManager.OnRemovedRagdoll(this);
		if (_roundRestartEventSet)
		{
			RoundRestart.OnRestartTriggered -= OnRestartTriggered;
		}
	}

	protected virtual void Update()
	{
		UpdateFreeze();
	}

	private void UpdateFreeze()
	{
		if (!Frozen)
		{
			_existenceTime += Time.deltaTime;
			if (!(_existenceTime < (float)RagdollManager.FreezeTime))
			{
				FreezeRagdoll();
			}
		}
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_ClientFreezeRpc()
	{
		FreezeRagdoll();
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
			writer.WriteRagdollData(Info);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteRagdollData(Info);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			GeneratedSyncVarDeserialize(ref Info, OnSyncDataChanged, reader.ReadRagdollData());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			GeneratedSyncVarDeserialize(ref Info, OnSyncDataChanged, reader.ReadRagdollData());
		}
	}
}
