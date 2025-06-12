using UnityEngine;

namespace CustomRendering;

public class FogSetting : MonoBehaviour
{
	private float _weight;

	public FogType FogType;

	public int Priority;

	public float StartDistance;

	public float EndDistance = 200f;

	[ColorUsage(false, true)]
	public Color Color = Color.black;

	private bool _isDirty;

	private bool _state;

	public bool IsEnabled
	{
		get
		{
			return this._state;
		}
		internal set
		{
			if (this._state != value)
			{
				this._state = value;
				this._isDirty = true;
			}
		}
	}

	public float BlendTime { get; internal set; }

	public float Weight
	{
		get
		{
			if (!FogController.Singleton.ForcedFog.HasValue)
			{
				return this._weight;
			}
			if (FogController.Singleton.ForcedFog.Value != this.FogType)
			{
				return 0f;
			}
			return 1f;
		}
		private set
		{
			this._weight = value;
		}
	}

	public void UpdateWeight()
	{
		if (!this._isDirty)
		{
			return;
		}
		if (this.BlendTime <= 0f)
		{
			this.Weight = (this.IsEnabled ? 1 : 0);
			this._isDirty = false;
			return;
		}
		float num = 1f / this.BlendTime;
		this.Weight += Time.deltaTime * (this.IsEnabled ? num : (0f - num));
		if (this.Weight < 0f || this.Weight > 1f)
		{
			this.Weight = Mathf.Clamp01(this.Weight);
			this._isDirty = false;
		}
	}
}
