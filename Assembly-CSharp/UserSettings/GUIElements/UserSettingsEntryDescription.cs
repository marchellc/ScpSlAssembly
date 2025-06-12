using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace UserSettings.GUIElements;

public class UserSettingsEntryDescription : MonoBehaviour, IPointerEnterHandler, IEventSystemHandler, IPointerExitHandler
{
	private bool _potentiallyCurrent;

	private TextLanguageReplacer _tlr;

	private Type _secondaryTranslationType;

	private Enum _secondaryTranslationValue;

	public static UserSettingsEntryDescription CurrentDescription { get; private set; }

	public virtual string Text
	{
		get
		{
			if (this.UsesReplacer)
			{
				return this._tlr.DisplayText;
			}
			if (this._secondaryTranslationType == null)
			{
				return string.Empty;
			}
			Type secondaryTranslationType = this._secondaryTranslationType;
			int index = ((IConvertible)this._secondaryTranslationValue).ToInt32((IFormatProvider)null);
			if (!Translations.TryGet(secondaryTranslationType, index, out var str))
			{
				return this._secondaryTranslationValue.ToString();
			}
			return str;
		}
	}

	public bool UsesReplacer { get; private set; }

	public void SetTranslation<T>(T translation) where T : Enum
	{
		this.UsesReplacer = false;
		this._secondaryTranslationType = typeof(T);
		this._secondaryTranslationValue = translation;
	}

	public void OnPointerEnter(PointerEventData eventData)
	{
		this._potentiallyCurrent = true;
		UserSettingsEntryDescription.CurrentDescription = this;
	}

	public void OnPointerExit(PointerEventData eventData)
	{
		this.Deselect();
	}

	private void Awake()
	{
		this.UsesReplacer = base.TryGetComponent<TextLanguageReplacer>(out this._tlr);
	}

	private void OnDisable()
	{
		this.Deselect();
	}

	private void Deselect()
	{
		if (this._potentiallyCurrent && !(UserSettingsEntryDescription.CurrentDescription != this))
		{
			UserSettingsEntryDescription.CurrentDescription = null;
			this._potentiallyCurrent = false;
		}
	}
}
