using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UserSettings.GUIElements;

public abstract class UserSettingsUIBase<TBehaviour, TStoredValue> : MonoBehaviour where TBehaviour : UIBehaviour
{
	[SerializeField]
	private bool _setupOnAwake = true;

	private bool _cachedTargetSet;

	private TBehaviour _cachedTargetUi;

	public LinkableEnum TargetEnum { get; private set; }

	public TStoredValue DefaultValue { get; private set; }

	public bool OverrideDefaultValue { get; private set; }

	public bool IsSetup { get; private set; }

	public TBehaviour TargetUI
	{
		get
		{
			if (!this._cachedTargetSet)
			{
				this._cachedTargetUi = base.GetComponent<TBehaviour>();
				this._cachedTargetSet = true;
			}
			return this._cachedTargetUi;
		}
	}

	protected abstract UnityEvent<TStoredValue> OnValueChangedEvent { get; }

	protected TStoredValue StoredValue
	{
		get
		{
			return this.ReadSavedValue();
		}
		set
		{
			this.SaveValue(value);
		}
	}

	protected virtual void SaveValue(TStoredValue val)
	{
		UserSetting<TStoredValue>.Set(this.TargetEnum.TypeHash, this.TargetEnum.Value, val);
	}

	protected virtual TStoredValue ReadSavedValue()
	{
		if (this.OverrideDefaultValue)
		{
			return UserSetting<TStoredValue>.Get(this.TargetEnum.TypeHash, this.TargetEnum.Value, this.DefaultValue);
		}
		return UserSetting<TStoredValue>.Get(this.TargetEnum.TypeHash, this.TargetEnum.Value);
	}

	protected virtual void Awake()
	{
		if (this._setupOnAwake)
		{
			this.Setup();
		}
	}

	protected virtual void OnDestroy()
	{
		this.Unlink();
	}

	protected abstract void SetValueAndTriggerEvent(TStoredValue val);

	protected abstract void SetValueWithoutNotify(TStoredValue val);

	public void Setup()
	{
		if (!this.IsSetup)
		{
			this.IsSetup = true;
			this.SetValueWithoutNotify(this.StoredValue);
			this.OnValueChangedEvent?.AddListener(SaveValue);
			UserSetting<TStoredValue>.AddListener(this.TargetEnum.TypeHash, this.TargetEnum.Value, SetValueWithoutNotify);
		}
	}

	public void Unlink()
	{
		if (this.IsSetup)
		{
			this.IsSetup = false;
			this.OnValueChangedEvent?.RemoveListener(SaveValue);
			UserSetting<TStoredValue>.RemoveListener(SetValueWithoutNotify);
		}
	}

	public void ChangeTargetEnum(LinkableEnum newEnum, bool autoSetup = true)
	{
		this.Unlink();
		this.TargetEnum = newEnum;
		if (autoSetup)
		{
			this.Setup();
		}
	}
}
