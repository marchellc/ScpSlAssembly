using System;
using System.Diagnostics;
using PlayerRoles.Subroutines;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.HUDs;

[Serializable]
public class AbilityHud
{
	[Serializable]
	private class GraphicColor
	{
		public Color StartingColor;

		public Graphic Graphic;

		private GraphicColor(Graphic graphic)
		{
			this.Graphic = graphic;
			this.StartingColor = graphic.color;
		}
	}

	[SerializeField]
	private GameObject _parent;

	[SerializeField]
	private CanvasGroup _fader;

	[SerializeField]
	private Image _durationCircle;

	[SerializeField]
	private Image _cooldownCircle;

	[SerializeField]
	private bool _inverseDuration;

	[SerializeField]
	private bool _inverseCooldown;

	[SerializeField]
	private bool _showDurationAtCooldown;

	[SerializeField]
	private Vector3 _startScale = Vector3.one;

	[SerializeField]
	private float _flashDuration;

	[SerializeField]
	private float _flashSpeed;

	[SerializeField]
	private GraphicColor[] _graphics;

	private readonly Stopwatch _flashStopwatch = new Stopwatch();

	private float _flashIdleTime;

	private IAbilityCooldown _cooldown;

	private IAbilityCooldown _duration;

	private RectTransform _rt;

	private bool _hasDuration;

	private bool _hasFader;

	private readonly Stopwatch _fullStopwatch = new Stopwatch();

	private const float MinFullTime = 0.3f;

	private const float FadeSpeed = 8.5f;

	public void Setup(IAbilityCooldown cd, IAbilityCooldown duration)
	{
		this._cooldown = cd;
		this._duration = duration;
		this._rt = this._cooldownCircle.rectTransform;
		this._hasDuration = this._duration != null;
		this._hasFader = this._fader != null;
		bool flag = this.UpdateVisibility();
		if (this._hasFader)
		{
			this._fader.alpha = (flag ? 1 : 0);
		}
		else
		{
			this._parent.SetActive(flag);
		}
	}

	public void Update(bool forceHidden = false)
	{
		bool flag = !forceHidden && this.UpdateVisibility();
		if (this._hasFader)
		{
			this._fader.alpha = Mathf.Clamp01(this._fader.alpha + Time.deltaTime * (flag ? 8.5f : (-8.5f)));
		}
		else
		{
			this._parent.SetActive(flag);
		}
	}

	private bool UpdateVisibility()
	{
		bool result = this.UpdateCooldown();
		if (this._hasDuration && this.UpdateDuration())
		{
			result = true;
		}
		return result;
	}

	private bool UpdateCooldown()
	{
		float t = this.FillCircle(this._cooldown, this._cooldownCircle, this._inverseCooldown);
		this._rt.localScale = Vector3.Lerp(this._startScale, Vector3.one, t);
		return !this._cooldown.IsReady;
	}

	private bool UpdateDuration()
	{
		if (this._duration.IsReady)
		{
			this._durationCircle.enabled = false;
			if (this._fullStopwatch.IsRunning)
			{
				return this._fullStopwatch.Elapsed.TotalSeconds < 0.30000001192092896;
			}
			return false;
		}
		this._durationCircle.enabled = this._showDurationAtCooldown || this._cooldown.IsReady;
		this.FillCircle(this._duration, this._durationCircle, !this._inverseDuration);
		this._fullStopwatch.Restart();
		return true;
	}

	private float FillCircle(IAbilityCooldown cd, Image circle, bool inverse)
	{
		float num = cd.Readiness;
		if (inverse)
		{
			num = 1f - num;
		}
		circle.fillAmount = num;
		return num;
	}

	public void LateUpdate()
	{
		this._graphics.ForEach(delegate(GraphicColor x)
		{
			this.UpdateFlash(x.Graphic, this._flashStopwatch, x.StartingColor, ref this._flashIdleTime);
		});
	}

	public void FlashAbility()
	{
		this._flashStopwatch.Restart();
	}

	private void UpdateFlash(Graphic targetGraphic, Stopwatch sw, Color normalColor, ref float idleTime)
	{
		Color color;
		if (sw.IsRunning && sw.Elapsed.TotalSeconds < (double)this._flashDuration)
		{
			float f = Mathf.Sin((Time.timeSinceLevelLoad - idleTime) * this._flashSpeed * MathF.PI);
			color = Color.Lerp(normalColor, Color.red, Mathf.Abs(f));
		}
		else
		{
			color = Color.Lerp(targetGraphic.color, normalColor, Time.deltaTime * this._flashSpeed);
			idleTime = Time.timeSinceLevelLoad;
		}
		targetGraphic.color = new Color(color.r, color.g, color.b, targetGraphic.color.a);
	}
}
