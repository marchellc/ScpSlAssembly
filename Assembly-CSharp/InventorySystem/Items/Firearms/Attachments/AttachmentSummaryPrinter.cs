using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;
using UserSettings;
using UserSettings.UserInterfaceSettings;

namespace InventorySystem.Items.Firearms.Attachments
{
	public class AttachmentSummaryPrinter : MonoBehaviour
	{
		private Firearm Firearm
		{
			get
			{
				return this._selectorReference.SelectedFirearm;
			}
		}

		private static float GetStatSafe<T>(Firearm fa, Func<T, float> selector, float fallback = 0f)
		{
			T t;
			if (!fa.TryGetModule(out t, true))
			{
				return fallback;
			}
			return selector(t);
		}

		private static bool UsesImperial()
		{
			return UserSetting<bool>.Get<UISetting>(UISetting.ImperialUnits);
		}

		private static bool UsesMetric()
		{
			return !AttachmentSummaryPrinter.UsesImperial();
		}

		private static bool UsesDispersionMode()
		{
			return UserSetting<bool>.Get<UISetting>(UISetting.InaccuracyAsDispersion);
		}

		private static bool UsesEffectiveRangeMode()
		{
			return !AttachmentSummaryPrinter.UsesDispersionMode();
		}

		private void OnEnable()
		{
			this.Refresh();
		}

		private void Update()
		{
			if (this.Firearm == this._prevFirearm)
			{
				return;
			}
			this._prevFirearm = this.Firearm;
			this.Refresh();
		}

		private void Refresh()
		{
			if (this.Firearm == null)
			{
				this._selectorReference.ToggleSummaryScreen(false);
				return;
			}
			uint currentAttachmentsCode = this.Firearm.GetCurrentAttachmentsCode();
			this.PrepareConfigurations(currentAttachmentsCode);
			int count = this._displayedConfigurations.Count;
			int i;
			this._tableHeader.Setup(this.Firearm.Name, count, (int i) => this._displayedConfigurations[i].ConfigurationName, false);
			for (int j = 0; j < count; j++)
			{
				this.UpdateComparableData(j);
			}
			bool flag = false;
			int i2;
			for (i = 0; i < AttachmentSummaryPrinter.ComparisonResults.Length; i = i2 + 1)
			{
				if (!AttachmentSummaryPrinter.StatsToCompare[i].Ignored)
				{
					AttachmentSummaryEntry attachmentSummaryEntry;
					if (!this._entryPool.TryDequeue(out attachmentSummaryEntry))
					{
						attachmentSummaryEntry = global::UnityEngine.Object.Instantiate<AttachmentSummaryEntry>(this._entryTemplate, this._entryTemplate.transform.parent);
					}
					attachmentSummaryEntry.gameObject.SetActive(true);
					attachmentSummaryEntry.Setup(AttachmentSummaryPrinter.StatsToCompare[i].Label, count, (int resultIndex) => AttachmentSummaryPrinter.ComparisonResults[i][resultIndex], flag);
					this._spawnedEntires.Add(attachmentSummaryEntry);
					flag = !flag;
				}
				i2 = i;
			}
			this.Firearm.ApplyAttachmentsCode(currentAttachmentsCode, true);
		}

		private void UpdateComparableData(int indexToCompare)
		{
			this.Firearm.ApplyAttachmentsCode(this._displayedConfigurations[indexToCompare].Code, false);
			for (int i = 0; i < AttachmentSummaryPrinter.StatsToCompare.Length; i++)
			{
				AttachmentSummaryPrinter.ComparableStat comparableStat = AttachmentSummaryPrinter.StatsToCompare[i];
				if (!comparableStat.Ignored)
				{
					List<string> list = AttachmentSummaryPrinter.ComparisonResults[i];
					if (list == null)
					{
						list = new List<string>();
						AttachmentSummaryPrinter.ComparisonResults[i] = list;
					}
					if (indexToCompare == 0)
					{
						AttachmentSummaryPrinter.OriginalDataNonAlloc[i] = comparableStat.GetValue(this.Firearm);
					}
					while (list.Count <= indexToCompare)
					{
						list.Add(string.Empty);
					}
					list[indexToCompare] = comparableStat.CompareTo(this, this.Firearm, AttachmentSummaryPrinter.OriginalDataNonAlloc[i]);
				}
			}
		}

		private void PrepareConfigurations(uint initial)
		{
			foreach (AttachmentSummaryEntry attachmentSummaryEntry in this._spawnedEntires)
			{
				if (!(attachmentSummaryEntry == null))
				{
					attachmentSummaryEntry.gameObject.SetActive(false);
					this._entryPool.Enqueue(attachmentSummaryEntry);
				}
			}
			this._spawnedEntires.Clear();
			this._displayedCodes.Clear();
			this._displayedConfigurations.Clear();
			this.AddConfiguration(0U, Translations.Get<AttachmentEditorsTranslation>(AttachmentEditorsTranslation.DefaultAttachments), true);
			if (AttachmentPreferences.GetPreset(this.Firearm.ItemTypeId) == 0)
			{
				this.AddConfiguration(initial, Translations.Get<AttachmentEditorsTranslation>(AttachmentEditorsTranslation.Custom), true);
			}
			for (int i = 1; i <= this._displayedPresets; i++)
			{
				uint preferenceCodeOfPreset = AttachmentPreferences.GetPreferenceCodeOfPreset(this.Firearm.ItemTypeId, i);
				this.AddConfiguration(preferenceCodeOfPreset, string.Format(Translations.Get<AttachmentEditorsTranslation>(AttachmentEditorsTranslation.PresetId), i), false);
			}
		}

		private void AddConfiguration(uint attachmentCode, string label, bool forceAdd = false)
		{
			uint num = this.Firearm.ValidateAttachmentsCode(attachmentCode);
			if (!this._displayedCodes.Add(num) && !forceAdd)
			{
				return;
			}
			this._displayedConfigurations.Add(new AttachmentSummaryPrinter.ComparableConfiguration(this.Firearm.ItemTypeId, label, num));
		}

		private const string InventoryTranslationKey = "InventoryGUI";

		private const float InchesToCm = 2.54f;

		private const float KilogramsToLbs = 2.20462f;

		private const float DegreesToMOAs = 60f;

		private static readonly AttachmentSummaryPrinter.ComparableStat[] StatsToCompare = new AttachmentSummaryPrinter.ComparableStat[]
		{
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => AttachmentSummaryPrinter.GetStatSafe<IHitregModule>(fa, (IHitregModule x) => x.DisplayDamage, 0f), "", true, AttachmentParam.DamageMultiplier, null),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => AttachmentSummaryPrinter.GetStatSafe<IHitregModule>(fa, (IHitregModule x) => x.DisplayPenetration * 100f, 0f), "%", true, AttachmentParam.PenetrationMultiplier, null),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => AttachmentSummaryPrinter.GetStatSafe<IActionModule>(fa, (IActionModule x) => x.DisplayCyclicRate * 60f, 0f), "/MIN", true, AttachmentParam.FireRateMultiplier, null),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => AttachmentSummaryPrinter.GetStatSafe<IDisplayableRecoilProviderModule>(fa, (IDisplayableRecoilProviderModule x) => Mathf.Round(x.DisplayHipRecoilDegrees * 60f), 0f), "′", false, AttachmentParam.OverallRecoilMultiplier, null),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => AttachmentSummaryPrinter.GetStatSafe<IDisplayableRecoilProviderModule>(fa, (IDisplayableRecoilProviderModule x) => Mathf.Round(x.DisplayAdsRecoilDegrees * 60f), 0f), "′", false, AttachmentParam.AdsRecoilMultiplier, null),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => AttachmentSummaryPrinter.GetStatSafe<IPrimaryAmmoContainerModule>(fa, (IPrimaryAmmoContainerModule x) => (float)x.AmmoMax, 0f), "", true, AttachmentParam.MagazineCapacityModifier, null),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(fa, false).BulletDeg, "°", false, AttachmentParam.BulletInaccuracyMultiplier, new Func<bool>(AttachmentSummaryPrinter.UsesDispersionMode)),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(fa, false).AdsDeg, "°", false, AttachmentParam.AdsInaccuracyMultiplier, new Func<bool>(AttachmentSummaryPrinter.UsesDispersionMode)),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(fa, true).GetAdsAccurateRange(false, true), " m", true, AttachmentParam.AdsInaccuracyMultiplier, () => AttachmentSummaryPrinter.UsesEffectiveRangeMode() && AttachmentSummaryPrinter.UsesMetric()),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(fa, true).GetAdsAccurateRange(true, true), " yd", true, AttachmentParam.AdsInaccuracyMultiplier, () => AttachmentSummaryPrinter.UsesEffectiveRangeMode() && AttachmentSummaryPrinter.UsesImperial()),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(fa, false).HipDeg, "°", false, AttachmentParam.HipInaccuracyMultiplier, new Func<bool>(AttachmentSummaryPrinter.UsesDispersionMode)),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(fa, true).GetHipAccurateRange(false, true), " m", true, AttachmentParam.HipInaccuracyMultiplier, () => AttachmentSummaryPrinter.UsesEffectiveRangeMode() && AttachmentSummaryPrinter.UsesMetric()),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(fa, true).GetHipAccurateRange(true, true), " yd", true, AttachmentParam.HipInaccuracyMultiplier, () => AttachmentSummaryPrinter.UsesEffectiveRangeMode() && AttachmentSummaryPrinter.UsesImperial()),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(fa, false).RunningDeg, "°", false, AttachmentParam.RunningInaccuracyMultiplier, new Func<bool>(AttachmentSummaryPrinter.UsesDispersionMode)),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(fa, true).GetRunningAccurateRange(false, true), " m", true, AttachmentParam.RunningInaccuracyMultiplier, () => AttachmentSummaryPrinter.UsesEffectiveRangeMode() && AttachmentSummaryPrinter.UsesMetric()),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => IDisplayableInaccuracyProviderModule.GetCombinedDisplayInaccuracy(fa, true).GetRunningAccurateRange(true, true), " yd", true, AttachmentParam.RunningInaccuracyMultiplier, () => AttachmentSummaryPrinter.UsesEffectiveRangeMode() && AttachmentSummaryPrinter.UsesImperial()),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => AttachmentSummaryPrinter.GetStatSafe<IEquipperModule>(fa, (IEquipperModule x) => x.GetDisplayEffectiveEquipTime(fa), 0f), " s", false, AttachmentParam.DrawTimeModifier, null),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => fa.TotalLengthInches() * 2.54f, " cm", false, "InventoryGUI", 6, new Func<bool>(AttachmentSummaryPrinter.UsesMetric)),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => fa.TotalLengthInches(), "″", false, "InventoryGUI", 6, new Func<bool>(AttachmentSummaryPrinter.UsesImperial)),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => fa.TotalWeightKg(), " kg", false, "InventoryGUI", 5, new Func<bool>(AttachmentSummaryPrinter.UsesMetric)),
			new AttachmentSummaryPrinter.ComparableStat((Firearm fa) => fa.TotalWeightKg() * 2.20462f, " lbs", false, "InventoryGUI", 5, new Func<bool>(AttachmentSummaryPrinter.UsesImperial))
		};

		private static readonly List<string>[] ComparisonResults = new List<string>[AttachmentSummaryPrinter.StatsToCompare.Length];

		private static readonly float[] OriginalDataNonAlloc = new float[AttachmentSummaryPrinter.StatsToCompare.Length];

		private readonly Queue<AttachmentSummaryEntry> _entryPool = new Queue<AttachmentSummaryEntry>();

		private readonly HashSet<AttachmentSummaryEntry> _spawnedEntires = new HashSet<AttachmentSummaryEntry>();

		private readonly HashSet<uint> _displayedCodes = new HashSet<uint>();

		private readonly List<AttachmentSummaryPrinter.ComparableConfiguration> _displayedConfigurations = new List<AttachmentSummaryPrinter.ComparableConfiguration>();

		private Firearm _prevFirearm;

		[SerializeField]
		private AttachmentSelectorBase _selectorReference;

		[SerializeField]
		private AttachmentSummaryEntry _tableHeader;

		[SerializeField]
		private AttachmentSummaryEntry _entryTemplate;

		[SerializeField]
		private int _displayedPresets;

		[SerializeField]
		private string _goodColor;

		[SerializeField]
		private string _badColor;

		[SerializeField]
		private Color _oddEntryColor;

		private class ComparableStat
		{
			public string Label
			{
				get
				{
					return TranslationReader.Get(this._translationKey, this._translationIndex, "NO_TRANSLATION");
				}
			}

			public bool Ignored
			{
				get
				{
					return this._validator != null && !this._validator();
				}
			}

			private float Round(float f)
			{
				return Mathf.Round(f * 100f) / 100f;
			}

			public string CompareTo(AttachmentSummaryPrinter printer, Firearm fa, float otherValue)
			{
				float value = this.GetValue(fa);
				string text = this.Round(value).ToString() + this._unit;
				if (this.Round(value) == this.Round(otherValue))
				{
					return text;
				}
				bool flag = value > otherValue;
				string text2 = ((this._moreIsBetter ? flag : (!flag)) ? printer._goodColor : printer._badColor);
				return string.Concat(new string[] { "<color=#", text2, ">", text, "</color>" });
			}

			public float GetValue(Firearm fa)
			{
				return this._fetcher(fa);
			}

			public ComparableStat(Func<Firearm, float> fetcher, string unit, bool moreIsBetter, string translationKey, int translationIndex, Func<bool> validator = null)
			{
				this._fetcher = fetcher;
				this._translationKey = translationKey;
				this._translationIndex = translationIndex;
				this._unit = unit;
				this._moreIsBetter = moreIsBetter;
				this._validator = validator;
			}

			public ComparableStat(Func<Firearm, float> fetcher, string unit, bool moreIsBetter, AttachmentParam param, Func<bool> validator = null)
				: this(fetcher, unit, moreIsBetter, "AttachmentParameters", (int)param, validator)
			{
			}

			private readonly Func<Firearm, float> _fetcher;

			private readonly Func<bool> _validator;

			private readonly bool _moreIsBetter;

			private readonly string _unit;

			private readonly string _translationKey;

			private readonly int _translationIndex;
		}

		[Serializable]
		private readonly struct ComparableConfiguration
		{
			public ComparableConfiguration(ItemType weapon, string label, uint code)
			{
				this.ConfigurationName = string.Format((AttachmentPreferences.GetSavedPreferenceCode(weapon) == code) ? "<b><u>{0}</u></b>" : "{0}", label);
				this.Code = code;
			}

			public readonly string ConfigurationName;

			public readonly uint Code;
		}
	}
}
