using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UserSettings.GUIElements;

public class UserSettingsFloatField : UserSettingsUIBase<TMP_InputField, float>
{
	[SerializeField]
	private float _minInput;

	[SerializeField]
	private float _maxInput = 1f;

	[SerializeField]
	private string _toStringFormat = "0.000";

	[SerializeField]
	private string _finalFormat = "{0}";

	[SerializeField]
	private float _valueMultiplier = 1f;

	private readonly UnityEvent<float> _onParsed = new UnityEvent<float>();

	protected override UnityEvent<float> OnValueChangedEvent => this._onParsed;

	protected override void Awake()
	{
		base.Awake();
		base.TargetUI.onEndEdit.AddListener(delegate(string str)
		{
			if (!float.TryParse(str, out var result))
			{
				this.SetValueAndTriggerEvent(base.StoredValue * this._valueMultiplier);
			}
			else
			{
				this._onParsed.Invoke(Mathf.Clamp(result / this._valueMultiplier, this._minInput, this._maxInput));
				EventSystem current = EventSystem.current;
				if (!current.alreadySelecting)
				{
					current.SetSelectedGameObject(null);
				}
			}
		});
		base.TargetUI.onSelect.AddListener(delegate
		{
			string text = (base.StoredValue * this._valueMultiplier).ToString();
			if (text.Length > base.TargetUI.characterLimit)
			{
				text = text.Remove(base.TargetUI.characterLimit);
			}
			base.TargetUI.SetTextWithoutNotify(text);
		});
	}

	protected override void SetValueAndTriggerEvent(float val)
	{
		base.TargetUI.text = this.FormatValue(val);
	}

	protected override void SetValueWithoutNotify(float val)
	{
		base.TargetUI.SetTextWithoutNotify(this.FormatValue(val));
	}

	private string FormatValue(float val)
	{
		return string.Format(this._finalFormat, (val * this._valueMultiplier).ToString(this._toStringFormat));
	}
}
