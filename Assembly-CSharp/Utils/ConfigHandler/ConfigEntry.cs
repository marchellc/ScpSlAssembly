using System;

namespace Utils.ConfigHandler;

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
		: this(key, inherit: true, name, description)
	{
	}
}
public class ConfigEntry<T> : ConfigEntry
{
	public override Type ValueType => typeof(T);

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
			this.Value = (T)value;
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
			this.Default = (T)value;
		}
	}

	public ConfigEntry(string key, T defaultValue = default(T), bool inherit = true, string name = null, string description = null)
		: base(key, inherit, name, description)
	{
		this.Default = defaultValue;
	}

	public ConfigEntry(string key, T defaultValue = default(T), string name = null, string description = null)
		: this(key, defaultValue, inherit: true, name, description)
	{
	}
}
