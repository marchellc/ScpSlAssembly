using InventorySystem.Drawers;
using InventorySystem.GUI;
using InventorySystem.GUI.Descriptions;
using InventorySystem.Items.Autosync;
using InventorySystem.Items.MicroHID.Modules;
using UnityEngine;

namespace InventorySystem.Items.MicroHID;

public class MicroHIDItem : ModularAutosyncItem, ICustomDescriptionItem, IItemDescription, IItemNametag, IItemAlertDrawer, IItemDrawer, ISoundEmittingItem
{
	private static readonly string[] ChargeMeterNonAlloc = new string[1];

	private readonly ItemHintAlertHelper _readyHint = new ItemHintAlertHelper(InventoryGuiTranslation.MicroHidReadyToDischarge, ActionName.Shoot, 0.3f, 2f);

	private readonly ItemHintAlertHelper _regularHint = new ItemHintAlertHelper(InventoryGuiTranslation.MicroHidPrimaryHint, ActionName.Shoot, InventoryGuiTranslation.MicroHidSecondaryHint, ActionName.Zoom);

	private readonly ItemHintAlertHelper _brokenHint = new ItemHintAlertHelper(InventoryGuiTranslation.MicroHidDamaged, null, InventoryGuiTranslation.MicroHidPrimaryHint, ActionName.Shoot);

	public override float Weight => 25.1f;

	public override bool AllowDropping => true;

	public override bool AllowHolster
	{
		get
		{
			if (base.AllowHolster)
			{
				return this.CycleController.Phase == MicroHidPhase.Standby;
			}
			return false;
		}
	}

	public EnergyManagerModule EnergyManager { get; private set; }

	public BacktrackerModule Backtracker { get; private set; }

	public InputSyncModule InputSync { get; private set; }

	public BrokenSyncModule BrokenSync { get; private set; }

	public CycleController CycleController { get; private set; }

	public override ItemDescriptionType DescriptionType => ItemDescriptionType.Custom;

	[field: SerializeField]
	public new CustomDescriptionGui CustomGuiPrefab { get; private set; }

	public new string[] CustomDescriptionContent
	{
		get
		{
			int num = Mathf.CeilToInt(this.EnergyManager.Energy * 100f);
			MicroHIDItem.ChargeMeterNonAlloc[0] = MicroHIDItem.FormatCharge(InventoryGuiTranslation.RemainingCharge, num + "%");
			return MicroHIDItem.ChargeMeterNonAlloc;
		}
	}

	public string Description => base.ItemTypeId.GetDescription();

	public string Name => base.ItemTypeId.GetName();

	public new AlertContent Alert => this.ActiveAlertHelper.Alert;

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
		this.ActiveAlertHelper.Update(base.Owner);
	}

	public override void InitializeSubcomponents()
	{
		this.CycleController = CycleSyncModule.GetCycleController(base.ItemSerial);
		SubcomponentBase[] allSubcomponents = base.AllSubcomponents;
		foreach (SubcomponentBase subcomponentBase in allSubcomponents)
		{
			if (!(subcomponentBase is EnergyManagerModule energyManager))
			{
				if (!(subcomponentBase is BacktrackerModule backtracker))
				{
					if (!(subcomponentBase is InputSyncModule inputSync))
					{
						if (subcomponentBase is BrokenSyncModule brokenSync)
						{
							this.BrokenSync = brokenSync;
						}
					}
					else
					{
						this.InputSync = inputSync;
					}
				}
				else
				{
					this.Backtracker = backtracker;
				}
			}
			else
			{
				this.EnergyManager = energyManager;
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
		string format = Translations.Get(translation, "{0}");
		string arg = "</color>" + coloredText + "<color=white>";
		return "<color=white>" + string.Format(format, arg) + "</color>";
	}
}
