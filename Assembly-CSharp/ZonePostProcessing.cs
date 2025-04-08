using System;
using MapGeneration;
using UnityEngine;
using UnityEngine.Rendering;
using Utils.NonAllocLINQ;

public class ZonePostProcessing : MonoBehaviour
{
	private void Start()
	{
		SeedSynchronizer.OnGenerationFinished += this.Initalize;
		if (!SeedSynchronizer.MapGenerated)
		{
			return;
		}
		this.Initalize();
	}

	private void OnDestroy()
	{
		SeedSynchronizer.OnGenerationFinished -= this.Initalize;
		MainCameraController.OnUpdated -= this.UpdateWeights;
	}

	private void UpdateWeights()
	{
		Vector3 position = MainCameraController.CurrentCamera.position;
		RoomIdentifier roomIdentifier = RoomUtils.RoomAtPositionRaycasts(position, true);
		if (roomIdentifier != null && roomIdentifier != this._lastDetectedRoom)
		{
			if (this.CheckWhitelisted(roomIdentifier))
			{
				this._lastWhitelistedRoom = roomIdentifier;
				this._lastBounds = this.GetRoomBounds(roomIdentifier);
				this._lastZone = roomIdentifier.Zone;
			}
			this._lastDetectedRoom = roomIdentifier;
		}
		if (this._lastWhitelistedRoom == roomIdentifier)
		{
			foreach (ZonePostProcessing.ZoneVolumePair zoneVolumePair in this._zoneVols)
			{
				zoneVolumePair.SetWeight((float)((zoneVolumePair.Zone == this._lastZone) ? 1 : 0));
			}
			return;
		}
		FacilityZone facilityZone = FacilityZone.None;
		float num = this._maxTransitionDis * this._maxTransitionDis;
		foreach (ZonePostProcessing.ZoneVolumePair zoneVolumePair2 in this._zoneVols)
		{
			if (zoneVolumePair2.Zone != this._lastZone)
			{
				float num2 = this._boundsPerZone[(int)zoneVolumePair2.Zone].SqrDistance(position);
				if (num2 < num)
				{
					facilityZone = zoneVolumePair2.Zone;
					num = num2;
				}
			}
		}
		if (facilityZone == FacilityZone.None)
		{
			return;
		}
		float num3 = Mathf.Sqrt(num);
		float num4 = Mathf.Sqrt(this._lastBounds.SqrDistance(position));
		float num5 = Mathf.Min(this._maxTransitionDis, num3 + num4);
		float num6 = Mathf.Clamp01(num3 / num5);
		foreach (ZonePostProcessing.ZoneVolumePair zoneVolumePair3 in this._zoneVols)
		{
			if (zoneVolumePair3.Zone == this._lastZone)
			{
				zoneVolumePair3.SetWeight(num6);
			}
			else if (zoneVolumePair3.Zone == facilityZone)
			{
				zoneVolumePair3.SetWeight(1f - num6);
			}
			else
			{
				zoneVolumePair3.SetWeight(0f);
			}
		}
	}

	private void Initalize()
	{
		if (this._initalized)
		{
			return;
		}
		this._initalized = true;
		this._whitelistedZones = new FacilityZone[this._zoneVols.Length];
		for (int i = 0; i < this._zoneVols.Length; i++)
		{
			this._whitelistedZones[i] = this._zoneVols[i].Zone;
		}
		this.GenerateZoneBounds();
		MainCameraController.OnUpdated += this.UpdateWeights;
	}

	private void GenerateZoneBounds()
	{
		int maxValue = 0;
		this._zoneVols.ForEach(delegate(ZonePostProcessing.ZoneVolumePair x)
		{
			maxValue = Mathf.Max(maxValue, (int)x.Zone);
		});
		int num = maxValue + 1;
		this._boundsSet = new bool[num];
		this._boundsPerZone = new Bounds[num];
		this._boundsSize = Vector3.Scale(RoomIdentifier.GridScale, new Vector3(1f, this._heightMultiplier, 1f));
		RoomIdentifier.AllRoomIdentifiers.ForEach(new Action<RoomIdentifier>(this.AddRoomToBounds));
	}

	private void AddRoomToBounds(RoomIdentifier room)
	{
		if (!this.CheckWhitelisted(room))
		{
			return;
		}
		int zone = (int)room.Zone;
		Bounds roomBounds = this.GetRoomBounds(room);
		if (this._boundsSet[zone])
		{
			this._boundsPerZone[zone].Encapsulate(roomBounds);
			return;
		}
		this._boundsSet[zone] = true;
		this._boundsPerZone[zone] = roomBounds;
	}

	private bool CheckWhitelisted(RoomIdentifier room)
	{
		return !this._excludedRooms.Contains(room.Name) && this._whitelistedZones.Contains(room.Zone);
	}

	private Bounds GetRoomBounds(RoomIdentifier room)
	{
		Bounds? bounds = null;
		Vector3Int[] occupiedCoords = room.OccupiedCoords;
		for (int i = 0; i < occupiedCoords.Length; i++)
		{
			Vector3 vector = RoomUtils.CoordsToCenterPos(occupiedCoords[i]);
			Bounds bounds2 = new Bounds(vector, this._boundsSize);
			if (bounds != null)
			{
				bounds.Value.Encapsulate(bounds2);
			}
			else
			{
				bounds = new Bounds?(bounds2);
			}
		}
		return bounds.Value;
	}

	[SerializeField]
	private ZonePostProcessing.ZoneVolumePair[] _zoneVols;

	[SerializeField]
	private RoomName[] _excludedRooms;

	[SerializeField]
	private float _maxTransitionDis;

	[SerializeField]
	private float _heightMultiplier;

	private bool _initalized;

	private bool[] _boundsSet;

	private Bounds[] _boundsPerZone;

	private Bounds _lastBounds;

	private FacilityZone _lastZone;

	private RoomIdentifier _lastDetectedRoom;

	private RoomIdentifier _lastWhitelistedRoom;

	private FacilityZone[] _whitelistedZones;

	private Vector3 _boundsSize;

	[Serializable]
	private struct ZoneVolumePair
	{
		public void SetWeight(float weight)
		{
			if (weight > 0f)
			{
				this._volume.enabled = true;
				this._volume.weight = Mathf.Clamp01(weight);
				return;
			}
			this._volume.enabled = false;
		}

		public FacilityZone Zone;

		[SerializeField]
		private Volume _volume;
	}
}
