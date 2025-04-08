using System;
using System.Collections.Generic;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace MapGeneration
{
	public class RoomIdentifier : MonoBehaviour
	{
		public Vector3Int[] OccupiedCoords { get; private set; }

		public HashSet<RoomIdentifier> ConnectedRooms { get; private set; } = new HashSet<RoomIdentifier>();

		public List<RoomLightController> LightControllers { get; private set; } = new List<RoomLightController>(2);

		public static event Action<RoomIdentifier> OnAdded;

		public static event Action<RoomIdentifier> OnRemoved;

		private void Awake()
		{
			RoomIdentifier.AllRoomIdentifiers.Add(this);
			Action<RoomIdentifier> onAdded = RoomIdentifier.OnAdded;
			if (onAdded == null)
			{
				return;
			}
			onAdded(this);
		}

		private void OnDestroy()
		{
			RoomIdentifier.AllRoomIdentifiers.Remove(this);
			Action<RoomIdentifier> onRemoved = RoomIdentifier.OnRemoved;
			if (onRemoved != null)
			{
				onRemoved(this);
			}
			this.LightControllers.Clear();
			if (this.OccupiedCoords == null)
			{
				return;
			}
			foreach (Vector3Int vector3Int in this.OccupiedCoords)
			{
				RoomIdentifier.RoomsByCoordinates.Remove(vector3Int);
			}
		}

		public bool TryAssignId()
		{
			if (!base.gameObject.activeInHierarchy)
			{
				return false;
			}
			Vector3Int vector3Int = RoomUtils.PositionToCoords(base.transform.position);
			RoomIdentifier.RoomsByCoordinates[vector3Int] = this;
			this.OccupiedCoords = new Vector3Int[this.AdditionalZones.Length + 1];
			this.OccupiedCoords[0] = vector3Int;
			int num = 1;
			Vector3Int[] additionalZones = this.AdditionalZones;
			for (int i = 0; i < additionalZones.Length; i++)
			{
				Vector3 vector = Vector3.Scale(additionalZones[i], RoomIdentifier.GridScale);
				vector3Int = RoomUtils.PositionToCoords(base.transform.position + base.transform.TransformDirection(vector));
				RoomIdentifier.RoomsByCoordinates[vector3Int] = this;
				this.OccupiedCoords[num] = vector3Int;
				num++;
			}
			return true;
		}

		public bool TryGetMainCoords(out Vector3Int coords)
		{
			if (this.OccupiedCoords == null || this.OccupiedCoords.Length == 0)
			{
				coords = Vector3Int.zero;
				return false;
			}
			coords = this.OccupiedCoords[0];
			return true;
		}

		public RoomLightController GetClosestLightController(ReferenceHub hub)
		{
			if (!(hub == null))
			{
				IFpcRole fpcRole = hub.roleManager.CurrentRole as IFpcRole;
				if (fpcRole != null)
				{
					if (this.LightControllers.Count == 0)
					{
						return null;
					}
					if (this.LightControllers.Count == 1)
					{
						return this.LightControllers[0];
					}
					Vector3 position = fpcRole.FpcModule.Position;
					float num = float.MaxValue;
					RoomLightController roomLightController = null;
					foreach (RoomLightController roomLightController2 in this.LightControllers)
					{
						float sqrMagnitude = (roomLightController2.transform.position - position).sqrMagnitude;
						if (sqrMagnitude < num)
						{
							num = sqrMagnitude;
							roomLightController = roomLightController2;
						}
					}
					return roomLightController;
				}
			}
			return null;
		}

		public RoomShape Shape;

		public RoomName Name;

		public FacilityZone Zone;

		public Vector3Int[] AdditionalZones;

		public Bounds[] SubBounds;

		public Sprite Icon;

		public static readonly HashSet<RoomIdentifier> AllRoomIdentifiers = new HashSet<RoomIdentifier>();

		public static readonly Dictionary<Vector3Int, RoomIdentifier> RoomsByCoordinates = new Dictionary<Vector3Int, RoomIdentifier>();

		public static readonly Vector3 GridScale = new Vector3(15f, 100f, 15f);
	}
}
