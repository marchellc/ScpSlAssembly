using GameObjectPools;
using UnityEngine;

namespace InventorySystem.Items.ToggleableLights.Lantern;

public class LanternLightManager : MonoBehaviour, IPoolResettable
{
	private const float MinSquaredDistanceMovedToProduceSound = 900f;

	private static readonly int EmissionId = Shader.PropertyToID("_EmissionColor");

	private MaterialPropertyBlock _mpb;

	public Light[] Lights;

	public Renderer MainRenderer;

	public Color EmissionColor;

	public ParticleSystem ParticleSystem;

	public float LightSpeed = 4f;

	public AudioClip ForwardSound;

	public AudioClip BackwardSound;

	public Rigidbody SoundTriggerRb;

	public float Forward;

	public float Backward;

	public float ForwardVolume = 0.8f;

	public float BackwardVolume = 0.8f;

	public float VolumeModifier = 0.2f;

	private float _previousRot;

	[SerializeField]
	private AudioSource _forwardSound;

	[SerializeField]
	private AudioSource _backwardSound;

	private LightBlink _blinker;

	private float[] _lightRanges;

	private float _enableRatio;

	private bool _initialized;

	private Vector3 _oldPosition;

	public bool IsEnabled { get; private set; } = true;

	private float AngularMag => this.SoundTriggerRb.angularVelocity.magnitude * this.VolumeModifier;

	private void AudioUpdate()
	{
		float x = this.SoundTriggerRb.transform.localRotation.x;
		bool flag = x > this._previousRot;
		if (flag && !this._forwardSound.isPlaying && x >= this.Forward && this._previousRot <= this.Forward)
		{
			this._forwardSound.volume = this.AngularMag * this.ForwardVolume;
			this._forwardSound.Play();
		}
		else if (!flag && !this._backwardSound.isPlaying && x <= this.Backward && this._previousRot >= this.Backward)
		{
			this._backwardSound.volume = this.AngularMag * this.BackwardVolume;
			this._backwardSound.Play();
		}
		this._previousRot = x;
	}

	private void Awake()
	{
		if (!this._initialized)
		{
			this._blinker = base.GetComponent<LightBlink>();
			this._mpb = new MaterialPropertyBlock();
			this.MainRenderer.GetPropertyBlock(this._mpb, 1);
			this._lightRanges = new float[this.Lights.Length];
			for (int i = 0; i < this.Lights.Length; i++)
			{
				this._lightRanges[i] = this.Lights[i].range;
			}
			this._initialized = true;
			this.ResetObject();
		}
	}

	public void SetLight(bool isEnabled)
	{
		if (!this._initialized)
		{
			this.Awake();
		}
		if (this.IsEnabled != isEnabled && base.enabled)
		{
			this.IsEnabled = isEnabled;
			this._blinker.enabled = isEnabled;
			if (isEnabled)
			{
				this.ParticleSystem.Play();
			}
			else
			{
				this.ParticleSystem.Stop();
			}
		}
	}

	private void Update()
	{
		Vector3 position = base.transform.position;
		Vector3 force = this._oldPosition - position;
		if (force.sqrMagnitude < 900f)
		{
			this.SoundTriggerRb.AddForce(force, ForceMode.VelocityChange);
		}
		this._oldPosition = position;
		this.AudioUpdate();
		if ((this.IsEnabled && !(this._enableRatio >= 1f)) || (!this.IsEnabled && !(this._enableRatio <= 0f)))
		{
			this._enableRatio += (this.IsEnabled ? Time.deltaTime : (0f - Time.deltaTime)) * this.LightSpeed;
			for (int i = 0; i < this.Lights.Length; i++)
			{
				this.Lights[i].range = Mathf.Lerp(0f, this._lightRanges[i], this._enableRatio);
			}
			this._mpb.SetColor(LanternLightManager.EmissionId, Color.Lerp(Color.black, this.EmissionColor, this._enableRatio));
			this.MainRenderer.SetPropertyBlock(this._mpb, 1);
		}
	}

	private void OnEnable()
	{
		if (this.IsEnabled)
		{
			this.ParticleSystem.Play();
		}
	}

	private void OnDisable()
	{
		if (this.IsEnabled)
		{
			this.ParticleSystem.Stop();
		}
	}

	public void ResetObject()
	{
		if (this.IsEnabled)
		{
			this._blinker.enabled = false;
			this._enableRatio = 0f;
			this.IsEnabled = false;
			Light[] lights = this.Lights;
			for (int i = 0; i < lights.Length; i++)
			{
				lights[i].range = 0f;
			}
			this._mpb.SetColor(LanternLightManager.EmissionId, Color.black);
			this.MainRenderer.SetPropertyBlock(this._mpb, 1);
			this.ParticleSystem.Stop();
		}
	}
}
