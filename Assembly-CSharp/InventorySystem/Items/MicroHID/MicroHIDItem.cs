using System;
using InventorySystem.Drawers;
using InventorySystem.GUI;
using InventorySystem.GUI.Descriptions;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.MicroHID.Modules;
using UnityEngine;

namespace InventorySystem.Items.MicroHID
{
	public class MicroHIDItem : ModularAutosyncItem, ICustomDescriptionItem, IItemDescription, IItemNametag, IItemAlertDrawer, IItemDrawer, ISoundEmittingItem
	{
		public override float Weight
		{
			get
			{
				return 25.1f;
			}
		}

		public override bool AllowDropping
		{
			get
			{
				return true;
			}
		}

		public override bool AllowHolster
		{
			get
			{
				return base.AllowHolster && this.CycleController.Phase == MicroHidPhase.Standby;
			}
		}

		public EnergyManagerModule EnergyManager { get; private set; }

		public BacktrackerModule Backtracker { get; private set; }

		public InputSyncModule InputSync { get; private set; }

		public BrokenSyncModule BrokenSync { get; private set; }

		public CycleController CycleController { get; private set; }

		public override ItemDescriptionType DescriptionType
		{
			get
			{
				return ItemDescriptionType.Custom;
			}
		}

		public CustomDescriptionGui CustomGuiPrefab { get; private set; }

		public string[] CustomDescriptionContent
		{
			get
			{
				int num = Mathf.CeilToInt(this.EnergyManager.Energy * 100f);
				MicroHIDItem.ChargeMeterNonAlloc[0] = MicroHIDItem.FormatCharge(InventoryGuiTranslation.RemainingCharge, num.ToString() + "%");
				return MicroHIDItem.ChargeMeterNonAlloc;
			}
		}

		public string Description
		{
			get
			{
				return this.ItemTypeId.GetDescription();
			}
		}

		public string Name
		{
			get
			{
				return this.ItemTypeId.GetName();
			}
		}

		public AlertContent Alert
		{
			get
			{
				return this.ActiveAlertHelper.Alert;
			}
		}

		private ItemHintAlertHelper ActiveAlertHelper
		{
			get
			{
				if (this.CycleController.Phase == MicroHidPhase.WoundUpSustain)
				{
					return this._readyHint;
				}
				this._readyHint.Reset();
				if (!this.BrokenSync.Broken)
				{
					return this._regularHint;
				}
				return this._brokenHint;
			}
		}

		public override void EquipUpdate()
		{
			base.EquipUpdate();
			this.ActiveAlertHelper.Update();
		}

		public override void InitializeSubcomponents()
		{
			this.CycleController = CycleSyncModule.GetCycleController(base.ItemSerial);
			foreach (SubcomponentBase subcomponentBase in base.AllSubcomponents)
			{
				EnergyManagerModule energyManagerModule = subcomponentBase as EnergyManagerModule;
				if (energyManagerModule == null)
				{
					BacktrackerModule backtrackerModule = subcomponentBase as BacktrackerModule;
					if (backtrackerModule == null)
					{
						InputSyncModule inputSyncModule = subcomponentBase as InputSyncModule;
						if (inputSyncModule == null)
						{
							BrokenSyncModule brokenSyncModule = subcomponentBase as BrokenSyncModule;
							if (brokenSyncModule != null)
							{
								this.BrokenSync = brokenSyncModule;
							}
						}
						else
						{
							this.InputSync = inputSyncModule;
						}
					}
					else
					{
						this.Backtracker = backtrackerModule;
					}
				}
				else
				{
					this.EnergyManager = energyManagerModule;
				}
			}
			base.InitializeSubcomponents();
		}

		public bool ServerTryGetSoundEmissionRange(out float range)
		{
			return AudioManagerModule.GetController(base.ItemSerial).ServerTryGetSoundEmissionRange(out range);
		}

		public static string FormatCharge(InventoryGuiTranslation translation, string coloredText)
		{
			string text = Translations.Get<InventoryGuiTranslation>(translation, "{0}");
			string text2 = "</color>" + coloredText + "<color=white>";
			return "<color=white>" + string.Format(text, text2) + "</color>";
		}

		private static readonly string[] ChargeMeterNonAlloc = new string[1];

		private readonly ItemHintAlertHelper _readyHint = new ItemHintAlertHelper(InventoryGuiTranslation.MicroHidReadyToDischarge, new ActionName?(ActionName.Shoot), 0.3f, 2f, 6f);

		private readonly ItemHintAlertHelper _regularHint = new ItemHintAlertHelper(InventoryGuiTranslation.MicroHidPrimaryHint, new ActionName?(ActionName.Shoot), InventoryGuiTranslation.MicroHidSecondaryHint, new ActionName?(ActionName.Zoom), 0.3f, 1f, 8f);

		private readonly ItemHintAlertHelper _brokenHint = new ItemHintAlertHelper(InventoryGuiTranslation.MicroHidDamaged, null, InventoryGuiTranslation.MicroHidPrimaryHint, new ActionName?(ActionName.Shoot), 0.3f, 1f, 8f);
	}
}
