using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class MimicryTooltipPopup : MonoBehaviour
	{
		private void Awake()
		{
			this._textLayout = this._text.GetComponent<LayoutElement>();
		}

		private void Update()
		{
			Scp939HudTranslation scp939HudTranslation;
			if (!MimicryTooltipTarget.TryGetHint(out scp939HudTranslation))
			{
				this._root.gameObject.SetActive(false);
				return;
			}
			this._root.gameObject.SetActive(true);
			float scaleFactor = MimicryMenuController.ScaleFactor;
			float num = 1f / scaleFactor;
			float num2 = Input.mousePosition.x * num;
			float num3 = (Mathf.Min(this._text.preferredWidth, this._textLayout.preferredWidth) + this._panning) / 2f;
			float num4 = (float)Screen.width * num;
			float num5 = Mathf.Max(0f, num2 + num3 - num4);
			num5 = Mathf.Min(num5, num2 - num3);
			num2 -= num5;
			num2 *= scaleFactor;
			this._arrowOffset.localPosition = Vector3.right * num5;
			base.transform.position = new Vector3(num2, Input.mousePosition.y);
			if (this._prevHint == scp939HudTranslation)
			{
				return;
			}
			this._prevHint = scp939HudTranslation;
			this._text.text = Translations.Get<Scp939HudTranslation>(scp939HudTranslation);
		}

		private void LateUpdate()
		{
			this._textLayout.enabled = this._text.preferredWidth >= this._textLayout.preferredWidth;
		}

		[SerializeField]
		private TMP_Text _text;

		[SerializeField]
		private RectTransform _root;

		[SerializeField]
		private RectTransform _arrowOffset;

		[SerializeField]
		private float _panning;

		private Scp939HudTranslation _prevHint;

		private LayoutElement _textLayout;
	}
}
