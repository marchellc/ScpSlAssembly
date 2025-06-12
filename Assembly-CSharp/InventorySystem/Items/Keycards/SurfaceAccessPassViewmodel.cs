using TMPro;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

public class SurfaceAccessPassViewmodel : KeycardViewmodel
{
	[SerializeField]
	private TMP_Text _labelText;

	[SerializeField]
	private int _maxCharacters;

	[SerializeField]
	private int _fixedUpdatesDelay;

	[SerializeField]
	private int _startMargin;

	[SerializeField]
	private int _endMargin;

	[SerializeField]
	private Renderer _renderer;

	[SerializeField]
	private Material _usedMaterial;

	[SerializeField]
	private float _materialReplaceDelay;

	private int _remainingDelay;

	private int _textLength;

	private bool _replaceMaterialWhenReady;

	private int _startCharacter;

	protected override void PlayUseAnimation(bool success)
	{
		if (success)
		{
			this._replaceMaterialWhenReady = true;
			base.PlayUseAnimation(success);
		}
	}

	public override void InitAny()
	{
		base.InitAny();
		this.SetText(TranslatedLabelDetail.KeycardLabelTranslation.SurfaceAccessPassNormal);
	}

	private void Update()
	{
		if (this._replaceMaterialWhenReady)
		{
			this._materialReplaceDelay -= Time.deltaTime;
			if (!(this._materialReplaceDelay > 0f))
			{
				this._renderer.sharedMaterial = this._usedMaterial;
				this.SetText(TranslatedLabelDetail.KeycardLabelTranslation.SurfaceAccessPassUsed);
				this._replaceMaterialWhenReady = false;
			}
		}
	}

	private void SetText(TranslatedLabelDetail.KeycardLabelTranslation translation)
	{
		string text = Translations.Get(translation);
		this._textLength = text.Length;
		this._startCharacter = -this._startMargin;
		this._labelText.text = text;
		this._labelText.firstVisibleCharacter = 0;
		this._labelText.maxVisibleCharacters = this._maxCharacters;
	}

	private void FixedUpdate()
	{
		if (--this._remainingDelay <= 0)
		{
			if (this._startCharacter > this._textLength + this._endMargin)
			{
				this._startCharacter = -this._startMargin;
			}
			else
			{
				this._startCharacter++;
			}
			int num = Mathf.Clamp(this._startCharacter, 0, this._textLength);
			this._labelText.firstVisibleCharacter = num;
			this._labelText.maxVisibleCharacters = this._maxCharacters + num;
			this._remainingDelay = this._fixedUpdatesDelay;
		}
	}
}
