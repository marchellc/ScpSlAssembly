using Mirror;

namespace UserSettings.ServerSpecific;

public class SSTwoButtonsSetting : ServerSpecificSettingBase
{
	public bool SyncIsB { get; internal set; }

	public bool SyncIsA => !this.SyncIsB;

	public string OptionA { get; private set; }

	public string OptionB { get; private set; }

	public bool DefaultIsB { get; private set; }

	public override string DebugValue
	{
		get
		{
			if (!this.SyncIsB)
			{
				return "A";
			}
			return "B";
		}
	}

	public SSTwoButtonsSetting(int? id, string label, string optionA, string optionB, bool defaultIsB = false, string hint = null)
	{
		base.SetId(id, label);
		base.Label = label;
		this.OptionA = optionA;
		this.OptionB = optionB;
		this.DefaultIsB = defaultIsB;
		base.HintDescription = hint;
	}

	public override void ApplyDefaultValues()
	{
		this.SyncIsB = this.DefaultIsB;
	}

	public override void DeserializeValue(NetworkReader reader)
	{
		this.SyncIsB = reader.ReadBool();
	}

	public override void SerializeValue(NetworkWriter writer)
	{
		writer.WriteBool(this.SyncIsB);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		this.OptionA = reader.ReadString();
		this.OptionB = reader.ReadString();
		this.DefaultIsB = reader.ReadBool();
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteString(this.OptionA);
		writer.WriteString(this.OptionB);
		writer.WriteBool(this.DefaultIsB);
	}
}
