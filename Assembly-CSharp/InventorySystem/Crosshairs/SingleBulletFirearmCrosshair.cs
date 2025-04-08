using System;
using InventorySystem.Items.Firearms;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace InventorySystem.Crosshairs
{
	public class SingleBulletFirearmCrosshair : FirearmCrosshairBase
	{
		private void SetupElements(float innerAngle, float speed, bool forceLerp)
		{
			float num = (forceLerp ? 1f : (Time.deltaTime * this._lerpSpeed));
			Vector2 vector = this._sizeRatio * innerAngle * Vector2.left;
			Vector3 vector2 = new Vector3(this._sizeOverSpeed.Evaluate(speed), this._width);
			foreach (RectTransform rectTransform in this._elements)
			{
				rectTransform.sizeDelta = Vector2.Lerp(rectTransform.sizeDelta, vector2, num);
				rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, vector, num);
			}
		}

		private void OnEnable()
		{
			this.SetupElements(0f, 20f, true);
		}

		protected override void UpdateCrosshair(Firearm firearm, float currentInaccuracy)
		{
			float num = firearm.Owner.GetVelocity().MagnitudeIgnoreY();
			this.SetupElements(currentInaccuracy, num, false);
		}

		[SerializeField]
		private RectTransform[] _elements;

		[SerializeField]
		private float _sizeRatio;

		[SerializeField]
		private float _width;

		[SerializeField]
		private float _lerpSpeed;

		[SerializeField]
		private AnimationCurve _sizeOverSpeed;
	}
}
