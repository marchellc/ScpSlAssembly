using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using CustomPlayerEffects;
using Footprinting;
using InventorySystem.Items.ThrowableProjectiles;
using InventorySystem.Items.Usables.Scp244;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using MEC;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using RelativePositioning;
using UnityEngine;

namespace Hazards;

public class TantrumEnvironmentalHazard : TemporaryHazard
{
	public static readonly List<TantrumEnvironmentalHazard> AllTantrums;

	private const float ExplosionHeight = 5f;

	private const float DelayedDestroy = 6f;

	[CompilerGenerated]
	[SyncVar]
	private RelativePosition _003CSynchronizedPosition_003Ek__BackingField;

	private float _decaySpeedOverride = -1f;

	private readonly float _explodeDistance = 5.25f;

	[SerializeField]
	private Transform _correctPosition;

	public override Vector3 SourcePosition
	{
		get
		{
			return this._correctPosition.position + this.SourceOffset;
		}
		set
		{
			base.transform.position = value;
		}
	}

	public RelativePosition SynchronizedPosition
	{
		[CompilerGenerated]
		get
		{
			return this._003CSynchronizedPosition_003Ek__BackingField;
		}
		[CompilerGenerated]
		set
		{
			this.Network_003CSynchronizedPosition_003Ek__BackingField = value;
		}
	}

	public bool PlaySizzle { get; set; }

	public override float HazardDuration { get; set; } = 180f;

	public override float DecaySpeed
	{
		get
		{
			if (this._decaySpeedOverride >= 0f)
			{
				return this._decaySpeedOverride;
			}
			float num = 1f;
			foreach (Scp244DeployablePickup instance in Scp244DeployablePickup.Instances)
			{
				num += instance.FogPercentForPoint(this.SourcePosition);
			}
			return num;
		}
	}

	public RelativePosition Network_003CSynchronizedPosition_003Ek__BackingField
	{
		get
		{
			return this.SynchronizedPosition;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.SynchronizedPosition, 1uL, null);
		}
	}

	public override bool OnEnter(ReferenceHub player)
	{
		if (!this.IsActive || player.IsSCP())
		{
			return false;
		}
		if (!base.OnEnter(player))
		{
			return false;
		}
		player.playerEffectsController.EnableEffect<Stained>(1f);
		PlayerEvents.OnEnteredHazard(new PlayerEnteredHazardEventArgs(player, this));
		return true;
	}

	public override void OnStay(ReferenceHub player)
	{
		player.playerEffectsController.EnableEffect<Stained>(1f);
	}

	public override bool OnExit(ReferenceHub player)
	{
		if (!base.OnExit(player))
		{
			return false;
		}
		if (!this.IsActive || player.IsSCP())
		{
			return false;
		}
		player.playerEffectsController.EnableEffect<Stained>(2f);
		PlayerEvents.OnLeftHazard(new PlayerLeftHazardEventArgs(player, this));
		return true;
	}

	protected override void Start()
	{
		base.Start();
		this.ClientApplyDecalSize();
		if (NetworkServer.active)
		{
			TantrumEnvironmentalHazard.AllTantrums.Add(this);
			ExplosionGrenade.OnExploded += CheckExplosion;
		}
	}

	protected override void ClientApplyDecalSize()
	{
	}

	[Server]
	public override void ServerDestroy()
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Void Hazards.TantrumEnvironmentalHazard::ServerDestroy()' called when server was not active");
			return;
		}
		base.ServerDestroy();
		this.RpcDespawn(this.PlaySizzle);
		this.ServerDelayedDestroy(6f);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (NetworkServer.active)
		{
			TantrumEnvironmentalHazard.AllTantrums.Remove(this);
			ExplosionGrenade.OnExploded -= CheckExplosion;
		}
	}

	[ClientRpc]
	private void RpcDespawn(bool playSizzle)
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		writer.WriteBool(playSizzle);
		this.SendRPCInternal("System.Void Hazards.TantrumEnvironmentalHazard::RpcDespawn(System.Boolean)", -963326249, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	private void ServerDelayedDestroy(float waitTime)
	{
		if (NetworkServer.active)
		{
			Timing.RunCoroutine(this.DelayedPuddleRemoval(waitTime).CancelWith(base.gameObject));
		}
	}

	private void LateUpdate()
	{
		this.SourcePosition = this.SynchronizedPosition.Position;
	}

	private void CheckExplosion(Footprint attacker, Vector3 pos, ExplosionGrenade grenade)
	{
		Vector3 position = this._correctPosition.position;
		if (!(Mathf.Abs(pos.y - position.y) > 5f))
		{
			float num = this._explodeDistance * this._explodeDistance;
			if (!((position - pos).SqrMagnitudeIgnoreY() > num))
			{
				this.PlaySizzle = true;
				this.ServerDestroy();
			}
		}
	}

	private IEnumerator<float> DelayedPuddleRemoval(float waitTime)
	{
		yield return Timing.WaitForSeconds(waitTime);
		NetworkServer.Destroy(base.gameObject);
	}

	static TantrumEnvironmentalHazard()
	{
		TantrumEnvironmentalHazard.AllTantrums = new List<TantrumEnvironmentalHazard>();
		RemoteProcedureCalls.RegisterRpc(typeof(TantrumEnvironmentalHazard), "System.Void Hazards.TantrumEnvironmentalHazard::RpcDespawn(System.Boolean)", InvokeUserCode_RpcDespawn__Boolean);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcDespawn__Boolean(bool playSizzle)
	{
	}

	protected static void InvokeUserCode_RpcDespawn__Boolean(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcDespawn called on server.");
		}
		else
		{
			((TantrumEnvironmentalHazard)obj).UserCode_RpcDespawn__Boolean(reader.ReadBool());
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteRelativePosition(this.SynchronizedPosition);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			writer.WriteRelativePosition(this.SynchronizedPosition);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.SynchronizedPosition, null, reader.ReadRelativePosition());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.SynchronizedPosition, null, reader.ReadRelativePosition());
		}
	}
}
