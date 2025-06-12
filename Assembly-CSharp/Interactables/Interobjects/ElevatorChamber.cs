using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Interactables.Interobjects.DoorUtils;
using Interactables.Verification;
using LabApi.Events.Arguments.PlayerEvents;
using LabApi.Events.Handlers;
using Mirror;
using UnityEngine;

namespace Interactables.Interobjects;

public class ElevatorChamber : NetworkBehaviour, IServerInteractable, IInteractable
{
	public delegate void ElevatorMoved(Bounds elevatorBounds, ElevatorChamber chamber, Vector3 deltaPos, Quaternion deltaRot);

	public enum ElevatorSequence
	{
		StartingSequence,
		DoorClosing,
		MovingAway,
		Arriving,
		DoorOpening,
		Ready
	}

	public static List<ElevatorChamber> AllChambers = new List<ElevatorChamber>();

	[SyncVar]
	public ElevatorGroup AssignedGroup;

	private static readonly int DoorAnimHash = Animator.StringToHash("isOpen");

	private ElevatorDoor _lastArrivedDestination;

	private List<ElevatorDoor> _floorDoors;

	private float _percentOfRotation;

	private Bounds _cachedBounds;

	private bool _cachedBoundsUpToDate;

	private bool _dynamicAdminLock;

	private int? _lastSetDestinationLevel;

	private byte? _queuedDestination;

	private readonly Stopwatch _sequenceStopwatch = Stopwatch.StartNew();

	[SerializeField]
	private Animator _doorAnimator;

	[SerializeField]
	private Vector3 _boundsCenter;

	[SerializeField]
	private float _boundsSize;

	[SerializeField]
	private float _boundsHeight;

	[SerializeField]
	private float _doorOpenTime;

	[SerializeField]
	private float _doorCloseTime;

	[SerializeField]
	private float _animationTime;

	[SerializeField]
	private float _rotationTime;

	[SerializeField]
	private AnimationCurve _translationCurve;

	[SerializeField]
	private AnimationCurve _rotationCurve;

	[SerializeField]
	private List<AudioSource> _travelSounds;

	[SyncVar]
	private byte _syncDestinationLevel;

	[SerializeField]
	private float _rotationSizeGrowMultiplier = 1.8f;

	[SerializeField]
	private float _serverLagCompensation;

	public bool IsReady => this.CurSequence == ElevatorSequence.Ready;

	public bool IsReadyForUserInput
	{
		get
		{
			if (this.IsReady)
			{
				return !this._queuedDestination.HasValue;
			}
			return false;
		}
	}

	public int DestinationLevel => this._syncDestinationLevel;

	public bool GoingUp { get; private set; }

	public ElevatorDoor DestinationDoor
	{
		get
		{
			if (!this._floorDoors.TryGet(this.DestinationLevel, out var element))
			{
				return null;
			}
			return element;
		}
	}

	public ElevatorDoor NextDestinationDoor
	{
		get
		{
			if (!this._floorDoors.TryGet(this.NextLevel, out var element))
			{
				return null;
			}
			return element;
		}
	}

	public ElevatorSequence CurSequence { get; private set; }

	public IVerificationRule VerificationRule => StandardDistanceVerification.Default;

	public DoorLockReason ActiveLocksAnyDoors
	{
		get
		{
			DoorLockReason doorLockReason = DoorLockReason.None;
			foreach (ElevatorDoor floorDoor in this._floorDoors)
			{
				doorLockReason = (DoorLockReason)((uint)doorLockReason | (uint)floorDoor.ActiveLocks);
			}
			return doorLockReason;
		}
	}

	public DoorLockReason ActiveLocksAllDoors
	{
		get
		{
			DoorLockReason doorLockReason = (DoorLockReason)65535;
			foreach (ElevatorDoor floorDoor in this._floorDoors)
			{
				doorLockReason = (DoorLockReason)((uint)doorLockReason & (uint)floorDoor.ActiveLocks);
			}
			return doorLockReason;
		}
	}

	public bool DynamicAdminLock
	{
		get
		{
			return this._dynamicAdminLock;
		}
		set
		{
			this._dynamicAdminLock = value;
			if (value)
			{
				this.UpdateDynamicLock();
				return;
			}
			this._floorDoors.ForEach(delegate(ElevatorDoor x)
			{
				x.NetworkActiveLocks = (ushort)(x.ActiveLocks & 0xFFF7);
			});
		}
	}

	public Bounds WorldspaceBounds
	{
		get
		{
			if (!this._cachedBoundsUpToDate)
			{
				float num = this._boundsSize * (this._percentOfRotation * this._rotationSizeGrowMultiplier + 1f);
				this._cachedBounds = new Bounds(base.transform.TransformPoint(this._boundsCenter), new Vector3(num, this._boundsHeight, num));
				this._cachedBoundsUpToDate = true;
			}
			return this._cachedBounds;
		}
	}

	public int NextLevel => (this.DestinationLevel + 1) % this._floorDoors.Count;

	private float TargetLagCompensation
	{
		get
		{
			if (!NetworkServer.active)
			{
				return 0f;
			}
			return this._serverLagCompensation;
		}
	}

	private float SequenceElapsed => (float)this._sequenceStopwatch.Elapsed.TotalSeconds;

	public ElevatorGroup NetworkAssignedGroup
	{
		get
		{
			return this.AssignedGroup;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this.AssignedGroup, 1uL, null);
		}
	}

	public byte Network_syncDestinationLevel
	{
		get
		{
			return this._syncDestinationLevel;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter(value, ref this._syncDestinationLevel, 2uL, null);
		}
	}

	public static event ElevatorMoved OnElevatorMoved;

	public static event Action<ElevatorChamber> OnElevatorSpawned;

	public static event Action<ElevatorChamber> OnElevatorRemoved;

	public event Action OnSequenceChanged;

	public static bool TryGetChamber(ElevatorGroup group, out ElevatorChamber chamber)
	{
		foreach (ElevatorChamber allChamber in ElevatorChamber.AllChambers)
		{
			if (allChamber.AssignedGroup == group)
			{
				chamber = allChamber;
				return true;
			}
		}
		chamber = null;
		return false;
	}

	private void ForceDestination(int level)
	{
		ElevatorDoor elevatorDoor = this._floorDoors[level];
		if (this._lastArrivedDestination != null)
		{
			if (NetworkServer.active)
			{
				this._lastArrivedDestination.NetworkTargetState = false;
			}
			this.GoingUp = this._lastArrivedDestination.TargetPosition.y < elevatorDoor.TargetPosition.y;
			this.CurSequence = ElevatorSequence.StartingSequence;
			this._sequenceStopwatch.Restart();
		}
		else
		{
			if (NetworkServer.active)
			{
				elevatorDoor.NetworkTargetState = true;
			}
			base.transform.SetPositionAndRotation(elevatorDoor.TargetPosition, elevatorDoor.transform.rotation);
			this._lastArrivedDestination = elevatorDoor;
			this.CurSequence = ElevatorSequence.Ready;
			this.UpdateDynamicLock();
			this.SetInnerDoor(state: true);
			this.OnSequenceChanged?.Invoke();
		}
		this._lastSetDestinationLevel = level;
	}

	private void SetInnerDoor(bool state)
	{
		this._doorAnimator.SetBool(ElevatorChamber.DoorAnimHash, state);
		this._cachedBoundsUpToDate = false;
	}

	private void Awake()
	{
		this.CurSequence = ElevatorSequence.Ready;
	}

	private void Start()
	{
		ElevatorChamber.AllChambers.Add(this);
		this._floorDoors = ElevatorDoor.GetDoorsForGroup(this.AssignedGroup);
		if (NetworkServer.active)
		{
			this.ServerSetDestination(UnityEngine.Random.Range(0, this._floorDoors.Count), allowQueueing: false);
		}
		ElevatorChamber.OnElevatorSpawned?.Invoke(this);
	}

	private void OnDestroy()
	{
		ElevatorChamber.AllChambers.Remove(this);
		ElevatorChamber.OnElevatorRemoved?.Invoke(this);
	}

	private void Update()
	{
		ElevatorSequence curSequence = this.CurSequence;
		this.UpdateDestination();
		this.UpdateSequence();
		if (curSequence != this.CurSequence)
		{
			this.OnSequenceChanged?.Invoke();
		}
	}

	protected override void OnValidate()
	{
		base.OnValidate();
		this._cachedBoundsUpToDate = false;
	}

	public void ServerLockAllDoors(DoorLockReason lockReason, bool state)
	{
		foreach (ElevatorDoor floorDoor in this._floorDoors)
		{
			if (state)
			{
				floorDoor.NetworkActiveLocks = (ushort)((uint)floorDoor.ActiveLocks | (uint)lockReason);
			}
			else
			{
				floorDoor.NetworkActiveLocks = (ushort)(floorDoor.ActiveLocks & (ushort)(~(int)lockReason));
			}
		}
	}

	public void ServerSetDestination(int level, bool allowQueueing)
	{
		if (this.IsReady || !allowQueueing)
		{
			this.Network_syncDestinationLevel = (byte)level;
			this._queuedDestination = null;
		}
		else
		{
			this._queuedDestination = (byte)level;
		}
	}

	public void ServerInteract(ReferenceHub ply, byte colliderId)
	{
		bool isAllowed = true;
		if (!this.IsReadyForUserInput)
		{
			isAllowed = false;
		}
		if (this.DestinationDoor.ActiveLocks != 0)
		{
			isAllowed = false;
		}
		ElevatorPanel panel = ElevatorPanel.AllPanels.First((ElevatorPanel n) => n.AssignedChamber == this && n.ColliderId == colliderId);
		PlayerInteractingElevatorEventArgs obj = new PlayerInteractingElevatorEventArgs(ply, this, panel)
		{
			IsAllowed = isAllowed
		};
		PlayerEvents.OnInteractingElevator(obj);
		if (obj.IsAllowed)
		{
			this.ServerSetDestination(this.NextLevel, allowQueueing: false);
			PlayerEvents.OnInteractedElevator(new PlayerInteractedElevatorEventArgs(ply, this, panel));
		}
	}

	private void UpdateDestination()
	{
		if (NetworkServer.active && this._queuedDestination.HasValue && this.IsReady)
		{
			this.Network_syncDestinationLevel = this._queuedDestination.Value;
			this._queuedDestination = null;
		}
		if (this._lastSetDestinationLevel != this.DestinationLevel && this.DestinationLevel >= 0 && this.DestinationLevel < this._floorDoors.Count)
		{
			this.ForceDestination(this.DestinationLevel);
		}
	}

	private void UpdateSequence()
	{
		switch (this.CurSequence)
		{
		case ElevatorSequence.StartingSequence:
			if (!(this.SequenceElapsed < this.TargetLagCompensation))
			{
				this._travelSounds.ForEach(delegate(AudioSource x)
				{
					x.Play();
				});
				this._sequenceStopwatch.Restart();
				this.CurSequence = ElevatorSequence.DoorClosing;
				this.SetInnerDoor(state: false);
			}
			break;
		case ElevatorSequence.MovingAway:
		case ElevatorSequence.Arriving:
		{
			Transform transform = base.transform;
			Bounds worldspaceBounds = this.WorldspaceBounds;
			transform.GetPositionAndRotation(out var position, out var rotation);
			this.UpdateMovement(transform, this.CurSequence == ElevatorSequence.Arriving);
			this._cachedBoundsUpToDate = false;
			Vector3 deltaPos = transform.position - position;
			Quaternion deltaRot = transform.rotation * Quaternion.Inverse(rotation);
			ElevatorChamber.OnElevatorMoved?.Invoke(worldspaceBounds, this, deltaPos, deltaRot);
			break;
		}
		case ElevatorSequence.DoorClosing:
			if (!(this.SequenceElapsed < this._doorCloseTime))
			{
				this.CurSequence = ElevatorSequence.MovingAway;
				this._sequenceStopwatch.Restart();
			}
			break;
		case ElevatorSequence.DoorOpening:
			if (!(this.SequenceElapsed < this._doorOpenTime + this.TargetLagCompensation))
			{
				this.CurSequence = ElevatorSequence.Ready;
			}
			break;
		}
	}

	private void UpdateMovement(Transform t, bool arriving)
	{
		ElevatorDoor elevatorDoor = this._floorDoors[this.DestinationLevel];
		float num = this._animationTime - this.TargetLagCompensation;
		float num2 = Mathf.Clamp01(this.SequenceElapsed / num);
		this.UpdateRotation(t, elevatorDoor, num2, arriving);
		if (arriving)
		{
			Vector3 a = (this.GoingUp ? elevatorDoor.BottomPosition : elevatorDoor.TopPosition);
			t.position = Vector3.Lerp(a, elevatorDoor.TargetPosition, this._translationCurve.Evaluate(num2));
			if (!(num2 < 1f))
			{
				if (NetworkServer.active)
				{
					elevatorDoor.NetworkTargetState = true;
				}
				this.SetInnerDoor(state: true);
				this._lastArrivedDestination = elevatorDoor;
				this.UpdateDynamicLock();
				this.CurSequence = ElevatorSequence.DoorOpening;
				this._sequenceStopwatch.Restart();
			}
		}
		else
		{
			Vector3 b = (this.GoingUp ? this._lastArrivedDestination.TopPosition : this._lastArrivedDestination.BottomPosition);
			t.position = Vector3.Lerp(this._lastArrivedDestination.TargetPosition, b, 1f - this._translationCurve.Evaluate(1f - num2));
			if (!(num2 < 1f))
			{
				t.SetParent(elevatorDoor.transform.parent);
				this.CurSequence = ElevatorSequence.Arriving;
				this._sequenceStopwatch.Restart();
			}
		}
	}

	private void UpdateRotation(Transform t, ElevatorDoor dest, float f, bool arriving)
	{
		if (arriving)
		{
			f += 1f;
		}
		f = Mathf.InverseLerp(this._rotationTime, 2f - this._rotationTime, f);
		this._percentOfRotation = (arriving ? (1f - f) : f);
		Quaternion rotation = this._lastArrivedDestination.transform.rotation;
		Quaternion rotation2 = dest.transform.rotation;
		t.rotation = Quaternion.Lerp(rotation, rotation2, this._rotationCurve.Evaluate(f));
	}

	private void UpdateDynamicLock()
	{
		if (!this._dynamicAdminLock)
		{
			return;
		}
		for (int i = 0; i < this._floorDoors.Count; i++)
		{
			if (i == this.DestinationLevel)
			{
				ElevatorDoor elevatorDoor = this._floorDoors[i];
				elevatorDoor.NetworkActiveLocks = (ushort)(elevatorDoor.ActiveLocks & 0xFFF7);
			}
			else
			{
				ElevatorDoor elevatorDoor2 = this._floorDoors[i];
				elevatorDoor2.NetworkActiveLocks = (ushort)(elevatorDoor2.ActiveLocks | 8);
			}
		}
	}

	private void OnDrawGizmosSelected()
	{
		Gizmos.color = Color.green;
		Gizmos.DrawWireCube(this.WorldspaceBounds.center, this.WorldspaceBounds.size);
		Gizmos.color = new Color(0f, 1f, 0f, 0.1f);
		Gizmos.DrawCube(this.WorldspaceBounds.center, this.WorldspaceBounds.size);
	}

	public override bool Weaved()
	{
		return true;
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			GeneratedNetworkCode._Write_Interactables_002EInterobjects_002EElevatorGroup(writer, this.AssignedGroup);
			NetworkWriterExtensions.WriteByte(writer, this._syncDestinationLevel);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1L) != 0L)
		{
			GeneratedNetworkCode._Write_Interactables_002EInterobjects_002EElevatorGroup(writer, this.AssignedGroup);
		}
		if ((base.syncVarDirtyBits & 2L) != 0L)
		{
			NetworkWriterExtensions.WriteByte(writer, this._syncDestinationLevel);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize(ref this.AssignedGroup, null, GeneratedNetworkCode._Read_Interactables_002EInterobjects_002EElevatorGroup(reader));
			base.GeneratedSyncVarDeserialize(ref this._syncDestinationLevel, null, NetworkReaderExtensions.ReadByte(reader));
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this.AssignedGroup, null, GeneratedNetworkCode._Read_Interactables_002EInterobjects_002EElevatorGroup(reader));
		}
		if ((num & 2L) != 0L)
		{
			base.GeneratedSyncVarDeserialize(ref this._syncDestinationLevel, null, NetworkReaderExtensions.ReadByte(reader));
		}
	}
}
