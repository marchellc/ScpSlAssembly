using System;
using System.Collections.Generic;
using InventorySystem.Items.Pickups;
using MapGeneration.Distributors;
using Mirror;
using UnityEngine;

namespace RelativePositioning
{
	public class LockerWaypoint : WaypointBase
	{
		private byte ChamberIndex
		{
			get
			{
				int num = this._chamberId.GetValueOrDefault();
				if (this._chamberId == null)
				{
					num = this._assignedLocker.Chambers.IndexOf(this._trackedChamber);
					this._chamberId = new int?(num);
				}
				return (byte)this._chamberId.Value;
			}
		}

		private Transform CachedTr
		{
			get
			{
				if (!this._trCacheSet)
				{
					this._transformCache = base.transform;
					this._trCacheSet = true;
				}
				return this._transformCache;
			}
		}

		private Bounds WorldspaceBounds
		{
			get
			{
				Vector3 vector;
				Quaternion quaternion;
				this.CachedTr.GetPositionAndRotation(out vector, out quaternion);
				Vector3 vector2 = vector + quaternion * this._localBounds.center;
				Vector3 vector3 = (quaternion * this._localBounds.size).Abs();
				return new Bounds(vector2, vector3);
			}
		}

		protected override float SqrDistanceTo(Vector3 pos)
		{
			if (!this._moving || !this.WorldspaceBounds.Contains(pos))
			{
				return float.MaxValue;
			}
			return -1f;
		}

		public override Vector3 GetWorldspacePosition(Vector3 relPosition)
		{
			return this.CachedTr.TransformPoint(relPosition);
		}

		public override Vector3 GetRelativePosition(Vector3 worldPoint)
		{
			return this.CachedTr.InverseTransformPoint(worldPoint);
		}

		public override Quaternion GetWorldspaceRotation(Quaternion relRotation)
		{
			return this.CachedTr.rotation * relRotation;
		}

		public override Quaternion GetRelativeRotation(Quaternion worldRot)
		{
			return Quaternion.Inverse(this.CachedTr.rotation) * worldRot;
		}

		protected override void Start()
		{
			base.Start();
			LockerWaypoint.AllInstances.Add(this);
			this._trackedChamber.OnDoorStatusSet += this.OnDoorSet;
			ItemPickupBase.OnPickupAdded += this.OnPickupAdded;
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			LockerWaypoint.AllInstances.Remove(this);
			this._trackedChamber.OnDoorStatusSet -= this.OnDoorSet;
			ItemPickupBase.OnPickupAdded -= this.OnPickupAdded;
		}

		private void OnDoorSet()
		{
			if (!NetworkServer.active || this._moving)
			{
				return;
			}
			byte b = this._prevWaypointId.GetValueOrDefault();
			if (this._prevWaypointId == null)
			{
				b = base.FindFreeId();
				this._prevWaypointId = new byte?(b);
			}
			this.ServerSetWaypoint(this._prevWaypointId.Value);
		}

		private void OnPickupAdded(ItemPickupBase ipb)
		{
			if (!this._moving)
			{
				return;
			}
			if (!this.WorldspaceBounds.Contains(ipb.Position))
			{
				return;
			}
			this._adoptedPickups.Add(ipb);
			ipb.transform.SetParent(this.CachedTr);
		}

		private void Update()
		{
			if (!this._moving)
			{
				return;
			}
			this.AdoptPickups();
			if (!NetworkServer.active || !this._trackedChamber.CanInteract)
			{
				return;
			}
			this.ServerSetWaypoint(0);
		}

		private void ServerSetWaypoint(byte waypointId)
		{
			this.ClientSetWaypoint(waypointId);
			NetworkServer.SendToReady<LockerWaypoint.LockerWaypointAssignMessage>(new LockerWaypoint.LockerWaypointAssignMessage
			{
				LockerNetId = this._assignedLocker.netId,
				Chamber = this.ChamberIndex,
				WaypointId = waypointId
			}, 0);
		}

		private void ClientSetWaypoint(byte waypointId)
		{
			this._moving = waypointId > 0;
			this.RevalidateAdoptedPickups();
			if (!this._moving)
			{
				return;
			}
			base.SetId(waypointId);
			this.AdoptPickups();
		}

		private void AdoptPickups()
		{
			Bounds worldspaceBounds = this.WorldspaceBounds;
			int num = Physics.OverlapBoxNonAlloc(worldspaceBounds.center, worldspaceBounds.extents, LockerWaypoint.PickupDetectionNonAlloc, Quaternion.identity, LockerWaypoint.PickupDetectionLayer);
			for (int i = 0; i < num; i++)
			{
				ItemPickupBase itemPickupBase;
				if (LockerWaypoint.PickupDetectionNonAlloc[i].transform.TryGetComponentInParent(out itemPickupBase) && worldspaceBounds.Contains(itemPickupBase.Position) && !(itemPickupBase.transform.parent == this.CachedTr))
				{
					this._adoptedPickups.Add(itemPickupBase);
					itemPickupBase.transform.SetParent(this.CachedTr);
				}
			}
		}

		private void RevalidateAdoptedPickups()
		{
			Bounds worldspaceBounds = this.WorldspaceBounds;
			for (int i = this._adoptedPickups.Count - 1; i >= 0; i--)
			{
				ItemPickupBase itemPickupBase = this._adoptedPickups[i];
				bool flag = itemPickupBase == null;
				Rigidbody rigidbody;
				if (flag || !worldspaceBounds.Contains(itemPickupBase.Position))
				{
					if (!flag)
					{
						itemPickupBase.transform.SetParent(null);
					}
					this._adoptedPickups.RemoveAt(i);
				}
				else if (NetworkServer.active && itemPickupBase.TryGetComponent<Rigidbody>(out rigidbody))
				{
					rigidbody.WakeUp();
				}
			}
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.green;
			Gizmos.DrawWireCube(this.WorldspaceBounds.center, this.WorldspaceBounds.size);
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			CustomNetworkManager.OnClientReady += delegate
			{
				NetworkClient.ReplaceHandler<LockerWaypoint.LockerWaypointAssignMessage>(new Action<LockerWaypoint.LockerWaypointAssignMessage>(LockerWaypoint.ClientProcessMessage), true);
			};
		}

		private static void ClientProcessMessage(LockerWaypoint.LockerWaypointAssignMessage msg)
		{
			if (NetworkServer.active)
			{
				return;
			}
			foreach (LockerWaypoint lockerWaypoint in LockerWaypoint.AllInstances)
			{
				if (lockerWaypoint._assignedLocker.netId == msg.LockerNetId && lockerWaypoint.ChamberIndex == msg.Chamber)
				{
					lockerWaypoint.ClientSetWaypoint(msg.WaypointId);
					break;
				}
			}
		}

		private static readonly HashSet<LockerWaypoint> AllInstances = new HashSet<LockerWaypoint>();

		private static readonly Collider[] PickupDetectionNonAlloc = new Collider[16];

		private static readonly CachedLayerMask PickupDetectionLayer = new CachedLayerMask(new string[] { "InteractableNoPlayerCollision" });

		[SerializeField]
		private Locker _assignedLocker;

		[SerializeField]
		private LockerChamber _trackedChamber;

		[SerializeField]
		private Bounds _localBounds;

		private readonly List<ItemPickupBase> _adoptedPickups = new List<ItemPickupBase>();

		private Transform _transformCache;

		private bool _trCacheSet;

		private bool _moving;

		private int? _chamberId;

		private byte? _prevWaypointId;

		private struct LockerWaypointAssignMessage : NetworkMessage
		{
			public uint LockerNetId;

			public byte Chamber;

			public byte WaypointId;
		}
	}
}
