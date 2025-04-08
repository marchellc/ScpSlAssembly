using System;
using System.Collections.Generic;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using RelativePositioning;
using UnityEngine;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class FlybyDetectorProjectile : SingleTrajectoryProjectile
	{
		protected override bool AlreadyCollided
		{
			get
			{
				return base.AlreadyCollided || this._stopped;
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
				HitboxIdentity hitboxIdentity;
				if (FlybyDetectorProjectile.Detections[i].TryGetComponent<HitboxIdentity>(out hitboxIdentity))
				{
					ReferenceHub targetHub = hitboxIdentity.TargetHub;
					if ((!(targetHub == this.PreviousOwner.Hub) || this._selfDamageDelay <= 0f) && (this._allowMultipleTriggering || this._alreadyHitPlayers.Add(targetHub)))
					{
						bool flag;
						switch (this._friendlyFireInteraction)
						{
						case FlybyDetectorProjectile.FriendlyFireInteraction.UseFriendlyFireConfig:
							flag = HitboxIdentity.IsDamageable(this.PreviousOwner.Role, targetHub.GetRoleId());
							break;
						case FlybyDetectorProjectile.FriendlyFireInteraction.CollideWithEveryone:
							flag = true;
							break;
						case FlybyDetectorProjectile.FriendlyFireInteraction.CollideWithOnlyEnemies:
							flag = HitboxIdentity.IsEnemy(this.PreviousOwner.Role, targetHub.GetRoleId());
							break;
						default:
							flag = false;
							break;
						}
						if (flag)
						{
							this.ServerProcessHit(hitboxIdentity);
							if (this._stopProjectileOnImpact)
							{
								this.RpcStopProjectile();
								break;
							}
						}
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
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			this.SendRPCInternal("System.Void InventorySystem.Items.ThrowableProjectiles.FlybyDetectorProjectile::RpcStopProjectile()", 646104935, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		static FlybyDetectorProjectile()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(FlybyDetectorProjectile), "System.Void InventorySystem.Items.ThrowableProjectiles.FlybyDetectorProjectile::RpcStopProjectile()", new RemoteCallDelegate(FlybyDetectorProjectile.InvokeUserCode_RpcStopProjectile));
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
				return;
			}
			((FlybyDetectorProjectile)obj).UserCode_RpcStopProjectile();
		}

		private static readonly CachedLayerMask HitboxMask = new CachedLayerMask(new string[] { "Hitbox" });

		private static readonly Collider[] Detections = new Collider[16];

		private readonly HashSet<ReferenceHub> _alreadyHitPlayers = new HashSet<ReferenceHub>();

		private RelativePosition _prevPosition;

		private bool _stopped;

		[SerializeField]
		private float _selfDamageDelay;

		[SerializeField]
		private FlybyDetectorProjectile.FriendlyFireInteraction _friendlyFireInteraction;

		[SerializeField]
		private bool _stopProjectileOnImpact;

		[SerializeField]
		private bool _allowMultipleTriggering;

		private enum FriendlyFireInteraction
		{
			UseFriendlyFireConfig,
			CollideWithEveryone,
			CollideWithOnlyEnemies
		}
	}
}
