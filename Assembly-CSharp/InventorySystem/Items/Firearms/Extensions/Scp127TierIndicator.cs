using System.Diagnostics;
using AudioPooling;
using InventorySystem.Items.Firearms.Modules.Scp127;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class Scp127TierIndicator : MonoBehaviour, IViewmodelExtension
{
	private class TierLevel
	{
		private readonly Scp127Tier _tier;

		private readonly int _baseColorHash;

		private readonly int _emissionColorHash;

		private readonly int _sliderHash;

		private const string BaseColorPrefix = "_BaseColor";

		private const string EmissionColorPrefix = "_EmissionColor";

		private const string SliderPrefix = "_Fill";

		private float _lastSlider;

		public TierLevel(Scp127Tier tier)
		{
			this._tier = tier;
			this._baseColorHash = Shader.PropertyToID("_BaseColor" + tier);
			this._emissionColorHash = Shader.PropertyToID("_EmissionColor" + tier);
			this._sliderHash = Shader.PropertyToID("_Fill" + tier);
		}

		public void Update(Scp127Tier tier, float nextProgress, float delta, Color glow)
		{
			Color value;
			bool flag;
			float b;
			if (this._tier <= tier)
			{
				value = Scp127TierIndicator.UnlockedColor;
				flag = true;
				b = 1f;
			}
			else if (this._tier == tier + 1)
			{
				value = Scp127TierIndicator.ProgressColor;
				flag = false;
				b = nextProgress;
			}
			else
			{
				value = Scp127TierIndicator.InactiveColor;
				flag = false;
				b = 0f;
			}
			this._lastSlider = Mathf.Lerp(this._lastSlider, b, delta);
			Scp127TierIndicator._propertyBlock.SetFloat(this._sliderHash, this._lastSlider);
			Scp127TierIndicator._propertyBlock.SetColor(this._baseColorHash, value);
			Scp127TierIndicator._propertyBlock.SetColor(this._emissionColorHash, flag ? glow : Color.black);
		}
	}

	private static readonly Color UnlockedColor = Color.white;

	private static readonly Color ProgressColor = Color.grey;

	private static readonly Color InactiveColor = Color.black;

	private static MaterialPropertyBlock _propertyBlock;

	[SerializeField]
	private Renderer _renderer;

	[SerializeField]
	private AudioClip _levelUpSound;

	[SerializeField]
	private float _lerpSpeed;

	[SerializeField]
	private float _levelUpLength;

	[SerializeField]
	private AnimationCurve _progressCorrectionCurve;

	[GradientUsage(true)]
	[SerializeField]
	private Gradient _levelUpGradient;

	private Firearm _firearm;

	private TierLevel[] _tierLevels;

	private Scp127Tier _lastTier;

	private readonly Stopwatch _levelUpSw = new Stopwatch();

	public void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
	{
		this._firearm = viewmodel.ParentFirearm;
		this._lastTier = Scp127TierManagerModule.GetTierForItem(this._firearm);
		if (Scp127TierIndicator._propertyBlock == null)
		{
			Scp127TierIndicator._propertyBlock = new MaterialPropertyBlock();
			this._renderer.GetPropertyBlock(Scp127TierIndicator._propertyBlock);
		}
		Scp127Tier[] values = EnumUtils<Scp127Tier>.Values;
		this._tierLevels = new TierLevel[values.Length];
		for (int i = 0; i < values.Length; i++)
		{
			this._tierLevels[i] = new TierLevel(values[i]);
		}
		this.UpdateMaterial(instant: true);
	}

	private void Update()
	{
		this.UpdateMaterial(instant: false);
	}

	private void UpdateMaterial(bool instant)
	{
		Scp127TierManagerModule.GetTierAndProgressForItem(this._firearm, out var tier, out var progress);
		double num = this._levelUpSw.Elapsed.TotalSeconds / (double)this._levelUpLength;
		Color glow = this._levelUpGradient.Evaluate((float)num);
		float nextProgress = this._progressCorrectionCurve.Evaluate(progress);
		float delta = (instant ? 1f : (Time.deltaTime * this._lerpSpeed));
		TierLevel[] tierLevels = this._tierLevels;
		for (int i = 0; i < tierLevels.Length; i++)
		{
			tierLevels[i].Update(tier, nextProgress, delta, glow);
		}
		if (this._lastTier != tier)
		{
			this._levelUpSw.Restart();
			AudioSourcePoolManager.Play2DWithParent(this._levelUpSound, this._firearm.transform, 1f, MixerChannel.NoDucking);
			this._lastTier = tier;
		}
		this._renderer.SetPropertyBlock(Scp127TierIndicator._propertyBlock);
	}
}
