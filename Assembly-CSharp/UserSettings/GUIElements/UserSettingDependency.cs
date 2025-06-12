using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UserSettings.GUIElements;

[RequireComponent(typeof(CanvasGroup))]
public class UserSettingDependency : MonoBehaviour
{
	[Serializable]
	private class Dependency
	{
		private enum Condition
		{
			EqualsTo,
			LessThan,
			GreaterThan
		}

		private enum SettingType
		{
			Slider,
			Toggle,
			Dropdown
		}

		[SerializeField]
		private Component _targetComponent;

		[SerializeField]
		private Condition _condition;

		[SerializeField]
		private bool _invertCondition;

		[SerializeField]
		private float _valueToCompare;

		private bool _setup;

		private SettingType _type;

		private UserSettingsUIBase<Slider, float> _uiSlider;

		private UserSettingsUIBase<Toggle, bool> _uiToggle;

		private UserSettingsUIBase<TMP_Dropdown, int> _uiDropdown;

		public bool ConditionMet
		{
			get
			{
				if (!this._setup)
				{
					this.Setup();
					this._setup = true;
				}
				bool flag = this.Evaluate();
				bool result = !flag;
				if (!this._invertCondition)
				{
					return flag;
				}
				return result;
			}
		}

		private void Setup()
		{
			if (this._targetComponent is UserSettingsUIBase<Slider, float> uiSlider)
			{
				this._uiSlider = uiSlider;
				this._type = SettingType.Slider;
				return;
			}
			if (this._targetComponent is UserSettingsUIBase<Toggle, bool> uiToggle)
			{
				this._uiToggle = uiToggle;
				this._type = SettingType.Toggle;
				return;
			}
			if (this._targetComponent is UserSettingsUIBase<TMP_Dropdown, int> uiDropdown)
			{
				this._uiDropdown = uiDropdown;
				this._type = SettingType.Dropdown;
				return;
			}
			throw new NotImplementedException("Unhandled type for settings dependency: " + this._targetComponent.GetType().AssemblyQualifiedName);
		}

		private bool Evaluate()
		{
			float num = this._type switch
			{
				SettingType.Slider => this._uiSlider.TargetUI.value, 
				SettingType.Toggle => this._uiToggle.TargetUI.isOn ? 1 : 0, 
				SettingType.Dropdown => this._uiDropdown.TargetUI.value, 
				_ => throw new NotImplementedException($"Unable to parse type '{this._type}' as float!"), 
			};
			return this._condition switch
			{
				Condition.EqualsTo => num == this._valueToCompare, 
				Condition.GreaterThan => num > this._valueToCompare, 
				Condition.LessThan => num < this._valueToCompare, 
				_ => throw new NotImplementedException($"Unhandled condition '{this._condition}'!"), 
			};
		}
	}

	[SerializeField]
	private Dependency[] _dependencies;

	[SerializeField]
	private float _fadeSpeed = 10f;

	[SerializeField]
	private float _minFade = 0.2f;

	private CanvasGroup _fader;

	private bool ShouldBeVisible
	{
		get
		{
			Dependency[] dependencies = this._dependencies;
			for (int i = 0; i < dependencies.Length; i++)
			{
				if (!dependencies[i].ConditionMet)
				{
					return false;
				}
			}
			return true;
		}
	}

	private void Awake()
	{
		this._fader = base.GetComponent<CanvasGroup>();
	}

	private void OnEnable()
	{
		this._fader.alpha = (this.ShouldBeVisible ? 1f : this._minFade);
	}

	private void Update()
	{
		bool shouldBeVisible = this.ShouldBeVisible;
		float num = Time.deltaTime * this._fadeSpeed;
		float num2 = (shouldBeVisible ? num : (0f - num));
		this._fader.blocksRaycasts = shouldBeVisible;
		this._fader.alpha = Mathf.Clamp(this._fader.alpha + num2, this._minFade, 1f);
	}
}
