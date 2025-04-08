using System;
using System.Collections.Generic;
using Interactables;
using Interactables.Interobjects.DoorUtils;
using InventorySystem.Items.Pickups;
using LabApi.Events.Arguments.ServerEvents;
using LabApi.Events.Handlers;
using Mirror;
using Mirror.RemoteCalls;
using PlayerRoles;
using PlayerStatsSystem;
using RelativePositioning;
using UnityEngine;
using Utils;

namespace InventorySystem.Items.ThrowableProjectiles
{
	public class Scp018Projectile : TimeGrenade
	{
		public float CurrentDamage
		{
			get
			{
				return this._damageOverVelocity.Evaluate(this._lastVelocity);
			}
		}

		private bool IgnoreFriendlyFire
		{
			get
			{
				return !this.PreviousOwner.IsSet || NetworkTime.time > this._activationTime + (double)this._friendlyFireTime;
			}
		}

		protected override float MinSoundCooldown
		{
			get
			{
				if (this._bypassBounceSoundCooldown)
				{
					this._bypassBounceSoundCooldown = false;
					return Time.deltaTime;
				}
				return base.MinSoundCooldown;
			}
		}

		protected override PickupPhysicsModule DefaultPhysicsModule
		{
			get
			{
				return new PickupStandardPhysics(this, PickupStandardPhysics.FreezingMode.FreezeWhenSleeping);
			}
		}

		public Vector3 RecreatedVelocity { get; private set; }

		[ClientRpc]
		public void RpcPlayBounce(float velSqr)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteFloat(velSqr);
			this.SendRPCInternal("System.Void InventorySystem.Items.ThrowableProjectiles.Scp018Projectile::RpcPlayBounce(System.Single)", -734142472, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		protected override void ProcessCollision(Collision collision)
		{
			if (base.PhysicsModule is Scp018Physics)
			{
				return;
			}
			base.ProcessCollision(collision);
			if (!NetworkServer.active)
			{
				return;
			}
			float sqrMagnitude = collision.relativeVelocity.sqrMagnitude;
			this.RpcPlayBounce(sqrMagnitude);
			if (sqrMagnitude < this._activationVelocitySqr)
			{
				return;
			}
			this.SetupModule();
			this.ServerActivate();
			PickupSyncInfo info = this.Info;
			info.Locked = true;
			base.NetworkInfo = info;
		}

		protected override void Start()
		{
			base.Start();
			this._tr = base.transform;
		}

		protected override void Update()
		{
			base.Update();
			Scp018Physics scp018Physics = base.PhysicsModule as Scp018Physics;
			if (scp018Physics == null)
			{
				return;
			}
			Vector3 position = scp018Physics.Position;
			Vector3 vector = position - this._prevTrPos;
			this._tr.position = position;
			this._prevTrPos = position;
			this.RecreatedVelocity = vector / Time.deltaTime;
			if (!NetworkServer.active)
			{
				return;
			}
			if (this._prevFlybyPos == null)
			{
				this._prevFlybyPos = new RelativePosition?(new RelativePosition(position));
				return;
			}
			int num = Physics.OverlapCapsuleNonAlloc(this._prevFlybyPos.Value.Position, position, this._flybyHitregRadius, Scp018Projectile.HitregDetections, Scp018Projectile.FlybyHitregMask);
			while (num-- > 0)
			{
				HitboxIdentity hitboxIdentity;
				if (Scp018Projectile.HitregDetections[num].TryGetComponent<HitboxIdentity>(out hitboxIdentity) && this._damagedPlayersSinceLastBounce.Add(hitboxIdentity.NetworkId))
				{
					float num2 = this.CurrentDamage;
					if (hitboxIdentity.TargetHub.IsSCP(true))
					{
						num2 *= this._scpDamageMultiplier;
					}
					hitboxIdentity.Damage(num2, new Scp018DamageHandler(this, num2, this.IgnoreFriendlyFire), position);
				}
			}
			this._prevFlybyPos = new RelativePosition?(new RelativePosition(position));
		}

		public override bool ServerFuseEnd()
		{
			if (!base.ServerFuseEnd())
			{
				return false;
			}
			ExplosionUtils.ServerExplode(this._tr.position, this.PreviousOwner, ExplosionType.SCP018);
			base.DestroySelf();
			ServerEvents.OnProjectileExploded(new ProjectileExplodedEventArgs(this, this.PreviousOwner.Hub, base.transform.position));
			return true;
		}

		[ClientRpc]
		internal override void SendPhysicsModuleRpc(ArraySegment<byte> arrSeg)
		{
			NetworkWriterPooled networkWriterPooled = NetworkWriterPool.Get();
			networkWriterPooled.WriteArraySegmentAndSize(arrSeg);
			this.SendRPCInternal("System.Void InventorySystem.Items.ThrowableProjectiles.Scp018Projectile::SendPhysicsModuleRpc(System.ArraySegment`1<System.Byte>)", 1041392993, networkWriterPooled, 0, true);
			NetworkWriterPool.Return(networkWriterPooled);
		}

		internal void RegisterBounce(float velocity, Vector3 point)
		{
			this._lastVelocity = velocity;
			this._bypassBounceSoundCooldown = true;
			float num = velocity * velocity;
			base.MakeCollisionSound(Mathf.Max(10f, num));
			if (!NetworkServer.active)
			{
				return;
			}
			int num2 = Physics.OverlapSphereNonAlloc(point, this._bounceHitregRadius, Scp018Projectile.HitregDetections, Scp018Projectile.BounceHitregMask);
			while (num2-- > 0)
			{
				Collider collider = Scp018Projectile.HitregDetections[num2];
				BreakableWindow breakableWindow;
				InteractableCollider interactableCollider;
				if (collider.TryGetComponent<BreakableWindow>(out breakableWindow))
				{
					breakableWindow.Damage(this.CurrentDamage, new Scp018DamageHandler(this, this.CurrentDamage, this.IgnoreFriendlyFire), point);
				}
				else if (collider.TryGetComponent<InteractableCollider>(out interactableCollider))
				{
					IDamageableDoor damageableDoor = interactableCollider.Target as IDamageableDoor;
					if (damageableDoor != null)
					{
						damageableDoor.ServerDamage(this.CurrentDamage * this._doorDamageMultiplier, DoorDamageType.Grenade, this.PreviousOwner);
					}
				}
			}
			this._damagedPlayersSinceLastBounce.Clear();
		}

		private void SetupModule()
		{
			this._activationTime = NetworkTime.time;
			this._damagedPlayersSinceLastBounce = new HashSet<uint>();
			base.PhysicsModule.DestroyModule();
			base.PhysicsModule = new Scp018Physics(this, this._trail, this._radius, this._maximumVelocity, this._onBounceVelocityAddition);
		}

		static Scp018Projectile()
		{
			RemoteProcedureCalls.RegisterRpc(typeof(Scp018Projectile), "System.Void InventorySystem.Items.ThrowableProjectiles.Scp018Projectile::RpcPlayBounce(System.Single)", new RemoteCallDelegate(Scp018Projectile.InvokeUserCode_RpcPlayBounce__Single));
			RemoteProcedureCalls.RegisterRpc(typeof(Scp018Projectile), "System.Void InventorySystem.Items.ThrowableProjectiles.Scp018Projectile::SendPhysicsModuleRpc(System.ArraySegment`1<System.Byte>)", new RemoteCallDelegate(Scp018Projectile.InvokeUserCode_SendPhysicsModuleRpc__ArraySegment`1));
		}

		public override bool Weaved()
		{
			return true;
		}

		protected void UserCode_RpcPlayBounce__Single(float velSqr)
		{
			if (NetworkServer.active)
			{
				return;
			}
			base.MakeCollisionSound(velSqr);
		}

		protected static void InvokeUserCode_RpcPlayBounce__Single(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC RpcPlayBounce called on server.");
				return;
			}
			((Scp018Projectile)obj).UserCode_RpcPlayBounce__Single(reader.ReadFloat());
		}

		protected override void UserCode_SendPhysicsModuleRpc__ArraySegment`1(ArraySegment<byte> arrSeg)
		{
			if (arrSeg.Count == 19 && !(base.PhysicsModule is Scp018Physics))
			{
				this.SetupModule();
			}
			base.UserCode_SendPhysicsModuleRpc__ArraySegment`1(arrSeg);
		}

		protected new static void InvokeUserCode_SendPhysicsModuleRpc__ArraySegment`1(NetworkBehaviour obj, NetworkReader reader, NetworkConnectionToClient senderConnection)
		{
			if (!NetworkClient.active)
			{
				Debug.LogError("RPC SendPhysicsModuleRpc called on server.");
				return;
			}
			((Scp018Projectile)obj).UserCode_SendPhysicsModuleRpc__ArraySegment`1(reader.ReadArraySegmentAndSize());
		}

		private static readonly CachedLayerMask BounceHitregMask = new CachedLayerMask(new string[] { "Door", "Glass" });

		private static readonly CachedLayerMask FlybyHitregMask = new CachedLayerMask(new string[] { "Hitbox" });

		private static readonly Collider[] HitregDetections = new Collider[8];

		private Transform _tr;

		private float _lastVelocity;

		private double _activationTime;

		private bool _bypassBounceSoundCooldown;

		private Vector3 _prevTrPos;

		private RelativePosition? _prevFlybyPos;

		private HashSet<uint> _damagedPlayersSinceLastBounce;

		[SerializeField]
		private float _radius;

		[SerializeField]
		private float _maximumVelocity;

		[SerializeField]
		private float _onBounceVelocityAddition;

		[SerializeField]
		private float _activationVelocitySqr;

		[SerializeField]
		private AnimationCurve _damageOverVelocity;

		[SerializeField]
		private float _doorDamageMultiplier;

		[SerializeField]
		private float _scpDamageMultiplier;

		[SerializeField]
		private float _friendlyFireTime;

		[SerializeField]
		private float _bounceHitregRadius;

		[SerializeField]
		private float _flybyHitregRadius;

		[SerializeField]
		private ParticleSystem _trail;
	}
}
