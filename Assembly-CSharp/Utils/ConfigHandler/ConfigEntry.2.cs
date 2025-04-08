using System;

namespace Utils.ConfigHandler
{
	public class ConfigEntry<T> : ConfigEntry
	{
		public override Type ValueType
		{
			get
			{
				return typeof(T);
			}
		}

		public T Value { get; set; }

		public T Default { get; set; }

		public override object ObjectValue
		{
			get
			{
				return this.Value;
			}
			set
			{
				this.Value = (T)((object)value);
			}
		}

		public override object ObjectDefault
		{
			get
			{
				return this.Default;
			}
			set
			{
				this.Default = (T)((object)value);
			}
		}

		public ConfigEntry(string key, T defaultValue = default(T), bool inherit = true, string name = null, string description = null)
			: base(key, inherit, name, description)
		{
			this.Default = defaultValue;
		}

		public ConfigEntry(string key, T defaultValue = default(T), string name = null, string description = null)
			: this(key, defaultValue, true, name, description)
		{
		}
	}
}
