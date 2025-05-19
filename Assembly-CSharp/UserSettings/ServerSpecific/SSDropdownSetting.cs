using Mirror;
using UnityEngine;

namespace UserSettings.ServerSpecific;

public class SSDropdownSetting : ServerSpecificSettingBase
{
	public enum DropdownEntryType
	{
		Regular,
		Scrollable,
		ScrollableLoop,
		Hybrid,
		HybridLoop
	}

	public string[] Options { get; private set; }

	public int DefaultOptionIndex { get; private set; }

	public DropdownEntryType EntryType { get; private set; }

	public int SyncSelectionIndexRaw { get; internal set; }

	public string SyncSelectionText
	{
		get
		{
			if (!(base.OriginalDefinition is SSDropdownSetting sSDropdownSetting))
			{
				return string.Empty;
			}
			int max = sSDropdownSetting.Options.Length - 1;
			int num = Mathf.Clamp(SyncSelectionIndexRaw, 0, max);
			return sSDropdownSetting.Options[num];
		}
	}

	public int SyncSelectionIndexValidated
	{
		get
		{
			if (!(base.OriginalDefinition is SSDropdownSetting sSDropdownSetting))
			{
				return 0;
			}
			int max = sSDropdownSetting.Options.Length - 1;
			return Mathf.Clamp(SyncSelectionIndexRaw, 0, max);
		}
	}

	public override string DebugValue => $"{SyncSelectionIndexRaw} ({SyncSelectionText})";

	public SSDropdownSetting(int? id, string label, string[] options, int defaultOptionIndex = 0, DropdownEntryType entryType = DropdownEntryType.Regular, string hint = null)
	{
		SetId(id, label);
		if (options == null || options.Length == 0)
		{
			options = new string[0];
		}
		base.Label = label;
		base.HintDescription = hint;
		Options = options;
		EntryType = entryType;
		DefaultOptionIndex = defaultOptionIndex;
	}

	public override void ApplyDefaultValues()
	{
		SyncSelectionIndexRaw = 0;
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteByte((byte)DefaultOptionIndex);
		writer.WriteByte((byte)EntryType);
		writer.WriteByte((byte)Options.Length);
		Options.ForEach(writer.WriteString);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		DefaultOptionIndex = reader.ReadByte();
		EntryType = (DropdownEntryType)reader.ReadByte();
		int num = reader.ReadByte();
		if (num > 0)
		{
			Options = new string[num];
			for (int i = 0; i < Options.Length; i++)
			{
				Options[i] = reader.ReadString();
			}
		}
		else
		{
			Options = new string[1] { string.Empty };
		}
	}

	public override void SerializeValue(NetworkWriter writer)
	{
		base.SerializeValue(writer);
		writer.WriteByte((byte)SyncSelectionIndexRaw);
	}

	public override void DeserializeValue(NetworkReader reader)
	{
		base.DeserializeValue(reader);
		SyncSelectionIndexRaw = reader.ReadByte();
	}
}
