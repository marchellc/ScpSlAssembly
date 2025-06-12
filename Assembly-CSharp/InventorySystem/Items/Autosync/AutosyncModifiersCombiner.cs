using System;
using InventorySystem.Drawers;
using InventorySystem.GUI.Descriptions;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.PlayableScps.HumeShield;
using UnityEngine;

namespace InventorySystem.Items.Autosync;

public class AutosyncModifiersCombiner : IMovementSpeedModifier, IStaminaModifier, IZoomModifyingItem, ILightEmittingItem, IAmmoDropPreventer, IHumeShieldProvider, IItemAlertDrawer, IItemDrawer, ICustomDescriptionItem
{
	private static readonly IMovementSpeedModifier[] MovementSpeedModifiersNonAlloc = new IMovementSpeedModifier[16];

	private static readonly IStaminaModifier[] StaminaModifiersNonAlloc = new IStaminaModifier[16];

	private static readonly IZoomModifyingItem[] ZoomModifiersNonAlloc = new IZoomModifyingItem[8];

	private static readonly ILightEmittingItem[] LightEmittersNonAlloc = new ILightEmittingItem[8];

	private static readonly IAmmoDropPreventer[] DropPreventersNonAlloc = new IAmmoDropPreventer[8];

	private static readonly IHumeShieldProvider[] HumeShieldProvidersNonAlloc = new IHumeShieldProvider[8];

	private static readonly IItemAlertDrawer[] AlertDrawersNonAlloc = new IItemAlertDrawer[8];

	private readonly IMovementSpeedModifier[] _movementSpeedModifiers;

	private readonly IStaminaModifier[] _staminaModifiers;

	private readonly IZoomModifyingItem[] _zoomModifiers;

	private readonly ILightEmittingItem[] _lightEmitters;

	private readonly IAmmoDropPreventer[] _dropPreventers;

	private readonly IHumeShieldProvider[] _humeShields;

	private readonly IItemAlertDrawer[] _alertDrawers;

	private readonly ICustomDescriptionItem _customDescription;

	private readonly ModularAutosyncItem _item;

	public bool StaminaModifierActive => this._item.IsEquipped;

	public bool MovementModifierActive => this._item.IsEquipped;

	public float MovementSpeedMultiplier => AutosyncModifiersCombiner.CombineMultiplier(this._movementSpeedModifiers, (IMovementSpeedModifier x) => x.MovementSpeedMultiplier, (IMovementSpeedModifier x) => x.MovementModifierActive);

	public float MovementSpeedLimit => AutosyncModifiersCombiner.MinValue(this._movementSpeedModifiers, float.MaxValue, (IMovementSpeedModifier x) => x.MovementSpeedLimit, (IMovementSpeedModifier x) => x.MovementModifierActive);

	public float StaminaUsageMultiplier => AutosyncModifiersCombiner.CombineMultiplier(this._staminaModifiers, (IStaminaModifier x) => x.StaminaUsageMultiplier, (IStaminaModifier x) => x.StaminaModifierActive);

	public float StaminaRegenMultiplier => AutosyncModifiersCombiner.CombineMultiplier(this._staminaModifiers, (IStaminaModifier x) => x.StaminaRegenMultiplier, (IStaminaModifier x) => x.StaminaModifierActive);

	public bool SprintingDisabled => AutosyncModifiersCombiner.AnyTrue(this._staminaModifiers, (IStaminaModifier x) => x.StaminaModifierActive && x.SprintingDisabled);

	public float ZoomAmount => AutosyncModifiersCombiner.CombineMultiplier(this._zoomModifiers, (IZoomModifyingItem x) => x.ZoomAmount);

	public float SensitivityScale => AutosyncModifiersCombiner.CombineMultiplier(this._zoomModifiers, (IZoomModifyingItem x) => x.SensitivityScale);

	public bool IsEmittingLight => AutosyncModifiersCombiner.AnyTrue(this._lightEmitters, (ILightEmittingItem x) => x.IsEmittingLight);

	public bool ForceBarVisible => AutosyncModifiersCombiner.AnyTrue(this._humeShields, (IHumeShieldProvider x) => x.ForceBarVisible);

	public float HsMax => AutosyncModifiersCombiner.CombineAdditive(this._humeShields, (IHumeShieldProvider x) => x.HsMax);

	public float HsRegeneration => AutosyncModifiersCombiner.CombineAdditive(this._humeShields, (IHumeShieldProvider x) => x.HsRegeneration);

	public CustomDescriptionGui CustomGuiPrefab => this._customDescription.CustomGuiPrefab;

	public string[] CustomDescriptionContent => this._customDescription.CustomDescriptionContent;

	public Color? HsWarningColor
	{
		get
		{
			IHumeShieldProvider[] humeShields = this._humeShields;
			for (int i = 0; i < humeShields.Length; i++)
			{
				Color? hsWarningColor = humeShields[i].HsWarningColor;
				if (hsWarningColor.HasValue)
				{
					return hsWarningColor;
				}
			}
			return null;
		}
	}

	public AlertContent Alert
	{
		get
		{
			IItemAlertDrawer[] alertDrawers = this._alertDrawers;
			foreach (IItemAlertDrawer itemAlertDrawer in alertDrawers)
			{
				if (itemAlertDrawer.Alert.Active)
				{
					return itemAlertDrawer.Alert;
				}
			}
			return default(AlertContent);
		}
	}

	public bool ValidateAmmoDrop(ItemType id)
	{
		return !AutosyncModifiersCombiner.AnyTrue(this._dropPreventers, (IAmmoDropPreventer x) => !x.ValidateAmmoDrop(id));
	}

	private static bool AnyTrue<T>(T[] arr, Func<T, bool> selector)
	{
		foreach (T arg in arr)
		{
			if (selector(arg))
			{
				return true;
			}
		}
		return false;
	}

	private static float MinValue<T>(T[] arr, float startMin, Func<T, float> selector, Func<T, bool> validator = null)
	{
		float num = startMin;
		foreach (T arg in arr)
		{
			if (validator == null || validator(arg))
			{
				num = Mathf.Min(num, selector(arg));
			}
		}
		return num;
	}

	private static float CombineMultiplier<T>(T[] arr, Func<T, float> selector, Func<T, bool> validator = null)
	{
		float num = 1f;
		foreach (T arg in arr)
		{
			if (validator == null || validator(arg))
			{
				num *= selector(arg);
			}
		}
		return num;
	}

	private static float CombineAdditive<T>(T[] arr, Func<T, float> selector, Func<T, bool> validator = null)
	{
		float num = 0f;
		foreach (T arg in arr)
		{
			if (validator == null || validator(arg))
			{
				num += selector(arg);
			}
		}
		return num;
	}

	private void FetchModifiers<T>(T[] nonAllocArray, ref T[] targetArray)
	{
		int num = 0;
		SubcomponentBase[] allSubcomponents = this._item.AllSubcomponents;
		for (int i = 0; i < allSubcomponents.Length; i++)
		{
			if (allSubcomponents[i] is T val)
			{
				nonAllocArray[num++] = val;
			}
		}
		targetArray = new T[num];
		Array.Copy(nonAllocArray, targetArray, num);
	}

	public AutosyncModifiersCombiner(ModularAutosyncItem item)
	{
		this._item = item;
		this.FetchModifiers(AutosyncModifiersCombiner.MovementSpeedModifiersNonAlloc, ref this._movementSpeedModifiers);
		this.FetchModifiers(AutosyncModifiersCombiner.StaminaModifiersNonAlloc, ref this._staminaModifiers);
		this.FetchModifiers(AutosyncModifiersCombiner.ZoomModifiersNonAlloc, ref this._zoomModifiers);
		this.FetchModifiers(AutosyncModifiersCombiner.LightEmittersNonAlloc, ref this._lightEmitters);
		this.FetchModifiers(AutosyncModifiersCombiner.DropPreventersNonAlloc, ref this._dropPreventers);
		this.FetchModifiers(AutosyncModifiersCombiner.HumeShieldProvidersNonAlloc, ref this._humeShields);
		this.FetchModifiers(AutosyncModifiersCombiner.AlertDrawersNonAlloc, ref this._alertDrawers);
		item.TryGetSubcomponent<ICustomDescriptionItem>(out this._customDescription);
	}
}
