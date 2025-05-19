using UnityEngine;

namespace UserSettings.ServerSpecific.Entries;

public class SSGroupHeaderEntry : MonoBehaviour, ISSEntry
{
	[SerializeField]
	private SSEntryLabel _label;

	[SerializeField]
	private float _normalPadding;

	[SerializeField]
	private float _shortPadding;

	public bool CheckCompatibility(ServerSpecificSettingBase setting)
	{
		return setting is SSGroupHeader;
	}

	public void Init(ServerSpecificSettingBase setting)
	{
		RectTransform obj = base.transform as RectTransform;
		obj.sizeDelta = new Vector2(y: (setting as SSGroupHeader).ReducedPadding ? _shortPadding : _normalPadding, x: obj.sizeDelta.x);
		_label.Set(setting);
	}
}
