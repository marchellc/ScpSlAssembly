using System;
using MapGeneration;
using UnityEngine;
using UnityEngine.Rendering;
using Utils.NonAllocLINQ;

public class ZonePostProcessing : MonoBehaviour
{
	[Serializable]
	private struct ZoneVolumePair
	{
		public FacilityZone Zone;

		[SerializeField]
		private Volume _volume;

		public void SetWeight(float weight)
		{
			if (weight > 0f)
			{
				this._volume.enabled = true;
				this._volume.weight = Mathf.Clamp01(weight);
			}
			else
			{
				this._volume.enabled = false;
			}
		}
	}

	[SerializeField]
	private ZoneVolumePair[] _zoneVols;

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

	private void Start()
	{
		SeedSynchronizer.OnGenerationFinished += Initalize;
		if (SeedSynchronizer.MapGenerated)
		{
			this.Initalize();
		}
	}

	private void OnDestroy()
	{
		SeedSynchronizer.OnGenerationFinished -= Initalize;
		MainCameraController.OnUpdated -= UpdateWeights;
	}

	private void UpdateWeights()
	{
		Vector3 lastPosition = MainCameraController.LastPosition;
		if (lastPosition.TryGetRoom(out var room) && room != this._lastDetectedRoom)
		{
			if (this.CheckWhitelisted(room))
			{
				this._lastWhitelistedRoom = room;
				this._lastBounds = this.GetRoomBounds(room);
				this._lastZone = room.Zone;
			}
			this._lastDetectedRoom = room;
		}
		ZoneVolumePair[] zoneVols;
		if (this._lastWhitelistedRoom == room)
		{
			zoneVols = this._zoneVols;
			for (int i = 0; i < zoneVols.Length; i++)
			{
				ZoneVolumePair zoneVolumePair = zoneVols[i];
				zoneVolumePair.SetWeight((zoneVolumePair.Zone == this._lastZone) ? 1 : 0);
			}
			return;
		}
		FacilityZone facilityZone = FacilityZone.None;
		float num = this._maxTransitionDis * this._maxTransitionDis;
		zoneVols = this._zoneVols;
		for (int i = 0; i < zoneVols.Length; i++)
		{
			ZoneVolumePair zoneVolumePair2 = zoneVols[i];
			if (zoneVolumePair2.Zone != this._lastZone)
			{
				float num2 = this._boundsPerZone[(int)zoneVolumePair2.Zone].SqrDistance(lastPosition);
				if (!(num2 >= num))
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
		float num4 = Mathf.Sqrt(this._lastBounds.SqrDistance(lastPosition));
		float num5 = Mathf.Min(this._maxTransitionDis, num3 + num4);
		float num6 = Mathf.Clamp01(num3 / num5);
		zoneVols = this._zoneVols;
		for (int i = 0; i < zoneVols.Length; i++)
		{
			ZoneVolumePair zoneVolumePair3 = zoneVols[i];
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
		if (!this._initalized)
		{
			this._initalized = true;
			this._whitelistedZones = new FacilityZone[this._zoneVols.Length];
			for (int i = 0; i < this._zoneVols.Length; i++)
			{
				this._whitelistedZones[i] = this._zoneVols[i].Zone;
			}
			this.GenerateZoneBounds();
			MainCameraController.OnUpdated += UpdateWeights;
		}
	}

	private void GenerateZoneBounds()
	{
		int maxValue = 0;
		this._zoneVols.ForEach(delegate(ZoneVolumePair x)
		{
			maxValue = Mathf.Max(maxValue, (int)x.Zone);
		});
		int num = maxValue + 1;
		this._boundsSet = new bool[num];
		this._boundsPerZone = new Bounds[num];
		this._boundsSize = Vector3.Scale(RoomIdentifier.GridScale, new Vector3(1f, this._heightMultiplier, 1f));
		RoomIdentifier.AllRoomIdentifiers.ForEach(AddRoomToBounds);
	}

	private void AddRoomToBounds(RoomIdentifier room)
	{
		if (this.CheckWhitelisted(room))
		{
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
	}

	private bool CheckWhitelisted(RoomIdentifier room)
	{
		if (!this._excludedRooms.Contains(room.Name))
		{
			return this._whitelistedZones.Contains(room.Zone);
		}
		return false;
	}

	private Bounds GetRoomBounds(RoomIdentifier room)
	{
		return new Bounds(room.transform.position, this._boundsSize);
	}
}
