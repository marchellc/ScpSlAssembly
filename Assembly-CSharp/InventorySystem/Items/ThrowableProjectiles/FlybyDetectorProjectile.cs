using System.Collections.Generic;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using RelativePositioning;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles;

public class FlybyDetectorProjectile : SingleTrajectoryProjectile
{
	private enum FriendlyFireInteraction
	{
		UseFriendlyFireConfig,
		CollideWithEveryone,
		CollideWithOnlyEnemies
	}

	private static readonly CachedLayerMask HitboxMask;

	private static readonly Collider[] Detections;

	private readonly HashSet<ReferenceHub> _alreadyHitPlayers = new HashSet<ReferenceHub>();

	private RelativePosition _prevPosition;

	private bool _stopped;

	[SerializeField]
	private float _selfDamageDelay;

	[SerializeField]
	private FriendlyFireInteraction _friendlyFireInteraction;

	[SerializeField]
	private bool _stopProjectileOnImpact;

	[SerializeField]
	private bool _allowMultipleTriggering;

	protected override bool AlreadyCollided
	{
		get
		{
			if (!base.AlreadyCollided)
			{
				return this._stopped;
			}
			return true;
		}
	}

	protected override void Awake()
	{
		base.Awake();
		this._prevPosition = new RelativePosition(base.Position);
	}

	protected override void Update()
	{
		base.Update();
		if (!NetworkServer.active || this.AlreadyCollided)
		{
			return;
		}
		this._selfDamageDelay -= Time.deltaTime;
		int num = Physics.OverlapCapsuleNonAlloc(base.Position, this._prevPosition.Position, base.ProjectileRadius, FlybyDetectorProjectile.Detections, FlybyDetectorProjectile.HitboxMask);
		for (int i = 0; i < num; i++)
		{
			if (!FlybyDetectorProjectile.Detections[i].TryGetComponent<HitboxIdentity>(out var component))
			{
				continue;
			}
			ReferenceHub targetHub = component.TargetHub;
			if ((!(targetHub == base.PreviousOwner.Hub) || !(this._selfDamageDelay > 0f)) && (this._allowMultipleTriggering || this._alreadyHitPlayers.Add(targetHub)) && this._friendlyFireInteraction switch
			{
				FriendlyFireInteraction.CollideWithEveryone => true, 
				FriendlyFireInteraction.CollideWithOnlyEnemies => HitboxIdentity.IsEnemy(base.PreviousOwner.Role, targetHub.GetRoleId()), 
				FriendlyFireInteraction.UseFriendlyFireConfig => HitboxIdentity.IsDamageable(base.PreviousOwner.Role, targetHub.GetRoleId()), 
				_ => false, 
			})
			{
				this.ServerProcessHit(component);
				if (this._stopProjectileOnImpact)
				{
					this.RpcStopProjectile();
					break;
				}
			}
		}
		this._prevPosition = new RelativePosition(base.Position);
	}

	public virtual void ServerProcessHit(HitboxIdentity hid)
	{
	}

	[ClientRpc]
	public void RpcStopProjectile()
	{
		NetworkWriterPooled writer = NetworkWriterPool.Get();
		this.SendRPCInternal("System.Void InventorySystem.Items.ThrowableProjectiles.FlybyDetectorProjectile::RpcStopProjectile()", 646104935, writer, 0, includeOwner: true);
		NetworkWriterPool.Return(writer);
	}

	static FlybyDetectorProjectile()
	{
		FlybyDetectorProjectile.HitboxMask = new CachedLayerMask("Hitbox");
		FlybyDetectorProjectile.Detections = new Collider[16];
		RemoteProcedureCalls.RegisterRpc(typeof(FlybyDetectorProjectile), "System.Void InventorySystem.Items.ThrowableProjectiles.FlybyDetectorProjectile::RpcStopProjectile()", InvokeUserCode_RpcStopProjectile);
	}

	public override bool Weaved()
	{
		return true;
	}

	protected void UserCode_RpcStopProjectile()
	{
		this._stopped = true;
	}

	protected static void InvokeUserCode_RpcStopProjectile(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
	{
		if (!NetworkClient.active)
		{
			Debug.LogError("RPC RpcStopProjectile called on server.");
		}
		else
		{
			((FlybyDetectorProjectile)obj).UserCode_RpcStopProjectile();
		}
	}
}
