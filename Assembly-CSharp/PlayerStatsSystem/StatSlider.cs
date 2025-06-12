using System;
using PlayerRoles;
using UnityEngine;
using UnityEngine.UI;
using UserSettings;
using UserSettings.GUIElements;
using UserSettings.UserInterfaceSettings;

namespace PlayerStatsSystem;

public class StatSlider : MonoBehaviour
{
	private enum DisplayExactMode
	{
		PreferenceBased,
		AlwaysExact,
		AlwaysPercent,
		ValueHidden
	}

	[SerializeField]
	private string _statTypeName;

	[SerializeField]
	private float _lerpSpeed;

	[SerializeField]
	private float _snapValueSkip;

	[SerializeField]
	private DisplayExactMode _displayExactMode;

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
		if (this.TryGetModule(out var sb))
		{
			this._currentValue = sb.CurValue;
		}
		this._targetSlider.value = this._currentValue;
	}

	public bool TryGetModule(out StatBase sb)
	{
		sb = null;
		if (!ReferenceHub.TryGetLocalHub(out var hub))
		{
			return false;
		}
		if (!(hub.roleManager.CurrentRole is IHealthbarRole healthbarRole))
		{
			return false;
		}
		if (healthbarRole.TargetStats == null)
		{
			return false;
		}
		if (!this.TryGetTypeId(out var val))
		{
			return false;
		}
		sb = healthbarRole.TargetStats.StatModules[val];
		return true;
	}

	public bool TryGetTypeId(out int val)
	{
		if (this._cachedTypeId.HasValue)
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
				this._cachedTypeId = i;
				return true;
			}
		}
		WarnOnMissingModule(this._statTypeName, definedModules);
		return false;
		static void WarnOnMissingModule(string typeName, Type[] types)
		{
			string text = "Type \"" + typeName + "\" is not defined as a stat module. Available Modules:\n";
			foreach (Type type in types)
			{
				text = text + "- " + type.Name + "\n";
			}
			text += "\nYou can add new modules at PlayerStats.StatModules.";
			Debug.LogError(text);
		}
	}

	private void Awake()
	{
		this._originalSuffix = this._suffix;
		if (this._displayExactMode == DisplayExactMode.PreferenceBased)
		{
			this.UpdateDisplayMode(UserSetting<bool>.Get(UISetting.HealthbarMode));
			UserSetting<bool>.AddListener(UISetting.HealthbarMode, UpdateDisplayMode);
		}
	}

	private void OnDestroy()
	{
		UserSetting<bool>.RemoveListener(UISetting.HealthbarMode, UpdateDisplayMode);
	}

	private void UpdateDisplayMode(bool isPercent)
	{
		this._displayExactMode = ((!isPercent) ? DisplayExactMode.AlwaysExact : DisplayExactMode.AlwaysPercent);
	}

	private void Update()
	{
		if (this.TryGetModule(out var sb))
		{
			this.UpdateSlider(sb);
			this.UpdateText(sb);
		}
	}

	private void UpdateSlider(StatBase stat)
	{
		float curValue = stat.CurValue;
		float minValue = stat.MinValue;
		float maxValue = stat.MaxValue;
		if (maxValue > minValue)
		{
			this._targetSlider.minValue = stat.MinValue;
			this._targetSlider.maxValue = stat.MaxValue;
		}
		float num = Mathf.Abs(curValue - this._currentValue);
		if (num > this._snapValueSkip)
		{
			this._currentValue = curValue;
		}
		else
		{
			float num2 = Mathf.Max(this._lerpSpeed * num, (maxValue - minValue) * 0.04f);
			this._currentValue = Mathf.MoveTowards(this._currentValue, curValue, num2 * Time.deltaTime);
		}
		this._targetSlider.value = this._currentValue;
	}

	private void UpdateText(StatBase stat)
	{
		if (this._displayExactMode != DisplayExactMode.ValueHidden)
		{
			bool flag = this._displayExactMode == DisplayExactMode.AlwaysExact;
			int num = Mathf.CeilToInt((flag ? stat.CurValue : ((float)Mathf.CeilToInt(stat.NormalizedValue * 100f))) * (float)this._roundingAccuracy) / this._roundingAccuracy;
			if (num != this._lastDisplayedValue)
			{
				this._targetText.text = num + (flag ? this._suffix : "%");
				this._lastDisplayedValue = num;
			}
		}
	}
}
