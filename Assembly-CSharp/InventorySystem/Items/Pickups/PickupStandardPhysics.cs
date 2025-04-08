using System;
using Interactables.Interobjects;
using MapGeneration;
using Mirror;
using RelativePositioning;
using UnityEngine;

namespace InventorySystem.Items.Pickups
{
	public class PickupStandardPhysics : PickupPhysicsModule
	{
		public Rigidbody Rb { get; private set; }

		protected override ItemPickupBase Pickup
		{
			get
			{
				return this._pickup;
			}
		}

		private Vector3 LastWorldPos
		{
			get
			{
				return this._lastReceivedRelPos.Position;
			}
		}

		private Quaternion LastWorldRot
		{
			get
			{
				return WaypointBase.GetWorldRotation(this._lastReceivedRelPos.WaypointId, this._lastReceivedRelRot);
			}
		}

		private bool ClientFrozen
		{
			get
			{
				return this._isFrozen;
			}
			set
			{
				this._isFrozen = value;
				this.Rb.isKinematic = value;
				this._freezeProgress = 0f;
			}
		}

		private bool ServerSendFreeze
		{
			get
			{
				if (!this._serverEverDecelerated)
				{
					return false;
				}
				switch (this._freezingMode)
				{
				case PickupStandardPhysics.FreezingMode.Default:
					return this.Rb.velocity.sqrMagnitude < 6.25f;
				case PickupStandardPhysics.FreezingMode.FreezeWhenSleeping:
					return this.Rb.IsSleeping();
				case PickupStandardPhysics.FreezingMode.NeverFreeze:
					return false;
				default:
					throw new InvalidOperationException("Unhandled freezing mode for a pickup: " + this._freezingMode.ToString());
				}
			}
		}

		public event Action OnParentSetByElevator;

		public PickupStandardPhysics(ItemPickupBase targetPickup, PickupStandardPhysics.FreezingMode freezingMode = PickupStandardPhysics.FreezingMode.Default)
		{
			this._pickup = targetPickup;
			this._freezingMode = freezingMode;
			this.Rb = this.Pickup.GetComponent<Rigidbody>();
			this.Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
			if (NetworkServer.active)
			{
				base.ServerSetSyncData(new Action<NetworkWriter>(this.ServerWriteRigidbody));
			}
			StaticUnityMethods.OnUpdate += this.OnUpdate;
			ElevatorChamber.OnElevatorMoved += this.OnElevatorMoved;
			this.Pickup.OnInfoChanged += this.UpdateWeight;
			this.Pickup.PhysicsModuleSyncData.OnModified += this.OnSyncvarsModified;
		}

		private void UpdateWeight()
		{
			this.Rb.mass = Mathf.Max(0.001f, this.Pickup.Info.WeightKg);
		}

		private void OnSyncvarsModified()
		{
			if (NetworkServer.active)
			{
				return;
			}
			base.ClientReadSyncData(new Action<NetworkReader>(this.ClientApplyRigidbody));
		}

		private void OnUpdate()
		{
			if (NetworkServer.active)
			{
				this.UpdateServer();
				return;
			}
			this.UpdateClient();
		}

		private void UpdateServer()
		{
			bool flag = this.Rb.IsSleeping() && this._freezingMode != PickupStandardPhysics.FreezingMode.NeverFreeze;
			if (flag)
			{
				if (this._serverPrevSleeping)
				{
					return;
				}
				this._serverEverDecelerated = true;
				base.ServerSetSyncData(new Action<NetworkWriter>(this.ServerWriteRigidbody));
			}
			else
			{
				float sqrMagnitude = this.Rb.velocity.sqrMagnitude;
				if (sqrMagnitude < this._serverPrevVelSqr)
				{
					this._serverEverDecelerated = true;
				}
				this._serverPrevVelSqr = sqrMagnitude;
				if (!this._serverPrevSleeping && this._serverNextUpdateTime > NetworkTime.time)
				{
					return;
				}
				base.ServerSendRpc(new Action<NetworkWriter>(this.ServerWriteRigidbody));
				this._serverNextUpdateTime = NetworkTime.time + 0.25;
			}
			this._serverPrevSleeping = flag;
		}

		private void UpdateClient()
		{
			if (!this.ClientFrozen || this._freezeProgress > 1f || !SeedSynchronizer.MapGenerated)
			{
				return;
			}
			Vector3 position = this.Rb.position;
			Vector3 lastWorldPos = this.LastWorldPos;
			if ((position - lastWorldPos).sqrMagnitude > 0.390625f)
			{
				this._freezeProgress = 0f;
				this.Rb.position = lastWorldPos;
				this.Rb.rotation = this.LastWorldRot;
				return;
			}
			this._freezeProgress += Time.deltaTime * 1.2f;
			float num = Mathf.Lerp(10f * Time.deltaTime, 1f, Mathf.Pow(this._freezeProgress, 5f));
			this.Rb.position = Vector3.Lerp(position, lastWorldPos, num);
			this.Rb.rotation = Quaternion.Lerp(this.Rb.rotation, this.LastWorldRot, num);
		}

		private void ServerWriteRigidbody(NetworkWriter writer)
		{
			byte serverOrderClock = this._serverOrderClock;
			this._serverOrderClock = serverOrderClock + 1;
			writer.WriteByte(serverOrderClock);
			RelativePosition relativePosition = new RelativePosition(this.Rb.position);
			Quaternion relativeRotation = WaypointBase.GetRelativeRotation(relativePosition.WaypointId, this.Rb.rotation);
			writer.WriteRelativePosition(relativePosition);
			writer.WriteLowPrecisionQuaternion(new LowPrecisionQuaternion(relativeRotation));
			if (this.ServerSendFreeze)
			{
				return;
			}
			writer.WriteVector3(this.Rb.velocity);
			writer.WriteVector3(this.Rb.angularVelocity);
			if (!relativePosition.OutOfRange || !base.IsSpawned)
			{
				return;
			}
			this.Pickup.DestroySelf();
		}

		private void ClientApplyRigidbody(NetworkReader reader)
		{
			int num = (int)reader.ReadByte();
			bool flag = this._lastReceivedUpdate == null;
			if (!flag)
			{
				int num2 = this._lastReceivedUpdate.Value - num;
				if (num2 >= 0 && num2 < 127)
				{
					return;
				}
			}
			this._lastReceivedUpdate = new int?(num);
			this._lastReceivedRelPos = reader.ReadRelativePosition();
			this._lastReceivedRelRot = reader.ReadLowPrecisionQuaternion().Value;
			this.ClientFrozen = reader.Remaining == 0;
			this._freezeProgress = 0f;
			if (!flag && this.ClientFrozen)
			{
				return;
			}
			this.Rb.position = this.LastWorldPos;
			this.Rb.rotation = this.LastWorldRot;
			if (this.ClientFrozen)
			{
				return;
			}
			this.Rb.velocity = reader.ReadVector3();
			this.Rb.angularVelocity = reader.ReadVector3();
		}

		private void OnElevatorMoved(Bounds elevatorBounds, ElevatorChamber chamber, Vector3 deltaPos, Quaternion deltaRot)
		{
			bool flag = this._inElevator && chamber == this._trackedChamber;
			if (!chamber.WorldspaceBounds.Contains(this.Pickup.Position))
			{
				if (!flag)
				{
					return;
				}
				this._inElevator = false;
				this.Pickup.Position -= deltaPos;
				this.Pickup.transform.SetParent(null);
				Action onParentSetByElevator = this.OnParentSetByElevator;
				if (onParentSetByElevator == null)
				{
					return;
				}
				onParentSetByElevator();
				return;
			}
			else
			{
				this.Rb.velocity = deltaRot * this.Rb.velocity;
				if (flag)
				{
					return;
				}
				this.Pickup.transform.SetParent(chamber.transform);
				this.Pickup.Position += deltaPos;
				this._trackedChamber = chamber;
				this._inElevator = true;
				Action onParentSetByElevator2 = this.OnParentSetByElevator;
				if (onParentSetByElevator2 == null)
				{
					return;
				}
				onParentSetByElevator2();
				return;
			}
		}

		internal override void ClientProcessRpc(NetworkReader rpcData)
		{
			base.ClientProcessRpc(rpcData);
			if (NetworkServer.active)
			{
				return;
			}
			this.ClientApplyRigidbody(rpcData);
		}

		public override void DestroyModule()
		{
			base.DestroyModule();
			StaticUnityMethods.OnUpdate -= this.OnUpdate;
			ElevatorChamber.OnElevatorMoved -= this.OnElevatorMoved;
		}

		private readonly PickupStandardPhysics.FreezingMode _freezingMode;

		private readonly ItemPickupBase _pickup;

		private ElevatorChamber _trackedChamber;

		private double _serverNextUpdateTime;

		private RelativePosition _lastReceivedRelPos;

		private Quaternion _lastReceivedRelRot;

		private bool _serverPrevSleeping;

		private bool _serverEverDecelerated;

		private float _serverPrevVelSqr;

		private bool _inElevator;

		private bool _isFrozen;

		private byte _serverOrderClock;

		private int? _lastReceivedUpdate;

		private float _freezeProgress;

		private const float UpdateCooldown = 0.25f;

		private const float FreezeSpeed = 1.2f;

		private const float FreezePow = 5f;

		private const float UnfrozenLerp = 10f;

		private const float MinWeight = 0.001f;

		private const float UnfreezeVelocity = 2.5f;

		private const float UnfreezeVelocitySqr = 6.25f;

		private const float FrozenTeleportDisSqr = 0.390625f;

		public enum FreezingMode
		{
			Default,
			FreezeWhenSleeping,
			NeverFreeze
		}
	}
}
