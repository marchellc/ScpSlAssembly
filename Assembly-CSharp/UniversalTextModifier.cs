using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public abstract class UniversalTextModifier : MonoBehaviour
{
	public string DisplayText
	{
		get
		{
			if (this._usesTmp)
			{
				return this._tmpText.text;
			}
			if (this._usesUnity)
			{
				return this._unityText.text;
			}
			if (this._text != null)
			{
				return this._text;
			}
			return string.Empty;
		}
		set
		{
			if (this._usesTmp)
			{
				this._tmpText.text = value;
			}
			if (this._usesUnity)
			{
				this._unityText.text = value;
			}
			this._text = value;
		}
	}

	protected virtual void Awake()
	{
		this._usesTmp = base.TryGetComponent<TMP_Text>(out this._tmpText);
		this._usesUnity = base.TryGetComponent<Text>(out this._unityText);
	}

	private TMP_Text _tmpText;

	private Text _unityText;

	private bool _usesTmp;

	private bool _usesUnity;

	private string _text;
}
