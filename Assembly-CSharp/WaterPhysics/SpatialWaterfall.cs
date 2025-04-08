using System;
using System.Collections.Generic;
using Interactables.Interobjects.DoorUtils;
using MapGeneration;
using UnityEngine;

namespace WaterPhysics
{
	public class SpatialWaterfall : MonoBehaviour
	{
		private void Start()
		{
			this._doorsHs = DoorVariant.DoorsByRoom.GetOrAdd(this._targetRoom, () => new HashSet<DoorVariant>());
			Vector3 position = base.transform.position;
			this._boundsSilent = new Bounds(position, new Vector3(this._areaSilent, 30f, this._areaSilent));
			this._boundsFullVolume = new Bounds(position, new Vector3(this._areaFullVolume, 30f, this._areaFullVolume));
			float num = (this._areaSilent - this._areaFullVolume) / 2f;
			this._fadeSqrDistance = num * num;
		}

		private void Update()
		{
			float num;
			float num2;
			this.CalculateTargetVolumes(out num, out num2);
			this.MoveTowardsVolume(ref this._prevMuffledVolume, num, new Action<float>(this.SetVolumeMuffled));
			this.MoveTowardsVolume(ref this._prevNormalVolume, num2, new Action<float>(this.SetVolumeNormal));
		}

		private void MoveTowardsVolume(ref float prev, float target, Action<float> processor)
		{
			if (target == prev)
			{
				return;
			}
			float num = Mathf.MoveTowards(prev, target, Time.deltaTime * 10f);
			processor(num);
			prev = num;
		}

		private void SetVolumeNormal(float f)
		{
			foreach (AudioSource audioSource in this._normalSources)
			{
				this.SetSrcVolume(audioSource, f);
			}
		}

		private void SetVolumeMuffled(float f)
		{
			this.SetSrcVolume(this._muffledSource, f);
		}

		private void SetSrcVolume(AudioSource src, float vol)
		{
			src.volume = vol;
			src.enabled = vol > 0f;
		}

		private void CalculateTargetVolumes(out float muffledVolume, out float normalVolume)
		{
			Vector3 lastPosition = MainCameraController.LastPosition;
			if (!this._boundsSilent.Contains(lastPosition))
			{
				muffledVolume = 0f;
				normalVolume = 0f;
				return;
			}
			if (this._boundsFullVolume.Contains(lastPosition))
			{
				muffledVolume = 0f;
				normalVolume = 1f;
				return;
			}
			this.ValidateDoorCache();
			int? num = null;
			float num2 = float.MaxValue;
			for (int i = 0; i < this._prevHsCnt; i++)
			{
				float sqrMagnitude = (this._doorPositions[i] - lastPosition).sqrMagnitude;
				if (sqrMagnitude <= num2)
				{
					num2 = sqrMagnitude;
					num = new int?(i);
				}
			}
			if (num == null)
			{
				muffledVolume = 1f;
				normalVolume = 0f;
				return;
			}
			float exactState = this._doors[num.Value].GetExactState();
			float num3 = Mathf.Clamp01(this._boundsFullVolume.SqrDistance(lastPosition) / this._fadeSqrDistance);
			normalVolume = exactState * (1f - num3);
			muffledVolume = 1f - normalVolume;
		}

		private void ValidateDoorCache()
		{
			int count = this._doorsHs.Count;
			if (this._prevHsCnt == count)
			{
				return;
			}
			this._doorPositions = new Vector3[count];
			this._doors = new DoorVariant[count];
			int num = 0;
			foreach (DoorVariant doorVariant in this._doorsHs)
			{
				this._doors[num] = doorVariant;
				this._doorPositions[num] = doorVariant.transform.position;
				num++;
			}
			this._prevHsCnt = count;
		}

		private void OnDrawGizmosSelected()
		{
			Gizmos.color = Color.blue;
			Gizmos.DrawWireCube(base.transform.position, Vector3.one * this._areaFullVolume);
			Gizmos.color = Color.cyan;
			Gizmos.DrawWireCube(base.transform.position, Vector3.one * this._areaSilent);
		}

		[SerializeField]
		private float _areaFullVolume;

		[SerializeField]
		private float _areaSilent;

		[SerializeField]
		private RoomIdentifier _targetRoom;

		[SerializeField]
		private AudioSource[] _normalSources;

		[SerializeField]
		private AudioSource _muffledSource;

		private HashSet<DoorVariant> _doorsHs;

		private int _prevHsCnt;

		private float _fadeSqrDistance;

		private float _prevNormalVolume;

		private float _prevMuffledVolume;

		private Vector3[] _doorPositions;

		private DoorVariant[] _doors;

		private Bounds _boundsSilent;

		private Bounds _boundsFullVolume;

		private const float VolumeAdjustSpeed = 10f;

		private const float BoundsHeight = 30f;
	}
}
