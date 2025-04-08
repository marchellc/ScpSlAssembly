using System;
using PlayerRoles.FirstPersonControl;
using UnityEngine;

namespace InventorySystem.Items.Autosync
{
	public class AutosyncModifiersCombiner : IMovementSpeedModifier, IStaminaModifier, IZoomModifyingItem, ILightEmittingItem, IAmmoDropPreventer
	{
		public bool StaminaModifierActive
		{
			get
			{
				return this._item.IsEquipped;
			}
		}

		public bool MovementModifierActive
		{
			get
			{
				return this._item.IsEquipped;
			}
		}

		public float MovementSpeedMultiplier
		{
			get
			{
				return AutosyncModifiersCombiner.CombineMultiplier<IMovementSpeedModifier>(this._movementSpeedModifiers, (IMovementSpeedModifier x) => x.MovementSpeedMultiplier, (IMovementSpeedModifier x) => x.MovementModifierActive);
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				return AutosyncModifiersCombiner.MinValue<IMovementSpeedModifier>(this._movementSpeedModifiers, float.MaxValue, (IMovementSpeedModifier x) => x.MovementSpeedLimit, (IMovementSpeedModifier x) => x.MovementModifierActive);
			}
		}

		public float StaminaUsageMultiplier
		{
			get
			{
				return AutosyncModifiersCombiner.CombineMultiplier<IStaminaModifier>(this._staminaModifiers, (IStaminaModifier x) => x.StaminaUsageMultiplier, (IStaminaModifier x) => x.StaminaModifierActive);
			}
		}

		public float StaminaRegenMultiplier
		{
			get
			{
				return AutosyncModifiersCombiner.CombineMultiplier<IStaminaModifier>(this._staminaModifiers, (IStaminaModifier x) => x.StaminaRegenMultiplier, (IStaminaModifier x) => x.StaminaModifierActive);
			}
		}

		public bool SprintingDisabled
		{
			get
			{
				return AutosyncModifiersCombiner.AnyTrue<IStaminaModifier>(this._staminaModifiers, (IStaminaModifier x) => x.StaminaModifierActive && x.SprintingDisabled);
			}
		}

		public float ZoomAmount
		{
			get
			{
				return AutosyncModifiersCombiner.CombineMultiplier<IZoomModifyingItem>(this._zoomModifiers, (IZoomModifyingItem x) => x.ZoomAmount, null);
			}
		}

		public float SensitivityScale
		{
			get
			{
				return AutosyncModifiersCombiner.CombineMultiplier<IZoomModifyingItem>(this._zoomModifiers, (IZoomModifyingItem x) => x.SensitivityScale, null);
			}
		}

		public bool IsEmittingLight
		{
			get
			{
				return AutosyncModifiersCombiner.AnyTrue<ILightEmittingItem>(this._lightEmitters, (ILightEmittingItem x) => x.IsEmittingLight);
			}
		}

		public bool ValidateAmmoDrop(ItemType id)
		{
			return !AutosyncModifiersCombiner.AnyTrue<IAmmoDropPreventer>(this._dropPreventers, (IAmmoDropPreventer x) => !x.ValidateAmmoDrop(id));
		}

		private static bool AnyTrue<T>(T[] arr, Func<T, bool> selector)
		{
			foreach (T t in arr)
			{
				if (selector(t))
				{
					return true;
				}
			}
			return false;
		}

		private static float MinValue<T>(T[] arr, float startMin, Func<T, float> selector, Func<T, bool> validator = null)
		{
			float num = startMin;
			foreach (T t in arr)
			{
				if (validator == null || validator(t))
				{
					num = Mathf.Min(num, selector(t));
				}
			}
			return num;
		}

		private static float CombineMultiplier<T>(T[] arr, Func<T, float> selector, Func<T, bool> validator = null)
		{
			float num = 1f;
			foreach (T t in arr)
			{
				if (validator == null || validator(t))
				{
					num *= selector(t);
				}
			}
			return num;
		}

		private void FetchModifiers<T>(T[] nonAllocArray, ref T[] targetArray)
		{
			int num = 0;
			foreach (SubcomponentBase subcomponentBase in this._item.AllSubcomponents)
			{
				if (subcomponentBase is T)
				{
					T t = subcomponentBase as T;
					nonAllocArray[num++] = t;
				}
			}
			targetArray = new T[num];
			Array.Copy(nonAllocArray, targetArray, num);
		}

		public AutosyncModifiersCombiner(ModularAutosyncItem item)
		{
			this._item = item;
			this.FetchModifiers<IMovementSpeedModifier>(AutosyncModifiersCombiner.MovementSpeedModifiersNonAlloc, ref this._movementSpeedModifiers);
			this.FetchModifiers<IStaminaModifier>(AutosyncModifiersCombiner.StaminaModifiersNonAlloc, ref this._staminaModifiers);
			this.FetchModifiers<IZoomModifyingItem>(AutosyncModifiersCombiner.ZoomModifiersNonAlloc, ref this._zoomModifiers);
			this.FetchModifiers<ILightEmittingItem>(AutosyncModifiersCombiner.LightEmittersNonAlloc, ref this._lightEmitters);
			this.FetchModifiers<IAmmoDropPreventer>(AutosyncModifiersCombiner.DropPreventersNonAlloc, ref this._dropPreventers);
		}

		private static readonly IMovementSpeedModifier[] MovementSpeedModifiersNonAlloc = new IMovementSpeedModifier[16];

		private static readonly IStaminaModifier[] StaminaModifiersNonAlloc = new IStaminaModifier[16];

		private static readonly IZoomModifyingItem[] ZoomModifiersNonAlloc = new IZoomModifyingItem[8];

		private static readonly ILightEmittingItem[] LightEmittersNonAlloc = new ILightEmittingItem[8];

		private static readonly IAmmoDropPreventer[] DropPreventersNonAlloc = new IAmmoDropPreventer[8];

		private readonly IMovementSpeedModifier[] _movementSpeedModifiers;

		private readonly IStaminaModifier[] _staminaModifiers;

		private readonly IZoomModifyingItem[] _zoomModifiers;

		private readonly ILightEmittingItem[] _lightEmitters;

		private readonly IAmmoDropPreventer[] _dropPreventers;

		private readonly ModularAutosyncItem _item;
	}
}
