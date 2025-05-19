using Mirror;
using UnityEngine;

namespace UserSettings.ServerSpecific;

public class SSSliderSetting : ServerSpecificSettingBase
{
	public float SyncFloatValue { get; set; }

	public int SyncIntValue => Mathf.RoundToInt(SyncFloatValue);

	public bool SyncDragging { get; set; }

	public float DefaultValue { get; private set; }

	public float MinValue { get; private set; }

	public float MaxValue { get; private set; }

	public bool Integer { get; private set; }

	public string ValueToStringFormat { get; private set; }

	public string FinalDisplayFormat { get; private set; }

	public override string DebugValue => SyncFloatValue.ToString();

	public SSSliderSetting(int? id, string label, float minValue, float maxValue, float defaultValue = 0f, bool integer = false, string valueToStringFormat = "0.##", string finalDisplayFormat = "{0}", string hint = null)
	{
		SetId(id, label);
		base.Label = label;
		base.HintDescription = hint;
		DefaultValue = Mathf.Clamp(defaultValue, minValue, maxValue);
		MinValue = minValue;
		MaxValue = maxValue;
		Integer = integer;
		ValueToStringFormat = valueToStringFormat;
		FinalDisplayFormat = finalDisplayFormat;
		if (!finalDisplayFormat.Contains("0"))
		{
			FinalDisplayFormat += "{0}";
		}
	}

	public override void ApplyDefaultValues()
	{
		SyncFloatValue = DefaultValue;
	}

	public override void DeserializeValue(NetworkReader reader)
	{
		SyncFloatValue = reader.ReadFloat();
		SyncDragging = reader.ReadBool();
	}

	public override void SerializeValue(NetworkWriter writer)
	{
		writer.WriteFloat(SyncFloatValue);
		writer.WriteBool(SyncDragging);
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteFloat(DefaultValue);
		writer.WriteFloat(MinValue);
		writer.WriteFloat(MaxValue);
		writer.WriteBool(Integer);
		writer.WriteString(ValueToStringFormat);
		writer.WriteString(FinalDisplayFormat);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		DefaultValue = reader.ReadFloat();
		MinValue = reader.ReadFloat();
		MaxValue = reader.ReadFloat();
		Integer = reader.ReadBool();
		ValueToStringFormat = reader.ReadString();
		FinalDisplayFormat = reader.ReadString();
	}
}
