using System;
using System.Diagnostics;
using TMPro;
using UnityEngine;

namespace PlayerRoles.PlayableScps.HUDs
{
	public class ScpWarningHud : MonoBehaviour
	{
		public float Alpha
		{
			get
			{
				return this._alpha;
			}
			private set
			{
				value = Mathf.Clamp01(value);
				if (this._alpha == value)
				{
					return;
				}
				this._alpha = value;
				this._text.alpha = value;
			}
		}

		private void Awake()
		{
			this._text.alpha = this.Alpha;
		}

		private void Update()
		{
			if (this._elapsed.Elapsed.TotalSeconds <= (double)this._duration && !this._dirty)
			{
				this.Alpha += Time.deltaTime * 8f;
				return;
			}
			this.Alpha -= Time.deltaTime * 8f;
			if (this.Alpha > 0f || !this._dirty)
			{
				return;
			}
			this._text.text = this._targetText;
			this._dirty = false;
		}

		public void SetText(string text, float duration = 3.8f)
		{
			this._dirty |= this._targetText != text;
			this._targetText = text;
			this._duration = duration;
			this._elapsed.Restart();
		}

		[SerializeField]
		private TextMeshProUGUI _text;

		private const float FadeSpeed = 8f;

		private const float DefaultTime = 3.8f;

		private float _duration;

		private string _targetText;

		private float _alpha;

		private bool _dirty;

		private readonly Stopwatch _elapsed = new Stopwatch();
	}
}
