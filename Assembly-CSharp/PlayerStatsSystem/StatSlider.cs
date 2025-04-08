using System;
using System.Runtime.CompilerServices;
using PlayerRoles;
using UnityEngine;
using UnityEngine.UI;
using UserSettings;
using UserSettings.GUIElements;
using UserSettings.UserInterfaceSettings;

namespace PlayerStatsSystem
{
	public class StatSlider : MonoBehaviour
	{
		public string CustomSuffix
		{
			get
			{
				return this._suffix;
			}
			set
			{
				this._suffix = (string.IsNullOrEmpty(value) ? this._originalSuffix : value);
			}
		}

		public void ForceUpdate()
		{
			StatBase statBase;
			if (this.TryGetModule(out statBase))
			{
				this._currentValue = statBase.CurValue;
			}
			this._targetSlider.value = this._currentValue;
		}

		public bool TryGetModule(out StatBase sb)
		{
			sb = null;
			ReferenceHub referenceHub;
			if (!ReferenceHub.TryGetLocalHub(out referenceHub))
			{
				return false;
			}
			IHealthbarRole healthbarRole = referenceHub.roleManager.CurrentRole as IHealthbarRole;
			if (healthbarRole == null)
			{
				return false;
			}
			if (healthbarRole.TargetStats == null)
			{
				return false;
			}
			int num;
			if (!this.TryGetTypeId(out num))
			{
				return false;
			}
			sb = healthbarRole.TargetStats.StatModules[num];
			return true;
		}

		public bool TryGetTypeId(out int val)
		{
			if (this._cachedTypeId != null)
			{
				val = this._cachedTypeId.Value;
				return true;
			}
			val = -1;
			Type[] definedModules = PlayerStats.DefinedModules;
			for (int i = 0; i < definedModules.Length; i++)
			{
				if (!(definedModules[i].Name != this._statTypeName))
				{
					val = i;
					this._cachedTypeId = new int?(i);
					return true;
				}
			}
			StatSlider.<TryGetTypeId>g__WarnOnMissingModule|19_0(this._statTypeName, definedModules);
			return false;
		}

		private void Awake()
		{
			this._originalSuffix = this._suffix;
			if (this._displayExactMode != StatSlider.DisplayExactMode.PreferenceBased)
			{
				return;
			}
			this.UpdateDisplayMode(UserSetting<bool>.Get<UISetting>(UISetting.HealthbarMode));
			UserSetting<bool>.AddListener<UISetting>(UISetting.HealthbarMode, new Action<bool>(this.UpdateDisplayMode));
		}

		private void OnDestroy()
		{
			UserSetting<bool>.RemoveListener<UISetting>(UISetting.HealthbarMode, new Action<bool>(this.UpdateDisplayMode));
		}

		private void UpdateDisplayMode(bool isPercent)
		{
			this._displayExactMode = (isPercent ? StatSlider.DisplayExactMode.AlwaysPercent : StatSlider.DisplayExactMode.AlwaysExact);
		}

		private void Update()
		{
			StatBase statBase;
			if (!this.TryGetModule(out statBase))
			{
				return;
			}
			this.UpdateSlider(statBase);
			this.UpdateText(statBase);
		}

		private void UpdateSlider(StatBase stat)
		{
			this._targetSlider.minValue = stat.MinValue;
			this._targetSlider.maxValue = stat.MaxValue;
			float num = Mathf.Abs(stat.CurValue - this._currentValue);
			if (num > this._snapValueSkip)
			{
				this._currentValue = stat.CurValue;
			}
			else
			{
				float num2 = Mathf.Max(this._lerpSpeed * num, (stat.MaxValue - stat.MinValue) * 0.04f);
				this._currentValue = Mathf.MoveTowards(this._currentValue, stat.CurValue, num2 * Time.deltaTime);
			}
			this._targetSlider.value = this._currentValue;
		}

		private void UpdateText(StatBase stat)
		{
			if (this._displayExactMode == StatSlider.DisplayExactMode.ValueHidden)
			{
				return;
			}
			bool flag = this._displayExactMode == StatSlider.DisplayExactMode.AlwaysExact;
			int num = Mathf.CeilToInt((flag ? stat.CurValue : ((float)Mathf.CeilToInt(stat.NormalizedValue * 100f))) * (float)this._roundingAccuracy) / this._roundingAccuracy;
			if (num == this._lastDisplayedValue)
			{
				return;
			}
			this._targetText.text = num.ToString() + (flag ? this._suffix : "%");
			this._lastDisplayedValue = num;
		}

		[CompilerGenerated]
		internal static void <TryGetTypeId>g__WarnOnMissingModule|19_0(string typeName, Type[] types)
		{
			string text = "Type \"" + typeName + "\" is not defined as a stat module. Available Modules:\n";
			foreach (Type type in types)
			{
				text = text + "- " + type.Name + "\n";
			}
			text += "\nYou can add new modules at PlayerStats.StatModules.";
			Debug.LogError(text);
		}

		[SerializeField]
		private string _statTypeName;

		[SerializeField]
		private float _lerpSpeed;

		[SerializeField]
		private float _snapValueSkip;

		[SerializeField]
		private StatSlider.DisplayExactMode _displayExactMode;

		[SerializeField]
		private LinkableEnum _preferenceKey;

		[SerializeField]
		private string _suffix;

		[SerializeField]
		private Text _targetText;

		[SerializeField]
		private Slider _targetSlider;

		[SerializeField]
		private int _roundingAccuracy;

		private float _currentValue;

		private int _lastDisplayedValue = -1;

		private string _originalSuffix;

		private int? _cachedTypeId;

		private const float AbsoluteMoveRatio = 0.04f;

		private enum DisplayExactMode
		{
			PreferenceBased,
			AlwaysExact,
			AlwaysPercent,
			ValueHidden
		}
	}
}
