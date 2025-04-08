using System;
using System.Collections.Generic;
using System.Linq;
using MapGeneration;
using NorthwoodLib.Pools;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Spawnpoints
{
	[Serializable]
	public class RoomRoleSpawnpoint : ISpawnpointHandler
	{
		private List<BoundsRoleSpawnpoint> Spawnpoints
		{
			get
			{
				if (this._spawnpointsCache != null)
				{
					int? lastSeed = this._lastSeed;
					int seed = SeedSynchronizer.Seed;
					if ((lastSeed.GetValueOrDefault() == seed) & (lastSeed != null))
					{
						goto IL_002F;
					}
				}
				this.RefreshSpawnpoints();
				IL_002F:
				return this._spawnpointsCache;
			}
		}

		public RoomRoleSpawnpoint(Vector3 localPoint, float lookRotation, float lookAngleVariation, float boundsWidth, float boundsLength, int spawnpointsInWidth, int spawnpointsInLength, RoomName nameFilter = RoomName.Unnamed, FacilityZone zoneFilter = FacilityZone.None, RoomShape shapeFilter = RoomShape.Undefined)
		{
			this._fName = nameFilter;
			this._fZone = zoneFilter;
			this._fShape = shapeFilter;
			this._localPoint = localPoint;
			this._lookAngle = lookRotation;
			this._angleVar = lookAngleVariation;
			this._width = boundsWidth;
			this._length = boundsLength;
			this._wNum = spawnpointsInWidth;
			this._lNum = spawnpointsInLength;
		}

		public RoomRoleSpawnpoint(RoomRoleSpawnpoint t)
			: this(t._localPoint, t._lookAngle, t._angleVar, t._width, t._length, t._wNum, t._lNum, t._fName, t._fZone, t._fShape)
		{
		}

		public bool TryGetSpawnpoint(out Vector3 position, out float horizontalRot)
		{
			if (this.Spawnpoints.Count == 0)
			{
				position = Vector3.zero;
				horizontalRot = 0f;
				return false;
			}
			return this.Spawnpoints.RandomItem<BoundsRoleSpawnpoint>().TryGetSpawnpoint(out position, out horizontalRot);
		}

		public bool TryGetSpawnpoint(out Vector3 position, out float horizontalRot, int spawnpointIndex)
		{
			position = default(Vector3);
			horizontalRot = 0f;
			if (spawnpointIndex > this.Spawnpoints.Count - 1)
			{
				Debug.LogWarning("Provided spawnpointIndex was too high.");
				return false;
			}
			return this.Spawnpoints[spawnpointIndex].TryGetSpawnpoint(out position, out horizontalRot);
		}

		public void FilterSpawnpointsByDistance()
		{
			if (this._distanceRequiredMeters <= 0 || this._requireDistanceFrom.Length == 0)
			{
				return;
			}
			BoundsRoleSpawnpoint[] array = this.Spawnpoints.ToArray();
			this.Spawnpoints.Clear();
			List<RoomIdentifier> list = ListPool<RoomIdentifier>.Shared.Rent();
			foreach (RoomIdentifier roomIdentifier in RoomIdentifier.AllRoomIdentifiers)
			{
				if (this._requireDistanceFrom.Contains(roomIdentifier.Name))
				{
					list.Add(roomIdentifier);
				}
			}
			for (int i = 0; i < array.Length; i++)
			{
				float num;
				Vector3 spawnpointPosition;
				if (array[i].TryGetSpawnpoint(out spawnpointPosition, out num) && list.Any((RoomIdentifier roomType) => roomType != null && (roomType.transform.position - spawnpointPosition).sqrMagnitude < (float)(this._distanceRequiredMeters * this._distanceRequiredMeters)))
				{
					this.Spawnpoints.Add(array[i]);
				}
			}
			ListPool<RoomIdentifier>.Shared.Return(list);
		}

		public int GetRoomAmount()
		{
			return this.Spawnpoints.Count;
		}

		private void RefreshSpawnpoints()
		{
			if (this._spawnpointsCache != null)
			{
				this._spawnpointsCache.Clear();
			}
			else
			{
				this._spawnpointsCache = new List<BoundsRoleSpawnpoint>();
			}
			this._lastSeed = new int?(SeedSynchronizer.Seed);
			foreach (RoomIdentifier roomIdentifier in RoomUtils.FindRooms(this._fName, this._fZone, this._fShape))
			{
				if (!RoomRoleSpawnpoint.ExcludedRooms.Contains(roomIdentifier.Name))
				{
					Transform transform = roomIdentifier.transform;
					Bounds bounds = new Bounds(transform.TransformPoint(this._localPoint), transform.rotation * new Vector3(this._width, 0f, this._length));
					Vector3 vector = transform.rotation * new Vector3((float)this._wNum, 0f, (float)this._lNum);
					Vector3Int vector3Int = new Vector3Int(Mathf.RoundToInt(Mathf.Abs(vector.x)), 1, Mathf.RoundToInt(Mathf.Abs(vector.z)));
					float num = transform.rotation.eulerAngles.y + this._lookAngle;
					this._spawnpointsCache.Add(new BoundsRoleSpawnpoint(bounds, num - this._angleVar, num + this._angleVar, vector3Int));
				}
			}
			this.FilterSpawnpointsByDistance();
		}

		private List<BoundsRoleSpawnpoint> _spawnpointsCache;

		private int? _lastSeed;

		[SerializeField]
		private RoomName _fName;

		[SerializeField]
		private FacilityZone _fZone;

		[SerializeField]
		private RoomShape _fShape;

		[SerializeField]
		private Vector3 _localPoint;

		[SerializeField]
		private float _lookAngle;

		[SerializeField]
		private float _angleVar;

		[SerializeField]
		private float _width;

		[SerializeField]
		private float _length;

		[SerializeField]
		private int _wNum;

		[SerializeField]
		private int _lNum;

		[Tooltip("Specify room types that the chosen spawnpoint should strive to be a a certain meters distance from. This filter will be ignored if it filters out all room instances available.")]
		[SerializeField]
		private RoomName[] _requireDistanceFrom = new RoomName[0];

		[Tooltip("Set this to more than 0 if there are multiple instances of this room, and only ones further away from all rooms in _requireDistanceFrom than <this value> in meters should be valid spawnpoints. This filter will be ignored if it filters out all room instances available.")]
		[SerializeField]
		private int _distanceRequiredMeters;

		private static readonly RoomName[] ExcludedRooms = new RoomName[] { RoomName.HczCheckpointToEntranceZone };
	}
}
