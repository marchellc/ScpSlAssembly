using System;
using System.Collections.Generic;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079;
using UnityEngine;
using UserSettings;
using UserSettings.AudioSettings;

public class AnnouncementSource : MonoBehaviour
{
	[Serializable]
	private class SourceSettingsPair
	{
		public AudioSource[] Sources;

		public AnimationCurve VolumeOverNormalizedDistance;

		public AnimationCurve SpatialBlendOverNormalizedDistance;

		private float[] _volumeMultipliers;

		public void Init()
		{
			this._volumeMultipliers = new float[this.Sources.Length];
			for (int i = 0; i < this.Sources.Length; i++)
			{
				this._volumeMultipliers[i] = 1f;
			}
		}

		public void SetVolumeScale(AudioSource target, float scale)
		{
			for (int i = 0; i < this.Sources.Length; i++)
			{
				AudioSource audioSource = this.Sources[i];
				if ((object)target == audioSource)
				{
					this._volumeMultipliers[i] = scale;
					break;
				}
			}
		}

		public void SetDistance(float normalizedDistance)
		{
			float num = this.VolumeOverNormalizedDistance.Evaluate(normalizedDistance);
			float num2 = this.SpatialBlendOverNormalizedDistance.Evaluate(normalizedDistance);
			float spread = 360f * (1f - num2);
			for (int i = 0; i < this.Sources.Length; i++)
			{
				AudioSource obj = this.Sources[i];
				float num3 = this._volumeMultipliers[i];
				obj.spatialBlend = num2;
				obj.spread = spread;
				obj.volume = num * num3;
			}
		}
	}

	private static readonly List<Vector3> AllSpeakerPositions = new List<Vector3>();

	private static readonly List<Vector3> NearbyPositions = new List<Vector3>();

	private static readonly CachedUserSetting<bool> SpatializationSetting = new CachedUserSetting<bool>(OtherAudioSetting.SpatialAnnouncements);

	private static AnnouncementSource _singleton;

	private Transform _tr;

	[SerializeField]
	private float _2dDistance;

	[SerializeField]
	private SourceSettingsPair[] _sources;

	private static Vector3 LastCamPos => MainCameraController.LastPosition;

	private void Awake()
	{
		this._tr = base.transform;
		AnnouncementSource._singleton = this;
		MainCameraController.OnUpdated += OnCamPosUpdated;
		SourceSettingsPair[] sources = AnnouncementSource._singleton._sources;
		for (int i = 0; i < sources.Length; i++)
		{
			sources[i].Init();
		}
	}

	private void OnDestroy()
	{
		if ((object)AnnouncementSource._singleton == this)
		{
			AnnouncementSource._singleton = null;
		}
		MainCameraController.OnUpdated -= OnCamPosUpdated;
	}

	private void OnCamPosUpdated()
	{
		if (!AnnouncementSource.SpatializationSetting.Value)
		{
			this._sources.ForEach(delegate(SourceSettingsPair x)
			{
				x.SetDistance(1f);
			});
			return;
		}
		this.UpdateNearby();
		Vector3 sourcePos;
		float normalizedDistance;
		switch (AnnouncementSource.NearbyPositions.Count)
		{
		case 0:
			sourcePos = default(Vector3);
			normalizedDistance = 1f;
			break;
		case 1:
			sourcePos = AnnouncementSource.NearbyPositions[0];
			normalizedDistance = this.GetNormalizedDistanceToCam(sourcePos);
			break;
		default:
			this.ResolveMultipleNearbySources(out sourcePos, out normalizedDistance);
			break;
		}
		this.SetSources(sourcePos, normalizedDistance);
	}

	private void UpdateNearby()
	{
		AnnouncementSource.NearbyPositions.Clear();
		float num = this._2dDistance * this._2dDistance;
		Vector3 lastCamPos = AnnouncementSource.LastCamPos;
		foreach (Vector3 allSpeakerPosition in AnnouncementSource.AllSpeakerPositions)
		{
			if (!((allSpeakerPosition - lastCamPos).sqrMagnitude > num))
			{
				AnnouncementSource.NearbyPositions.Add(allSpeakerPosition);
			}
		}
	}

	private void ResolveMultipleNearbySources(out Vector3 sourcePos, out float normalizedDistance)
	{
		Vector3 lastCamPos = AnnouncementSource.LastCamPos;
		int count = AnnouncementSource.NearbyPositions.Count;
		Vector3 zero = Vector3.zero;
		float num = float.MaxValue;
		float num2 = 0f;
		for (int i = 0; i < count; i++)
		{
			Vector3 vector = AnnouncementSource.NearbyPositions[i];
			float normalizedDistanceToCam = this.GetNormalizedDistanceToCam(vector);
			num = Mathf.Min(num, normalizedDistanceToCam);
			float num3 = 1f - normalizedDistanceToCam * normalizedDistanceToCam;
			num2 += num3;
			zero += (vector - lastCamPos) * num3;
		}
		if (num2 < 0.01f)
		{
			sourcePos = default(Vector3);
			normalizedDistance = 1f;
		}
		else
		{
			Vector3 vector2 = zero / num2;
			sourcePos = vector2 + lastCamPos;
			normalizedDistance = num;
		}
	}

	private void SetSources(Vector3 sourcePos, float normalizedDistance)
	{
		this._tr.position = sourcePos;
		SourceSettingsPair[] sources = this._sources;
		for (int i = 0; i < sources.Length; i++)
		{
			sources[i].SetDistance(normalizedDistance);
		}
	}

	private float GetNormalizedDistanceToCam(Vector3 pos)
	{
		return Mathf.Clamp01(Vector3.Distance(AnnouncementSource.LastCamPos, pos) / this._2dDistance);
	}

	[RuntimeInitializeOnLoadMethod]
	private static void Init()
	{
		SeedSynchronizer.OnGenerationFinished += RefreshPositions;
	}

	private static void RefreshPositions()
	{
		AnnouncementSource.AllSpeakerPositions.Clear();
		foreach (Scp079InteractableBase allInstance in Scp079InteractableBase.AllInstances)
		{
			if (allInstance is Scp079Speaker)
			{
				AnnouncementSource.AllSpeakerPositions.Add(allInstance.Position);
			}
		}
	}

	public static void SetVolumeScale(AudioSource target, float scale)
	{
		if ((object)AnnouncementSource._singleton != null)
		{
			SourceSettingsPair[] sources = AnnouncementSource._singleton._sources;
			for (int i = 0; i < sources.Length; i++)
			{
				sources[i].SetVolumeScale(target, scale);
			}
		}
	}
}
