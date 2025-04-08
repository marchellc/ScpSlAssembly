using System;
using InventorySystem.Items.Firearms;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Crosshairs
{
	public class BuckshotCrosshair : FirearmCrosshairBase
	{
		private void SetupElements(float innerAngle, float buckshotRadius)
		{
			foreach (RectTransform rectTransform in this._elements)
			{
				Vector2 vector = this._displacementRatio * innerAngle * Vector2.up;
				rectTransform.anchoredPosition = Vector2.Lerp(rectTransform.anchoredPosition, vector, Time.deltaTime * this._lerpSpeed);
				rectTransform.sizeDelta = this._radiusRatio * buckshotRadius * Vector2.one;
			}
		}

		protected override void UpdateCrosshair(Firearm firearm, float currentInaccuracy)
		{
			BuckshotHitreg buckshotHitreg;
			if (!firearm.TryGetModule(out buckshotHitreg, false))
			{
				return;
			}
			this.SetupElements(currentInaccuracy, buckshotHitreg.BuckshotScale);
		}

		[SerializeField]
		private RectTransform[] _elements;

		[SerializeField]
		private float _displacementRatio;

		[SerializeField]
		private float _radiusRatio;

		[SerializeField]
		private float _lerpSpeed;
	}
}
