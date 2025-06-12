using System.Collections.Generic;
using InventorySystem.Items.Pickups;
using MapGeneration.Distributors;
using Mirror;
using UnityEngine;

namespace RelativePositioning;

public class LockerWaypoint : WaypointBase
{
	private struct LockerWaypointAssignMessage : NetworkMessage
	{
		public uint LockerNetId;

		public byte Chamber;

		public byte WaypointId;
	}

	private static readonly HashSet<LockerWaypoint> AllInstances = new HashSet<LockerWaypoint>();

	private static readonly Collider[] PickupDetectionNonAlloc = new Collider[16];

	private static readonly CachedLayerMask PickupDetectionLayer = new CachedLayerMask("InteractableNoPlayerCollision");

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

	private byte ChamberIndex
	{
		get
		{
			int valueOrDefault = this._chamberId.GetValueOrDefault();
			if (!this._chamberId.HasValue)
			{
				valueOrDefault = this._assignedLocker.Chambers.IndexOf(this._trackedChamber);
				this._chamberId = valueOrDefault;
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
			this.CachedTr.GetPositionAndRotation(out var position, out var rotation);
			Vector3 center = position + rotation * this._localBounds.center;
			Vector3 size = (rotation * this._localBounds.size).Abs();
			return new Bounds(center, size);
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
		this._trackedChamber.OnDoorStatusSet += OnDoorSet;
		ItemPickupBase.OnPickupAdded += OnPickupAdded;
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		LockerWaypoint.AllInstances.Remove(this);
		this._trackedChamber.OnDoorStatusSet -= OnDoorSet;
		ItemPickupBase.OnPickupAdded -= OnPickupAdded;
	}

	private void OnDoorSet()
	{
		if (NetworkServer.active && !this._moving)
		{
			byte valueOrDefault = this._prevWaypointId.GetValueOrDefault();
			if (!this._prevWaypointId.HasValue)
			{
				valueOrDefault = base.FindFreeId();
				this._prevWaypointId = valueOrDefault;
			}
			this.ServerSetWaypoint(this._prevWaypointId.Value);
		}
	}

	private void OnPickupAdded(ItemPickupBase ipb)
	{
		if (this._moving && this.WorldspaceBounds.Contains(ipb.Position))
		{
			this._adoptedPickups.Add(ipb);
			ipb.transform.SetParent(this.CachedTr);
		}
	}

	private void Update()
	{
		if (this._moving)
		{
			this.AdoptPickups();
			if (NetworkServer.active && this._trackedChamber.CanInteract)
			{
				this.ServerSetWaypoint(0);
			}
		}
	}

	private void ServerSetWaypoint(byte waypointId)
	{
		this.ClientSetWaypoint(waypointId);
		NetworkServer.SendToReady(new LockerWaypointAssignMessage
		{
			LockerNetId = this._assignedLocker.netId,
			Chamber = this.ChamberIndex,
			WaypointId = waypointId
		});
	}

	private void ClientSetWaypoint(byte waypointId)
	{
		this._moving = waypointId != 0;
		this.RevalidateAdoptedPickups();
		if (this._moving)
		{
			base.SetId(waypointId);
			this.AdoptPickups();
		}
	}

	private void AdoptPickups()
	{
		Bounds worldspaceBounds = this.WorldspaceBounds;
		int num = Physics.OverlapBoxNonAlloc(worldspaceBounds.center, worldspaceBounds.extents, LockerWaypoint.PickupDetectionNonAlloc, Quaternion.identity, LockerWaypoint.PickupDetectionLayer);
		for (int i = 0; i < num; i++)
		{
			if (LockerWaypoint.PickupDetectionNonAlloc[i].transform.TryGetComponentInParent<ItemPickupBase>(out var comp) && worldspaceBounds.Contains(comp.Position) && !(comp.transform.parent == this.CachedTr))
			{
				this._adoptedPickups.Add(comp);
				comp.transform.SetParent(this.CachedTr);
			}
		}
	}

	private void RevalidateAdoptedPickups()
	{
		Bounds worldspaceBounds = this.WorldspaceBounds;
		for (int num = this._adoptedPickups.Count - 1; num >= 0; num--)
		{
			ItemPickupBase itemPickupBase = this._adoptedPickups[num];
			bool flag = itemPickupBase == null;
			Rigidbody component;
			if (flag || !worldspaceBounds.Contains(itemPickupBase.Position))
			{
				if (!flag)
				{
					itemPickupBase.transform.SetParent(null);
				}
				this._adoptedPickups.RemoveAt(num);
			}
			else if (NetworkServer.active && itemPickupBase.TryGetComponent<Rigidbody>(out component))
			{
				component.WakeUp();
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
			NetworkClient.ReplaceHandler<LockerWaypointAssignMessage>(ClientProcessMessage);
		};
	}

	private static void ClientProcessMessage(LockerWaypointAssignMessage msg)
	{
		if (NetworkServer.active)
		{
			return;
		}
		foreach (LockerWaypoint allInstance in LockerWaypoint.AllInstances)
		{
			if (allInstance._assignedLocker.netId == msg.LockerNetId && allInstance.ChamberIndex == msg.Chamber)
			{
				allInstance.ClientSetWaypoint(msg.WaypointId);
				break;
			}
		}
	}
}
