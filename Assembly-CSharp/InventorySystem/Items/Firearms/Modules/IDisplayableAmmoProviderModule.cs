using System;

namespace InventorySystem.Items.Firearms.Modules
{
	public interface IDisplayableAmmoProviderModule
	{
		DisplayAmmoValues PredictedDisplayAmmo { get; }

		DisplayAmmoValues GetDisplayAmmoForSerial(ushort serial);

		public static DisplayAmmoValues GetCombinedDisplayAmmo(Firearm firearm)
		{
			int num = 0;
			int num2 = 0;
			ModuleBase[] modules = firearm.Modules;
			for (int i = 0; i < modules.Length; i++)
			{
				IDisplayableAmmoProviderModule displayableAmmoProviderModule = modules[i] as IDisplayableAmmoProviderModule;
				if (displayableAmmoProviderModule != null)
				{
					DisplayAmmoValues predictedDisplayAmmo = displayableAmmoProviderModule.PredictedDisplayAmmo;
					num += predictedDisplayAmmo.Magazines;
					num2 += predictedDisplayAmmo.Chambered;
				}
			}
			return new DisplayAmmoValues(num, num2);
		}

		public static DisplayAmmoValues GetCombinedDisplayAmmo(ItemIdentifier id)
		{
			Firearm firearm;
			if (!InventoryItemLoader.TryGetItem<Firearm>(id.TypeId, out firearm))
			{
				return default(DisplayAmmoValues);
			}
			ushort serialNumber = id.SerialNumber;
			int num = 0;
			int num2 = 0;
			ModuleBase[] modules = firearm.Modules;
			for (int i = 0; i < modules.Length; i++)
			{
				IDisplayableAmmoProviderModule displayableAmmoProviderModule = modules[i] as IDisplayableAmmoProviderModule;
				if (displayableAmmoProviderModule != null)
				{
					DisplayAmmoValues displayAmmoForSerial = displayableAmmoProviderModule.GetDisplayAmmoForSerial(serialNumber);
					num += displayAmmoForSerial.Magazines;
					num2 += displayAmmoForSerial.Chambered;
				}
			}
			return new DisplayAmmoValues(num, num2);
		}
	}
}
