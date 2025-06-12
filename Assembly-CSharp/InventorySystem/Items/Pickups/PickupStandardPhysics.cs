using System;
using Interactables.Interobjects;
using MapGeneration;
using Mirror;
using RelativePositioning;
using UnityEngine;

namespace InventorySystem.Items.Pickups;

public class PickupStandardPhysics : PickupPhysicsModule
{
	public enum FreezingMode
	{
		Default,
		FreezeWhenSleeping,
		NeverFreeze
	}

	private readonly FreezingMode _freezingMode;

	private readonly ItemPickupBase _pickup;

	private readonly Action<NetworkReader> _clientApplyRigidbody;

	private readonly Action<NetworkWriter> _serverWriteRigidbody;

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

	private const float FrozenTeleportDisSqr = 25f / 64f;

	public Rigidbody Rb { get; private set; }

	protected override ItemPickupBase Pickup => this._pickup;

	private Vector3 LastWorldPos => this._lastReceivedRelPos.Position;

	private Quaternion LastWorldRot => WaypointBase.GetWorldRotation(this._lastReceivedRelPos.WaypointId, this._lastReceivedRelRot);

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
			return this._freezingMode switch
			{
				FreezingMode.Default => this.Rb.linearVelocity.sqrMagnitude < 6.25f, 
				FreezingMode.FreezeWhenSleeping => this.Rb.IsSleeping(), 
				FreezingMode.NeverFreeze => false, 
				_ => throw new InvalidOperationException("Unhandled freezing mode for a pickup: " + this._freezingMode), 
			};
		}
	}

	public event Action OnParentSetByElevator;

	public PickupStandardPhysics(ItemPickupBase targetPickup, FreezingMode freezingMode = FreezingMode.Default)
	{
		this._pickup = targetPickup;
		this._freezingMode = freezingMode;
		this.Rb = this.Pickup.GetComponent<Rigidbody>();
		this.Rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
		this._clientApplyRigidbody = ClientApplyRigidbody;
		this._serverWriteRigidbody = ServerWriteRigidbody;
		if (NetworkServer.active)
		{
			base.ServerSetSyncData(this._serverWriteRigidbody);
		}
		else
		{
			this.Rb.interpolation = RigidbodyInterpolation.Interpolate;
		}
		StaticUnityMethods.OnUpdate += OnUpdate;
		ElevatorChamber.OnElevatorMoved += OnElevatorMoved;
		this.Pickup.OnInfoChanged += UpdateWeight;
		this.Pickup.PhysicsModuleSyncData.OnModified += OnSyncvarsModified;
	}

	private void UpdateWeight()
	{
		this.Rb.mass = Mathf.Max(0.001f, this.Pickup.Info.WeightKg);
	}

	private void OnSyncvarsModified()
	{
		if (!NetworkServer.active)
		{
			base.ClientReadSyncData(this._clientApplyRigidbody);
		}
	}

	private void OnUpdate()
	{
		if (NetworkServer.active)
		{
			this.UpdateServer();
		}
		else
		{
			this.UpdateClient();
		}
	}

	private void UpdateServer()
	{
		bool flag = this.Rb.IsSleeping() && this._freezingMode != FreezingMode.NeverFreeze;
		if (flag)
		{
			if (this._serverPrevSleeping)
			{
				return;
			}
			this._serverEverDecelerated = true;
			base.ServerSetSyncData(this._serverWriteRigidbody);
		}
		else
		{
			float sqrMagnitude = this.Rb.linearVelocity.sqrMagnitude;
			if (sqrMagnitude < this._serverPrevVelSqr)
			{
				this._serverEverDecelerated = true;
			}
			this._serverPrevVelSqr = sqrMagnitude;
			if (!this._serverPrevSleeping && this._serverNextUpdateTime > NetworkTime.time)
			{
				return;
			}
			base.ServerSendRpc(this._serverWriteRigidbody);
			this._serverNextUpdateTime = NetworkTime.time + 0.25;
		}
		this._serverPrevSleeping = flag;
	}

	private void UpdateClient()
	{
		if (this.ClientFrozen && !(this._freezeProgress > 1f) && SeedSynchronizer.MapGenerated)
		{
			Vector3 position = this.Rb.position;
			Vector3 lastWorldPos = this.LastWorldPos;
			if ((position - lastWorldPos).sqrMagnitude > 25f / 64f)
			{
				this._freezeProgress = 0f;
				this.Rb.position = lastWorldPos;
				this.Rb.rotation = this.LastWorldRot;
			}
			else
			{
				this._freezeProgress += Time.deltaTime * 1.2f;
				float t = Mathf.Lerp(10f * Time.deltaTime, 1f, Mathf.Pow(this._freezeProgress, 5f));
				this.Rb.position = Vector3.Lerp(position, lastWorldPos, t);
				this.Rb.rotation = Quaternion.Lerp(this.Rb.rotation, this.LastWorldRot, t);
			}
		}
	}

	private void ServerWriteRigidbody(NetworkWriter writer)
	{
		writer.WriteByte(this._serverOrderClock++);
		RelativePosition msg = new RelativePosition(this.Rb.position);
		Quaternion relativeRotation = WaypointBase.GetRelativeRotation(msg.WaypointId, this.Rb.rotation);
		writer.WriteRelativePosition(msg);
		writer.WriteLowPrecisionQuaternion(new LowPrecisionQuaternion(relativeRotation));
		if (!this.ServerSendFreeze)
		{
			writer.WriteVector3(this.Rb.linearVelocity);
			writer.WriteVector3(this.Rb.angularVelocity);
			if (msg.OutOfRange && base.IsSpawned)
			{
				this.Pickup.DestroySelf();
			}
		}
	}

	private void ClientApplyRigidbody(NetworkReader reader)
	{
		int num = reader.ReadByte();
		bool flag = !this._lastReceivedUpdate.HasValue;
		if (!flag)
		{
			int num2 = this._lastReceivedUpdate.Value - num;
			if (num2 >= 0 && num2 < 127)
			{
				return;
			}
		}
		this._lastReceivedUpdate = num;
		this._lastReceivedRelPos = reader.ReadRelativePosition();
		this._lastReceivedRelRot = reader.ReadLowPrecisionQuaternion().Value;
		this.ClientFrozen = reader.Remaining == 0;
		this._freezeProgress = 0f;
		if (flag || !this.ClientFrozen)
		{
			this.Rb.position = this.LastWorldPos;
			this.Rb.rotation = this.LastWorldRot;
			if (!this.ClientFrozen)
			{
				this.Rb.linearVelocity = reader.ReadVector3();
				this.Rb.angularVelocity = reader.ReadVector3();
			}
		}
	}

	private void OnElevatorMoved(Bounds elevatorBounds, ElevatorChamber chamber, Vector3 deltaPos, Quaternion deltaRot)
	{
		bool flag = this._inElevator && chamber == this._trackedChamber;
		if (!chamber.WorldspaceBounds.Contains(this.Pickup.Position))
		{
			if (flag)
			{
				this._inElevator = false;
				this.Pickup.Position -= deltaPos;
				this.Pickup.transform.SetParent(null);
				this.Rb.interpolation = RigidbodyInterpolation.Interpolate;
				this.OnParentSetByElevator?.Invoke();
			}
			return;
		}
		if (!this.Rb.isKinematic)
		{
			this.Rb.linearVelocity = deltaRot * this.Rb.linearVelocity;
		}
		if (!flag)
		{
			this.Rb.interpolation = RigidbodyInterpolation.None;
			this.Pickup.transform.SetParent(chamber.transform);
			this.Pickup.Position += deltaPos;
			this._trackedChamber = chamber;
			this._inElevator = true;
			this.OnParentSetByElevator?.Invoke();
		}
	}

	internal override void ClientProcessRpc(NetworkReader rpcData)
	{
		base.ClientProcessRpc(rpcData);
		if (!NetworkServer.active)
		{
			this.ClientApplyRigidbody(rpcData);
		}
	}

	public override void DestroyModule()
	{
		base.DestroyModule();
		StaticUnityMethods.OnUpdate -= OnUpdate;
		ElevatorChamber.OnElevatorMoved -= OnElevatorMoved;
	}
}
