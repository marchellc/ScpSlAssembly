using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

namespace UserSettings.GUIElements
{
	public abstract class UserSettingsUIBase<TBehaviour, TStoredValue> : MonoBehaviour where TBehaviour : UIBehaviour
	{
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
				return UserSetting<TStoredValue>.Get(this.TargetEnum.TypeHash, this.TargetEnum.Value, this.DefaultValue, false);
			}
			return UserSetting<TStoredValue>.Get(this.TargetEnum.TypeHash, this.TargetEnum.Value);
		}

		protected virtual void Awake()
		{
			if (!this._setupOnAwake)
			{
				return;
			}
			this.Setup();
		}

		protected virtual void OnDestroy()
		{
			this.Unlink();
		}

		protected abstract void SetValueAndTriggerEvent(TStoredValue val);

		protected abstract void SetValueWithoutNotify(TStoredValue val);

		public void Setup()
		{
			if (this.IsSetup)
			{
				return;
			}
			this.IsSetup = true;
			this.SetValueWithoutNotify(this.StoredValue);
			UnityEvent<TStoredValue> onValueChangedEvent = this.OnValueChangedEvent;
			if (onValueChangedEvent != null)
			{
				onValueChangedEvent.AddListener(new UnityAction<TStoredValue>(this.SaveValue));
			}
			UserSetting<TStoredValue>.AddListener(this.TargetEnum.TypeHash, this.TargetEnum.Value, new Action<TStoredValue>(this.SetValueWithoutNotify));
		}

		public void Unlink()
		{
			if (!this.IsSetup)
			{
				return;
			}
			this.IsSetup = false;
			UnityEvent<TStoredValue> onValueChangedEvent = this.OnValueChangedEvent;
			if (onValueChangedEvent != null)
			{
				onValueChangedEvent.RemoveListener(new UnityAction<TStoredValue>(this.SaveValue));
			}
			UserSetting<TStoredValue>.RemoveListener(new Action<TStoredValue>(this.SetValueWithoutNotify));
		}

		public void ChangeTargetEnum(LinkableEnum newEnum, bool autoSetup = true)
		{
			this.Unlink();
			this.TargetEnum = newEnum;
			if (!autoSetup)
			{
				return;
			}
			this.Setup();
		}

		[SerializeField]
		private bool _setupOnAwake = true;

		private bool _cachedTargetSet;

		private TBehaviour _cachedTargetUi;
	}
}
