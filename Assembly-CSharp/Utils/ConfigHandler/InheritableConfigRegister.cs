using System;
using System.Collections.Generic;
using NorthwoodLib.Pools;

namespace Utils.ConfigHandler
{
	public abstract class InheritableConfigRegister : ConfigRegister
	{
		protected InheritableConfigRegister(ConfigRegister parentConfigRegister = null)
		{
			this.ParentConfigRegister = parentConfigRegister;
		}

		public ConfigRegister ParentConfigRegister { get; protected set; }

		public abstract bool ShouldInheritConfigEntry(ConfigEntry configEntry);

		public abstract void UpdateConfigValueInheritable(ConfigEntry configEntry);

		public override void UpdateConfigValue(ConfigEntry configEntry)
		{
			if (configEntry != null && configEntry.Inherit && this.ParentConfigRegister != null && this.ShouldInheritConfigEntry(configEntry))
			{
				this.ParentConfigRegister.UpdateConfigValue(configEntry);
				return;
			}
			this.UpdateConfigValueInheritable(configEntry);
		}

		public ConfigRegister[] GetConfigRegisterHierarchy(bool highestToLowest = true)
		{
			List<ConfigRegister> list = ListPool<ConfigRegister>.Shared.Rent();
			ConfigRegister configRegister = this;
			while (configRegister != null && !list.Contains(configRegister))
			{
				list.Add(configRegister);
				InheritableConfigRegister inheritableConfigRegister = configRegister as InheritableConfigRegister;
				if (inheritableConfigRegister == null)
				{
					break;
				}
				configRegister = inheritableConfigRegister.ParentConfigRegister;
			}
			if (highestToLowest)
			{
				list.Reverse();
			}
			ConfigRegister[] array = list.ToArray();
			ListPool<ConfigRegister>.Shared.Return(list);
			return array;
		}
	}
}
