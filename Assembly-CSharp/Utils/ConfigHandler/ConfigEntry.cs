using System;

namespace Utils.ConfigHandler
{
	public abstract class ConfigEntry
	{
		public string Key { get; }

		public abstract Type ValueType { get; }

		public abstract object ObjectValue { get; set; }

		public abstract object ObjectDefault { get; set; }

		public bool Inherit { get; }

		public string Name { get; }

		public string Description { get; }

		public ConfigEntry(string key, bool inherit = true, string name = null, string description = null)
		{
			this.Key = key;
			this.Inherit = inherit;
			this.Name = name;
			this.Description = description;
		}

		public ConfigEntry(string key, string name = null, string description = null)
			: this(key, true, name, description)
		{
		}
	}
}
