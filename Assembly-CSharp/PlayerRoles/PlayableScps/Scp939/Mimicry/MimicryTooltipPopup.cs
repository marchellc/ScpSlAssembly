using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

public class MimicryTooltipPopup : MonoBehaviour
{
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

	private void Awake()
	{
		this._textLayout = this._text.GetComponent<LayoutElement>();
	}

	private void Update()
	{
		if (MimicryTooltipTarget.TryGetHint(out var hint))
		{
			this._root.gameObject.SetActive(value: true);
			float scaleFactor = MimicryMenuController.ScaleFactor;
			float num = 1f / scaleFactor;
			float num2 = Input.mousePosition.x * num;
			float num3 = (Mathf.Min(this._text.preferredWidth, this._textLayout.preferredWidth) + this._panning) / 2f;
			float num4 = (float)Screen.width * num;
			float a = Mathf.Max(0f, num2 + num3 - num4);
			a = Mathf.Min(a, num2 - num3);
			num2 -= a;
			num2 *= scaleFactor;
			this._arrowOffset.localPosition = Vector3.right * a;
			base.transform.position = new Vector3(num2, Input.mousePosition.y);
			if (this._prevHint != hint)
			{
				this._prevHint = hint;
				this._text.text = Translations.Get(hint);
			}
		}
		else
		{
			this._root.gameObject.SetActive(value: false);
		}
	}

	private void LateUpdate()
	{
		this._textLayout.enabled = this._text.preferredWidth >= this._textLayout.preferredWidth;
	}
}
