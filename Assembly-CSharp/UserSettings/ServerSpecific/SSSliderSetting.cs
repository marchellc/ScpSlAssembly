using Mirror;
using UnityEngine;

namespace UserSettings.ServerSpecific;

public class SSSliderSetting : ServerSpecificSettingBase
{
	public float SyncFloatValue { get; set; }

	public int SyncIntValue => Mathf.RoundToInt(this.SyncFloatValue);

	public bool SyncDragging { get; set; }

	public float DefaultValue { get; private set; }

	public float MinValue { get; private set; }

	public float MaxValue { get; private set; }

	public bool Integer { get; private set; }

	public string ValueToStringFormat { get; private set; }

	public string FinalDisplayFormat { get; private set; }

	public override string DebugValue => this.SyncFloatValue.ToString();

	public SSSliderSetting(int? id, string label, float minValue, float maxValue, float defaultValue = 0f, bool integer = false, string valueToStringFormat = "0.##", string finalDisplayFormat = "{0}", string hint = null)
	{
		base.SetId(id, label);
		base.Label = label;
		base.HintDescription = hint;
		this.DefaultValue = Mathf.Clamp(defaultValue, minValue, maxValue);
		this.MinValue = minValue;
		this.MaxValue = maxValue;
		this.Integer = integer;
		this.ValueToStringFormat = valueToStringFormat;
		this.FinalDisplayFormat = finalDisplayFormat;
		if (!finalDisplayFormat.Contains("0"))
		{
			this.FinalDisplayFormat += "{0}";
		}
	}

	public override void ApplyDefaultValues()
	{
		this.SyncFloatValue = this.DefaultValue;
	}

	public override void DeserializeValue(NetworkReader reader)
	{
		this.SyncFloatValue = reader.ReadFloat();
		this.SyncDragging = reader.ReadBool();
	}

	public override void SerializeValue(NetworkWriter writer)
	{
		writer.WriteFloat(this.SyncFloatValue);
		writer.WriteBool(this.SyncDragging);
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteFloat(this.DefaultValue);
		writer.WriteFloat(this.MinValue);
		writer.WriteFloat(this.MaxValue);
		writer.WriteBool(this.Integer);
		writer.WriteString(this.ValueToStringFormat);
		writer.WriteString(this.FinalDisplayFormat);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		this.DefaultValue = reader.ReadFloat();
		this.MinValue = reader.ReadFloat();
		this.MaxValue = reader.ReadFloat();
		this.Integer = reader.ReadBool();
		this.ValueToStringFormat = reader.ReadString();
		this.FinalDisplayFormat = reader.ReadString();
	}
}
