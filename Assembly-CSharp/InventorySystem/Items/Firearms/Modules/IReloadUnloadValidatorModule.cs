using System;

namespace InventorySystem.Items.Firearms.Modules
{
	public interface IReloadUnloadValidatorModule
	{
		IReloadUnloadValidatorModule.Authorization ReloadAuthorization { get; }

		IReloadUnloadValidatorModule.Authorization UnloadAuthorization { get; }

		public static bool ValidateReload(Firearm firearm)
		{
			return IReloadUnloadValidatorModule.ValidateAny(firearm, (IReloadUnloadValidatorModule x) => x.ReloadAuthorization) && !firearm.PrimaryActionBlocked;
		}

		public static bool ValidateUnload(Firearm firearm)
		{
			return IReloadUnloadValidatorModule.ValidateAny(firearm, (IReloadUnloadValidatorModule x) => x.UnloadAuthorization) && !firearm.PrimaryActionBlocked;
		}

		private static bool ValidateAny(Firearm firearm, Func<IReloadUnloadValidatorModule, IReloadUnloadValidatorModule.Authorization> authorizationFetcher)
		{
			bool flag = false;
			ModuleBase[] modules = firearm.Modules;
			for (int i = 0; i < modules.Length; i++)
			{
				IReloadUnloadValidatorModule reloadUnloadValidatorModule = modules[i] as IReloadUnloadValidatorModule;
				if (reloadUnloadValidatorModule != null)
				{
					IReloadUnloadValidatorModule.Authorization authorization = authorizationFetcher(reloadUnloadValidatorModule);
					if (authorization != IReloadUnloadValidatorModule.Authorization.Allowed)
					{
						if (authorization == IReloadUnloadValidatorModule.Authorization.Vetoed)
						{
							return false;
						}
					}
					else
					{
						flag = true;
					}
				}
			}
			return flag;
		}

		public enum Authorization
		{
			Idle,
			Allowed,
			Vetoed
		}
	}
}
