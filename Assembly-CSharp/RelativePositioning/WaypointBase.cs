using System;
using System.Collections.Generic;
using UnityEngine;

namespace RelativePositioning;

public abstract class WaypointBase : MonoBehaviour
{
	private static readonly bool[] SetWaypoints = new bool[255];

	private static readonly byte[] WaypointIndexes = new byte[255];

	private static readonly WaypointBase[] AllWaypoints = new WaypointBase[255];

	private static readonly HashSet<byte> OccupiedIdFinder = new HashSet<byte>(255);

	private byte _id;

	private byte _index;

	protected virtual bool AutoAssign => true;

	protected abstract float SqrDistanceTo(Vector3 pos);

	public abstract Vector3 GetWorldspacePosition(Vector3 relPosition);

	public abstract Vector3 GetRelativePosition(Vector3 worldPos);

	public virtual Quaternion GetWorldspaceRotation(Quaternion relRotation)
	{
		return relRotation;
	}

	public virtual Quaternion GetRelativeRotation(Quaternion worldRot)
	{
		return worldRot;
	}

	protected virtual void Start()
	{
		if (this.AutoAssign)
		{
			this.AssignSelfToList();
		}
	}

	protected virtual void OnDestroy()
	{
		this.RemoveSelfFromList();
	}

	protected virtual void AssignSelfToList()
	{
		for (byte b = 1; b < byte.MaxValue; b++)
		{
			if (!WaypointBase.SetWaypoints[b])
			{
				this._index = b;
				WaypointBase.AllWaypoints[b] = this;
				WaypointBase.SetWaypoints[b] = true;
				return;
			}
		}
		Debug.LogError("Could not add waypoint '" + base.name + "' - the list is full.");
	}

	protected virtual void RemoveSelfFromList()
	{
		WaypointBase.AllWaypoints[this._index] = null;
		WaypointBase.SetWaypoints[this._index] = false;
	}

	protected void SetId(byte newId)
	{
		if (newId == 0)
		{
			throw new InvalidOperationException("Cannot assign ID of 0 to a waypoint. This ID is reserved for the value of null.");
		}
		this._id = newId;
		WaypointBase.WaypointIndexes[this._id] = this._index;
	}

	protected byte FindFreeId()
	{
		WaypointBase.OccupiedIdFinder.Clear();
		for (byte b = 1; b < byte.MaxValue; b++)
		{
			if (WaypointBase.SetWaypoints[b])
			{
				WaypointBase.OccupiedIdFinder.Add(WaypointBase.AllWaypoints[b]._id);
			}
		}
		for (byte b2 = 1; b2 < byte.MaxValue; b2++)
		{
			if (!WaypointBase.OccupiedIdFinder.Contains(b2))
			{
				return b2;
			}
		}
		Debug.LogError("Could find an empty id for '" + base.name + "'.");
		return 0;
	}

	private static void ExtractWaypointData(Vector3 worldPoint, bool extractPoint, Quaternion worldRot, bool extractRot, out byte closestId, out Vector3 relPoint, out Quaternion relRot)
	{
		float num = float.MaxValue;
		relPoint = Vector3.zero;
		relRot = Quaternion.identity;
		closestId = 0;
		WaypointBase waypointBase = null;
		for (byte b = 1; b < byte.MaxValue; b++)
		{
			if (WaypointBase.SetWaypoints[b])
			{
				WaypointBase waypointBase2 = WaypointBase.AllWaypoints[b];
				float num2 = waypointBase2.SqrDistanceTo(worldPoint);
				if (!(num2 > num))
				{
					num = num2;
					waypointBase = waypointBase2;
					closestId = waypointBase2._id;
				}
			}
		}
		if (closestId != 0)
		{
			if (extractPoint)
			{
				relPoint = waypointBase.GetRelativePosition(worldPoint);
			}
			if (extractRot)
			{
				relRot = waypointBase.GetRelativeRotation(worldRot);
			}
		}
	}

	public static void GetRelativePositionAndRotation(Vector3 worldPoint, Quaternion worldRot, out byte closestId, out Vector3 relPoint, out Quaternion relRot)
	{
		WaypointBase.ExtractWaypointData(worldPoint, extractPoint: true, worldRot, extractRot: true, out closestId, out relPoint, out relRot);
	}

	public static void GetRelativePosition(Vector3 worldPoint, out byte closestId, out Vector3 rel)
	{
		WaypointBase.ExtractWaypointData(worldPoint, extractPoint: true, Quaternion.identity, extractRot: false, out closestId, out rel, out var _);
	}

	public static void GetRelativeRotation(Vector3 center, Quaternion worldRot, out byte closestId, out Quaternion rel)
	{
		WaypointBase.ExtractWaypointData(center, extractPoint: false, worldRot, extractRot: true, out closestId, out var _, out rel);
	}

	public static Vector3 GetWorldPosition(byte id, Vector3 point)
	{
		if (!WaypointBase.TryGetWaypoint(id, out var wp))
		{
			return point;
		}
		return wp.GetWorldspacePosition(point);
	}

	public static Quaternion GetRelativeRotation(byte id, Quaternion rot)
	{
		if (!WaypointBase.TryGetWaypoint(id, out var wp))
		{
			return rot;
		}
		return wp.GetRelativeRotation(rot);
	}

	public static Quaternion GetWorldRotation(byte id, Quaternion rot)
	{
		if (!WaypointBase.TryGetWaypoint(id, out var wp))
		{
			return rot;
		}
		return wp.GetWorldspaceRotation(rot);
	}

	public static bool TryGetWaypoint(byte id, out WaypointBase wp)
	{
		int num = WaypointBase.WaypointIndexes[id];
		bool result = WaypointBase.SetWaypoints[num];
		wp = WaypointBase.AllWaypoints[num];
		return result;
	}
}
