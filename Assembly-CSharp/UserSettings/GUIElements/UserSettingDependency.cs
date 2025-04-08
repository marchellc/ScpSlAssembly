using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UserSettings.GUIElements
{
	[RequireComponent(typeof(CanvasGroup))]
	public class UserSettingDependency : MonoBehaviour
	{
		private bool ShouldBeVisible
		{
			get
			{
				UserSettingDependency.Dependency[] dependencies = this._dependencies;
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
			float num2 = (shouldBeVisible ? num : (-num));
			this._fader.blocksRaycasts = shouldBeVisible;
			this._fader.alpha = Mathf.Clamp(this._fader.alpha + num2, this._minFade, 1f);
		}

		[SerializeField]
		private UserSettingDependency.Dependency[] _dependencies;

		[SerializeField]
		private float _fadeSpeed = 10f;

		[SerializeField]
		private float _minFade = 0.2f;

		private CanvasGroup _fader;

		[Serializable]
		private class Dependency
		{
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
					bool flag2 = !flag;
					if (!this._invertCondition)
					{
						return flag;
					}
					return flag2;
				}
			}

			private void Setup()
			{
				UserSettingsUIBase<Slider, float> userSettingsUIBase = this._targetComponent as UserSettingsUIBase<Slider, float>;
				if (userSettingsUIBase != null)
				{
					this._uiSlider = userSettingsUIBase;
					this._type = UserSettingDependency.Dependency.SettingType.Slider;
					return;
				}
				UserSettingsUIBase<Toggle, bool> userSettingsUIBase2 = this._targetComponent as UserSettingsUIBase<Toggle, bool>;
				if (userSettingsUIBase2 != null)
				{
					this._uiToggle = userSettingsUIBase2;
					this._type = UserSettingDependency.Dependency.SettingType.Toggle;
					return;
				}
				UserSettingsUIBase<TMP_Dropdown, int> userSettingsUIBase3 = this._targetComponent as UserSettingsUIBase<TMP_Dropdown, int>;
				if (userSettingsUIBase3 != null)
				{
					this._uiDropdown = userSettingsUIBase3;
					this._type = UserSettingDependency.Dependency.SettingType.Dropdown;
					return;
				}
				throw new NotImplementedException("Unhandled type for settings dependency: " + this._targetComponent.GetType().AssemblyQualifiedName);
			}

			private bool Evaluate()
			{
				float num;
				switch (this._type)
				{
				case UserSettingDependency.Dependency.SettingType.Slider:
					num = this._uiSlider.TargetUI.value;
					break;
				case UserSettingDependency.Dependency.SettingType.Toggle:
					num = (float)(this._uiToggle.TargetUI.isOn ? 1 : 0);
					break;
				case UserSettingDependency.Dependency.SettingType.Dropdown:
					num = (float)this._uiDropdown.TargetUI.value;
					break;
				default:
					throw new NotImplementedException(string.Format("Unable to parse type '{0}' as float!", this._type));
				}
				switch (this._condition)
				{
				case UserSettingDependency.Dependency.Condition.EqualsTo:
					return num == this._valueToCompare;
				case UserSettingDependency.Dependency.Condition.LessThan:
					return num < this._valueToCompare;
				case UserSettingDependency.Dependency.Condition.GreaterThan:
					return num > this._valueToCompare;
				default:
					throw new NotImplementedException(string.Format("Unhandled condition '{0}'!", this._condition));
				}
			}

			[SerializeField]
			private Component _targetComponent;

			[SerializeField]
			private UserSettingDependency.Dependency.Condition _condition;

			[SerializeField]
			private bool _invertCondition;

			[SerializeField]
			private float _valueToCompare;

			private bool _setup;

			private UserSettingDependency.Dependency.SettingType _type;

			private UserSettingsUIBase<Slider, float> _uiSlider;

			private UserSettingsUIBase<Toggle, bool> _uiToggle;

			private UserSettingsUIBase<TMP_Dropdown, int> _uiDropdown;

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
		}
	}
}
