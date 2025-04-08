using System;
using MapGeneration.Holidays;
using UnityEngine;
using UnityEngine.UI;

public class HolidayBackgroundReplacer : MonoBehaviour
{
	private void Awake()
	{
		foreach (HolidayBackgroundReplacer.BackgroundInfo backgroundInfo in this._backgrounds)
		{
			this.RefreshBackground(backgroundInfo);
		}
	}

	private void RefreshBackground(HolidayBackgroundReplacer.BackgroundInfo info)
	{
		Sprite sprite;
		if (!info.Variants.TryGetResult(out sprite))
		{
			return;
		}
		info.Target.sprite = sprite;
	}

	[SerializeField]
	private HolidayBackgroundReplacer.BackgroundInfo[] _backgrounds;

	[Serializable]
	private struct BackgroundInfo
	{
		public Image Target;

		public HolidayBackgroundReplacer.HolidayInfo[] Variants;
	}

	[Serializable]
	private struct HolidayInfo : IHolidayFetchableData<Sprite>
	{
		public HolidayType Holiday { readonly get; private set; }

		public Sprite Result { readonly get; private set; }
	}
}
