using MapGeneration;
using UnityEngine;

namespace FacilitySoundtrack;

public class PocketDimensionAmbientSoundtrack : SoundtrackLayerBase
{
	[SerializeField]
	private float _fadeInSpeed;

	[SerializeField]
	private float _fadeOutSpeed;

	[SerializeField]
	private AnimationCurve _bassBlendCurve;

	[SerializeField]
	private AnimationCurve _ambienceBlendCurve;

	[SerializeField]
	private float _startBassDistance;

	[SerializeField]
	private float _endBassDistance;

	[SerializeField]
	private AudioSource[] _bassSources;

	[SerializeField]
	private AudioSource _ambienceSource;

	private float _weight;

	public override bool Additive => false;

	public override float Weight => this._weight;

	public override void UpdateVolume(float masterScale)
	{
		Vector3 lastPosition = MainCameraController.LastPosition;
		if (!lastPosition.TryGetRoom(out var room) || room.Name != RoomName.Pocket)
		{
			this._weight = Mathf.Max(0f, this._weight - Time.deltaTime * this._fadeOutSpeed);
			AudioSource[] bassSources = this._bassSources;
			for (int i = 0; i < bassSources.Length; i++)
			{
				bassSources[i].UpdateFadeOut(this._fadeOutSpeed);
			}
			this._ambienceSource.volume = Mathf.Min(this._ambienceSource.volume, masterScale);
			return;
		}
		if (this._weight == 0f)
		{
			AudioSource[] bassSources = this._bassSources;
			for (int i = 0; i < bassSources.Length; i++)
			{
				bassSources[i].Replay();
			}
			this._ambienceSource.Replay();
		}
		float value = (lastPosition - room.transform.position).MagnitudeIgnoreY();
		float time = Mathf.InverseLerp(this._startBassDistance, this._endBassDistance, value);
		float num = this._bassBlendCurve.Evaluate(time);
		this._weight = Mathf.Min(1f, this._weight + this._fadeInSpeed * Time.deltaTime);
		this._ambienceSource.volume = masterScale * this._ambienceBlendCurve.Evaluate(time);
		for (int j = 0; j < this._bassSources.Length; j++)
		{
			float num2 = Mathf.Abs((float)j - num + 1f);
			float num3 = num2 * num2;
			float num4 = Mathf.Clamp01(1f - num3);
			this._bassSources[j].volume = num4 * masterScale;
		}
	}
}
