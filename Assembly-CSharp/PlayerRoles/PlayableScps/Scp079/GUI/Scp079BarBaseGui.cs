using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PlayerRoles.PlayableScps.Scp079.GUI;

public abstract class Scp079BarBaseGui : Scp079GuiElementBase
{
	[SerializeField]
	private Image _slider;

	[SerializeField]
	private TextMeshProUGUI _textNormal;

	[SerializeField]
	private TextMeshProUGUI _textInverted;

	private RectMask2D _rectMask;

	private float _width;

	protected abstract string Text { get; }

	protected abstract float FillAmount { get; }

	private void Awake()
	{
		this._rectMask = this._slider.GetComponent<RectMask2D>();
		this._width = this._slider.rectTransform.rect.width;
	}

	protected virtual void Update()
	{
		string text = this.Text;
		this._textNormal.text = text;
		this._textInverted.text = text;
		float num = Mathf.Clamp01(this.FillAmount);
		this._slider.fillAmount = num;
		this._rectMask.padding = this._width * (1f - num) * Vector3.forward;
	}
}
