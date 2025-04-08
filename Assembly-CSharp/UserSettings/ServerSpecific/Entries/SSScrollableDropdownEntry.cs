using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UserSettings.ServerSpecific.Entries
{
	public class SSScrollableDropdownEntry : SSDropdownEntry, ISSEntry
	{
		public void Next()
		{
			int num = base.TargetUI.value + 1;
			base.TargetUI.value = num % base.TargetUI.options.Count;
			this.UpdateInteractability();
		}

		public void Prev()
		{
			if (base.TargetUI.value > 0)
			{
				TMP_Dropdown targetUI = base.TargetUI;
				int value = targetUI.value;
				targetUI.value = value - 1;
			}
			else
			{
				base.TargetUI.value = base.TargetUI.options.Count - 1;
			}
			this.UpdateInteractability();
		}

		public override bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			SSDropdownSetting ssdropdownSetting = setting as SSDropdownSetting;
			return ssdropdownSetting != null && ssdropdownSetting.EntryType > SSDropdownSetting.DropdownEntryType.Regular;
		}

		public override void Init(ServerSpecificSettingBase setting)
		{
			base.Init(setting);
			SSDropdownSetting.DropdownEntryType entryType = (setting as SSDropdownSetting).EntryType;
			this._loopable = entryType == SSDropdownSetting.DropdownEntryType.ScrollableLoop || entryType == SSDropdownSetting.DropdownEntryType.HybridLoop;
			if (entryType != SSDropdownSetting.DropdownEntryType.Hybrid && entryType != SSDropdownSetting.DropdownEntryType.HybridLoop)
			{
				this._hybridArrow.SetActive(false);
				base.TargetUI.interactable = false;
				base.TargetUI.captionText.alignment = TextAlignmentOptions.Center;
				RectTransform rectTransform = base.TargetUI.captionText.rectTransform;
				rectTransform.anchoredPosition = new Vector2(0f, rectTransform.anchoredPosition.y);
			}
			this.UpdateInteractability();
			base.TargetUI.onValueChanged.AddListener(delegate(int _)
			{
				this.UpdateInteractability();
			});
		}

		private void UpdateInteractability()
		{
			if (this._loopable)
			{
				return;
			}
			this._prevArrow.interactable = base.TargetUI.value > 0;
			this._nextArrow.interactable = base.TargetUI.value < base.TargetUI.options.Count - 1;
		}

		[SerializeField]
		private Button _prevArrow;

		[SerializeField]
		private Button _nextArrow;

		[SerializeField]
		private GameObject _hybridArrow;

		private bool _loopable;
	}
}
