using System;
using UnityEngine;

namespace UserSettings.ServerSpecific.Entries
{
	public class SSGroupHeaderEntry : MonoBehaviour, ISSEntry
	{
		public bool CheckCompatibility(ServerSpecificSettingBase setting)
		{
			return setting is SSGroupHeader;
		}

		public void Init(ServerSpecificSettingBase setting)
		{
			RectTransform rectTransform = base.transform as RectTransform;
			float num = ((setting as SSGroupHeader).ReducedPadding ? this._shortPadding : this._normalPadding);
			rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, num);
			this._label.Set(setting);
		}

		[SerializeField]
		private SSEntryLabel _label;

		[SerializeField]
		private float _normalPadding;

		[SerializeField]
		private float _shortPadding;
	}
}
