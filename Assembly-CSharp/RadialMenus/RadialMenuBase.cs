using System;
using UnityEngine;
using UnityEngine.UI;

namespace RadialMenus
{
	public abstract class RadialMenuBase : MonoBehaviour
	{
		public abstract int Slots { get; }

		public int HighlightedSlot { get; private set; }

		protected Image GetHighlightSafe(int index)
		{
			Image image = this.Highlights[index];
			if (image != null)
			{
				return image;
			}
			image = global::UnityEngine.Object.Instantiate<Image>(this._slotTemplate, this.RingImage.transform);
			this.Highlights[index] = image;
			return image;
		}

		protected virtual void OnSlotsNumberChanged(int prev, int cur)
		{
			for (int i = 0; i < prev; i++)
			{
				this.Highlights[i].enabled = false;
			}
			for (int j = 0; j < cur; j++)
			{
				Image highlightSafe = this.GetHighlightSafe(j);
				highlightSafe.rectTransform.SetAsFirstSibling();
				highlightSafe.rectTransform.localPosition = Vector3.zero;
				highlightSafe.rectTransform.localEulerAngles = this._slotsAngleStep * (float)j * Vector3.back;
				highlightSafe.sprite = this._settings.HighlightTemplates[cur];
				highlightSafe.enabled = true;
			}
			this.RingImage.sprite = this._settings.MainRings[cur];
		}

		protected bool InRingRange(out float angle)
		{
			float num = (float)Screen.width / (float)Screen.height;
			Vector2 vector = new Vector2(Mathf.Lerp(-1f, 1f, Mathf.Clamp01(Input.mousePosition.x / (float)Screen.width)) * num, Mathf.Lerp(-1f, 1f, Input.mousePosition.y / (float)Screen.height));
			angle = Vector2.Angle(Vector2.up, vector.normalized);
			if (vector.x < 0f)
			{
				angle = 360f - angle;
			}
			float magnitude = vector.magnitude;
			return magnitude < this._ringWidth.y && magnitude > this._ringWidth.x;
		}

		protected virtual void Update()
		{
			int num = Mathf.Clamp(this.Slots, 1, this._settings.HighlightTemplates.Length + 1);
			if (num != this._slotsNum)
			{
				int slotsNum = this._slotsNum;
				this._slotsNum = num;
				this._slotsAngleStep = 360f / (float)num;
				this.OnSlotsNumberChanged(slotsNum, num);
			}
			float num2;
			if (this.InRingRange(out num2))
			{
				this.HighlightedSlot = 0;
				while (num2 > this._slotsAngleStep)
				{
					num2 -= this._slotsAngleStep;
					int highlightedSlot = this.HighlightedSlot;
					this.HighlightedSlot = highlightedSlot + 1;
				}
				return;
			}
			this.HighlightedSlot = -1;
		}

		private int _slotsNum;

		private float _slotsAngleStep;

		[SerializeField]
		private RadialMenuSettings _settings;

		[SerializeField]
		private Vector2 _ringWidth = new Vector2(0.41f, 1.1f);

		[SerializeField]
		private Image _slotTemplate;

		[SerializeField]
		protected Image RingImage;

		protected Image[] Highlights = new Image[32];
	}
}
