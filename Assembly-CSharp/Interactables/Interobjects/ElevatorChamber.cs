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

namespace Interactables.Interobjects
{
	public class ElevatorChamber : NetworkBehaviour, IServerInteractable, IInteractable
	{
		public static event ElevatorChamber.ElevatorMoved OnElevatorMoved;

		public static event Action<ElevatorChamber> OnElevatorSpawned;

		public static event Action<ElevatorChamber> OnElevatorRemoved;

		public event Action OnSequenceChanged;

		public bool IsReady
		{
			get
			{
				return this.CurSequence == ElevatorChamber.ElevatorSequence.Ready;
			}
		}

		public bool IsReadyForUserInput
		{
			get
			{
				return this.IsReady && this._queuedDestination == null;
			}
		}

		public int DestinationLevel
		{
			get
			{
				return (int)this._syncDestinationLevel;
			}
		}

		public bool GoingUp { get; private set; }

		public ElevatorDoor DestinationDoor
		{
			get
			{
				ElevatorDoor elevatorDoor;
				if (!this._floorDoors.TryGet(this.DestinationLevel, out elevatorDoor))
				{
					return null;
				}
				return elevatorDoor;
			}
		}

		public ElevatorDoor NextDestinationDoor
		{
			get
			{
				ElevatorDoor elevatorDoor;
				if (!this._floorDoors.TryGet(this.NextLevel, out elevatorDoor))
				{
					return null;
				}
				return elevatorDoor;
			}
		}

		public ElevatorChamber.ElevatorSequence CurSequence { get; private set; }

		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		public DoorLockReason ActiveLocksAnyDoors
		{
			get
			{
				DoorLockReason doorLockReason = DoorLockReason.None;
				foreach (ElevatorDoor elevatorDoor in this._floorDoors)
				{
					doorLockReason |= (DoorLockReason)elevatorDoor.ActiveLocks;
				}
				return doorLockReason;
			}
		}

		public DoorLockReason ActiveLocksAllDoors
		{
			get
			{
				DoorLockReason doorLockReason = (DoorLockReason)65535;
				foreach (ElevatorDoor elevatorDoor in this._floorDoors)
				{
					doorLockReason &= (DoorLockReason)elevatorDoor.ActiveLocks;
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
					x.NetworkActiveLocks = x.ActiveLocks & 65527;
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

		public int NextLevel
		{
			get
			{
				return (this.DestinationLevel + 1) % this._floorDoors.Count;
			}
		}

		public static bool TryGetChamber(ElevatorGroup group, out ElevatorChamber chamber)
		{
			foreach (ElevatorChamber elevatorChamber in ElevatorChamber.AllChambers)
			{
				if (elevatorChamber.AssignedGroup == group)
				{
					chamber = elevatorChamber;
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
				this._travelSounds.ForEach(delegate(AudioSource x)
				{
					x.Play();
				});
				this.CurSequence = ElevatorChamber.ElevatorSequence.DoorClosing;
				this._seqTimer.Restart();
				this.SetInnerDoor(false);
			}
			else
			{
				if (NetworkServer.active)
				{
					elevatorDoor.NetworkTargetState = true;
				}
				base.transform.SetPositionAndRotation(elevatorDoor.TargetPosition, elevatorDoor.transform.rotation);
				this._lastArrivedDestination = elevatorDoor;
				this.CurSequence = ElevatorChamber.ElevatorSequence.Ready;
				this.UpdateDynamicLock();
				this.SetInnerDoor(true);
				Action onSequenceChanged = this.OnSequenceChanged;
				if (onSequenceChanged != null)
				{
					onSequenceChanged();
				}
			}
			this._lastSetDestinationLevel = new int?(level);
		}

		private void SetInnerDoor(bool state)
		{
			this._doorAnimator.SetBool(ElevatorChamber.DoorAnimHash, state);
			this._cachedBoundsUpToDate = false;
		}

		private void Awake()
		{
			this.CurSequence = ElevatorChamber.ElevatorSequence.Ready;
		}

		private void Start()
		{
			ElevatorChamber.AllChambers.Add(this);
			this._floorDoors = ElevatorDoor.GetDoorsForGroup(this.AssignedGroup);
			if (NetworkServer.active)
			{
				this.ServerSetDestination(global::UnityEngine.Random.Range(0, this._floorDoors.Count), false);
			}
			Action<ElevatorChamber> onElevatorSpawned = ElevatorChamber.OnElevatorSpawned;
			if (onElevatorSpawned == null)
			{
				return;
			}
			onElevatorSpawned(this);
		}

		private void OnDestroy()
		{
			ElevatorChamber.AllChambers.Remove(this);
			Action<ElevatorChamber> onElevatorRemoved = ElevatorChamber.OnElevatorRemoved;
			if (onElevatorRemoved == null)
			{
				return;
			}
			onElevatorRemoved(this);
		}

		private void Update()
		{
			ElevatorChamber.ElevatorSequence curSequence = this.CurSequence;
			this.UpdateDestination();
			this.UpdateSequence();
			if (curSequence != this.CurSequence)
			{
				Action onSequenceChanged = this.OnSequenceChanged;
				if (onSequenceChanged == null)
				{
					return;
				}
				onSequenceChanged();
			}
		}

		protected override void OnValidate()
		{
			base.OnValidate();
			this._cachedBoundsUpToDate = false;
		}

		public void ServerLockAllDoors(DoorLockReason lockReason, bool state)
		{
			foreach (ElevatorDoor elevatorDoor in this._floorDoors)
			{
				if (state)
				{
					ElevatorDoor elevatorDoor2 = elevatorDoor;
					elevatorDoor2.NetworkActiveLocks = elevatorDoor2.ActiveLocks | (ushort)lockReason;
				}
				else
				{
					ElevatorDoor elevatorDoor3 = elevatorDoor;
					elevatorDoor3.NetworkActiveLocks = elevatorDoor3.ActiveLocks & (ushort)(~(ushort)lockReason);
				}
			}
		}

		public void ServerSetDestination(int level, bool allowQueueing)
		{
			if (this.IsReady || !allowQueueing)
			{
				this.Network_syncDestinationLevel = (byte)level;
				this._queuedDestination = null;
				return;
			}
			this._queuedDestination = new byte?((byte)level);
		}

		public void ServerInteract(ReferenceHub ply, byte colliderId)
		{
			bool flag = true;
			if (!this.IsReadyForUserInput)
			{
				flag = false;
			}
			if (this.DestinationDoor.ActiveLocks != 0)
			{
				flag = false;
			}
			ElevatorPanel elevatorPanel = ElevatorPanel.AllPanels.First((ElevatorPanel n) => n.AssignedChamber == this && n.ColliderId == colliderId);
			PlayerInteractingElevatorEventArgs playerInteractingElevatorEventArgs = new PlayerInteractingElevatorEventArgs(ply, this, elevatorPanel);
			playerInteractingElevatorEventArgs.IsAllowed = flag;
			PlayerEvents.OnInteractingElevator(playerInteractingElevatorEventArgs);
			if (!playerInteractingElevatorEventArgs.IsAllowed)
			{
				return;
			}
			this.ServerSetDestination(this.NextLevel, false);
			PlayerEvents.OnInteractedElevator(new PlayerInteractedElevatorEventArgs(ply, this, elevatorPanel));
		}

		private void UpdateDestination()
		{
			if (NetworkServer.active && this._queuedDestination != null && this.IsReady)
			{
				this.Network_syncDestinationLevel = this._queuedDestination.Value;
				this._queuedDestination = null;
			}
			int? lastSetDestinationLevel = this._lastSetDestinationLevel;
			int destinationLevel = this.DestinationLevel;
			if ((lastSetDestinationLevel.GetValueOrDefault() == destinationLevel) & (lastSetDestinationLevel != null))
			{
				return;
			}
			if (this.DestinationLevel < 0 || this.DestinationLevel >= this._floorDoors.Count)
			{
				return;
			}
			this.ForceDestination(this.DestinationLevel);
		}

		private void UpdateSequence()
		{
			switch (this.CurSequence)
			{
			case ElevatorChamber.ElevatorSequence.DoorClosing:
				if (this._seqTimer.Elapsed.TotalSeconds < (double)this._doorCloseTime)
				{
					return;
				}
				this.CurSequence = ElevatorChamber.ElevatorSequence.MovingAway;
				this._seqTimer.Restart();
				return;
			case ElevatorChamber.ElevatorSequence.MovingAway:
			case ElevatorChamber.ElevatorSequence.Arriving:
			{
				Transform transform = base.transform;
				Bounds worldspaceBounds = this.WorldspaceBounds;
				Vector3 vector;
				Quaternion quaternion;
				transform.GetPositionAndRotation(out vector, out quaternion);
				this.UpdateMovement(transform, this.CurSequence == ElevatorChamber.ElevatorSequence.Arriving);
				this._cachedBoundsUpToDate = false;
				Vector3 vector2 = transform.position - vector;
				Quaternion quaternion2 = transform.rotation * Quaternion.Inverse(quaternion);
				ElevatorChamber.ElevatorMoved onElevatorMoved = ElevatorChamber.OnElevatorMoved;
				if (onElevatorMoved == null)
				{
					return;
				}
				onElevatorMoved(worldspaceBounds, this, vector2, quaternion2);
				return;
			}
			case ElevatorChamber.ElevatorSequence.DoorOpening:
				if (this._seqTimer.Elapsed.TotalSeconds < (double)this._doorOpenTime)
				{
					return;
				}
				this.CurSequence = ElevatorChamber.ElevatorSequence.Ready;
				return;
			default:
				return;
			}
		}

		private void UpdateMovement(Transform t, bool arriving)
		{
			ElevatorDoor elevatorDoor = this._floorDoors[this.DestinationLevel];
			float num = Mathf.Clamp01((float)this._seqTimer.Elapsed.TotalSeconds / this._animationTime);
			this.UpdateRotation(t, elevatorDoor, num, arriving);
			if (arriving)
			{
				Vector3 vector = (this.GoingUp ? elevatorDoor.BottomPosition : elevatorDoor.TopPosition);
				t.position = Vector3.Lerp(vector, elevatorDoor.TargetPosition, this._translationCurve.Evaluate(num));
				if (num < 1f)
				{
					return;
				}
				if (NetworkServer.active)
				{
					elevatorDoor.NetworkTargetState = true;
				}
				this.SetInnerDoor(true);
				this._lastArrivedDestination = elevatorDoor;
				this.UpdateDynamicLock();
				this.CurSequence = ElevatorChamber.ElevatorSequence.DoorOpening;
				this._seqTimer.Restart();
				return;
			}
			else
			{
				Vector3 vector2 = (this.GoingUp ? this._lastArrivedDestination.TopPosition : this._lastArrivedDestination.BottomPosition);
				t.position = Vector3.Lerp(this._lastArrivedDestination.TargetPosition, vector2, 1f - this._translationCurve.Evaluate(1f - num));
				if (num < 1f)
				{
					return;
				}
				t.SetParent(elevatorDoor.transform.parent);
				this.CurSequence = ElevatorChamber.ElevatorSequence.Arriving;
				this._seqTimer.Restart();
				return;
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
					elevatorDoor.NetworkActiveLocks = elevatorDoor.ActiveLocks & 65527;
				}
				else
				{
					ElevatorDoor elevatorDoor2 = this._floorDoors[i];
					elevatorDoor2.NetworkActiveLocks = elevatorDoor2.ActiveLocks | 8;
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

		public ElevatorGroup NetworkAssignedGroup
		{
			get
			{
				return this.AssignedGroup;
			}
			[param: In]
			set
			{
				base.GeneratedSyncVarSetter<ElevatorGroup>(value, ref this.AssignedGroup, 1UL, null);
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
				base.GeneratedSyncVarSetter<byte>(value, ref this._syncDestinationLevel, 2UL, null);
			}
		}

		public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
		{
			base.SerializeSyncVars(writer, forceAll);
			if (forceAll)
			{
				global::Mirror.GeneratedNetworkCode._Write_Interactables.Interobjects.ElevatorGroup(writer, this.AssignedGroup);
				writer.WriteByte(this._syncDestinationLevel);
				return;
			}
			writer.WriteULong(base.syncVarDirtyBits);
			if ((base.syncVarDirtyBits & 1UL) != 0UL)
			{
				global::Mirror.GeneratedNetworkCode._Write_Interactables.Interobjects.ElevatorGroup(writer, this.AssignedGroup);
			}
			if ((base.syncVarDirtyBits & 2UL) != 0UL)
			{
				writer.WriteByte(this._syncDestinationLevel);
			}
		}

		public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
		{
			base.DeserializeSyncVars(reader, initialState);
			if (initialState)
			{
				base.GeneratedSyncVarDeserialize<ElevatorGroup>(ref this.AssignedGroup, null, global::Mirror.GeneratedNetworkCode._Read_Interactables.Interobjects.ElevatorGroup(reader));
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncDestinationLevel, null, reader.ReadByte());
				return;
			}
			long num = (long)reader.ReadULong();
			if ((num & 1L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<ElevatorGroup>(ref this.AssignedGroup, null, global::Mirror.GeneratedNetworkCode._Read_Interactables.Interobjects.ElevatorGroup(reader));
			}
			if ((num & 2L) != 0L)
			{
				base.GeneratedSyncVarDeserialize<byte>(ref this._syncDestinationLevel, null, reader.ReadByte());
			}
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

		private readonly Stopwatch _seqTimer = Stopwatch.StartNew();

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

		public delegate void ElevatorMoved(Bounds elevatorBounds, ElevatorChamber chamber, Vector3 deltaPos, Quaternion deltaRot);

		public enum ElevatorSequence
		{
			DoorClosing,
			MovingAway,
			Arriving,
			DoorOpening,
			Ready
		}
	}
}
