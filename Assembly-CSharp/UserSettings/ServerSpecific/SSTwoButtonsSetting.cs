using Mirror;

namespace UserSettings.ServerSpecific;

public class SSTwoButtonsSetting : ServerSpecificSettingBase
{
	public bool SyncIsB { get; internal set; }

	public bool SyncIsA => !SyncIsB;

	public string OptionA { get; private set; }

	public string OptionB { get; private set; }

	public bool DefaultIsB { get; private set; }

	public override string DebugValue
	{
		get
		{
			if (!SyncIsB)
			{
				return "A";
			}
			return "B";
		}
	}

	public SSTwoButtonsSetting(int? id, string label, string optionA, string optionB, bool defaultIsB = false, string hint = null)
	{
		SetId(id, label);
		base.Label = label;
		OptionA = optionA;
		OptionB = optionB;
		DefaultIsB = defaultIsB;
		base.HintDescription = hint;
	}

	public override void ApplyDefaultValues()
	{
		SyncIsB = DefaultIsB;
	}

	public override void DeserializeValue(NetworkReader reader)
	{
		SyncIsB = reader.ReadBool();
	}

	public override void SerializeValue(NetworkWriter writer)
	{
		writer.WriteBool(SyncIsB);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		OptionA = reader.ReadString();
		OptionB = reader.ReadString();
		DefaultIsB = reader.ReadBool();
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteString(OptionA);
		writer.WriteString(OptionB);
		writer.WriteBool(DefaultIsB);
	}
}
