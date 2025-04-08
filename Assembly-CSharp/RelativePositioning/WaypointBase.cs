using System;
using System.Collections.Generic;
using UnityEngine;

namespace RelativePositioning
{
	public abstract class WaypointBase : MonoBehaviour
	{
		protected virtual bool AutoAssign
		{
			get
			{
				return true;
			}
		}

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
			for (byte b = 1; b < 255; b += 1)
			{
				if (!WaypointBase.SetWaypoints[(int)b])
				{
					this._index = b;
					WaypointBase.AllWaypoints[(int)b] = this;
					WaypointBase.SetWaypoints[(int)b] = true;
					return;
				}
			}
			Debug.LogError("Could not add waypoint '" + base.name + "' - the list is full.");
		}

		protected virtual void RemoveSelfFromList()
		{
			WaypointBase.AllWaypoints[(int)this._index] = null;
			WaypointBase.SetWaypoints[(int)this._index] = false;
		}

		protected void SetId(byte newId)
		{
			if (newId == 0)
			{
				throw new InvalidOperationException("Cannot assign ID of 0 to a waypoint. This ID is reserved for the value of null.");
			}
			this._id = newId;
			WaypointBase.WaypointIndexes[(int)this._id] = this._index;
		}

		protected byte FindFreeId()
		{
			WaypointBase.OccupiedIdFinder.Clear();
			for (byte b = 1; b < 255; b += 1)
			{
				if (WaypointBase.SetWaypoints[(int)b])
				{
					WaypointBase.OccupiedIdFinder.Add(WaypointBase.AllWaypoints[(int)b]._id);
				}
			}
			for (byte b2 = 1; b2 < 255; b2 += 1)
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
			for (byte b = 1; b < 255; b += 1)
			{
				if (WaypointBase.SetWaypoints[(int)b])
				{
					WaypointBase waypointBase2 = WaypointBase.AllWaypoints[(int)b];
					float num2 = waypointBase2.SqrDistanceTo(worldPoint);
					if (num2 <= num)
					{
						num = num2;
						waypointBase = waypointBase2;
						closestId = waypointBase2._id;
					}
				}
			}
			if (closestId == 0)
			{
				return;
			}
			if (extractPoint)
			{
				relPoint = waypointBase.GetRelativePosition(worldPoint);
			}
			if (extractRot)
			{
				relRot = waypointBase.GetRelativeRotation(worldRot);
			}
		}

		public static void GetRelativePositionAndRotation(Vector3 worldPoint, Quaternion worldRot, out byte closestId, out Vector3 relPoint, out Quaternion relRot)
		{
			WaypointBase.ExtractWaypointData(worldPoint, true, worldRot, true, out closestId, out relPoint, out relRot);
		}

		public static void GetRelativePosition(Vector3 worldPoint, out byte closestId, out Vector3 rel)
		{
			Quaternion quaternion;
			WaypointBase.ExtractWaypointData(worldPoint, true, Quaternion.identity, false, out closestId, out rel, out quaternion);
		}

		public static void GetRelativeRotation(Vector3 center, Quaternion worldRot, out byte closestId, out Quaternion rel)
		{
			Vector3 vector;
			WaypointBase.ExtractWaypointData(center, false, worldRot, true, out closestId, out vector, out rel);
		}

		public static Vector3 GetWorldPosition(byte id, Vector3 point)
		{
			WaypointBase waypointBase;
			if (!WaypointBase.TryGetWaypoint(id, out waypointBase))
			{
				return point;
			}
			return waypointBase.GetWorldspacePosition(point);
		}

		public static Quaternion GetRelativeRotation(byte id, Quaternion rot)
		{
			WaypointBase waypointBase;
			if (!WaypointBase.TryGetWaypoint(id, out waypointBase))
			{
				return rot;
			}
			return waypointBase.GetRelativeRotation(rot);
		}

		public static Quaternion GetWorldRotation(byte id, Quaternion rot)
		{
			WaypointBase waypointBase;
			if (!WaypointBase.TryGetWaypoint(id, out waypointBase))
			{
				return rot;
			}
			return waypointBase.GetWorldspaceRotation(rot);
		}

		public static bool TryGetWaypoint(byte id, out WaypointBase wp)
		{
			int num = (int)WaypointBase.WaypointIndexes[(int)id];
			bool flag = WaypointBase.SetWaypoints[num];
			wp = WaypointBase.AllWaypoints[num];
			return flag;
		}

		private static readonly bool[] SetWaypoints = new bool[255];

		private static readonly byte[] WaypointIndexes = new byte[255];

		private static readonly WaypointBase[] AllWaypoints = new WaypointBase[255];

		private static readonly HashSet<byte> OccupiedIdFinder = new HashSet<byte>(255);

		private byte _id;

		private byte _index;
	}
}
