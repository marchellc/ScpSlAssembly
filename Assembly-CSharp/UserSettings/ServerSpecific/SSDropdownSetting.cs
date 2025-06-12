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
			int num = Mathf.Clamp(this.SyncSelectionIndexRaw, 0, max);
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
			return Mathf.Clamp(this.SyncSelectionIndexRaw, 0, max);
		}
	}

	public override string DebugValue => $"{this.SyncSelectionIndexRaw} ({this.SyncSelectionText})";

	public SSDropdownSetting(int? id, string label, string[] options, int defaultOptionIndex = 0, DropdownEntryType entryType = DropdownEntryType.Regular, string hint = null)
	{
		base.SetId(id, label);
		if (options == null || options.Length == 0)
		{
			options = new string[0];
		}
		base.Label = label;
		base.HintDescription = hint;
		this.Options = options;
		this.EntryType = entryType;
		this.DefaultOptionIndex = defaultOptionIndex;
	}

	public override void ApplyDefaultValues()
	{
		this.SyncSelectionIndexRaw = 0;
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteByte((byte)this.DefaultOptionIndex);
		writer.WriteByte((byte)this.EntryType);
		writer.WriteByte((byte)this.Options.Length);
		this.Options.ForEach(writer.WriteString);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		this.DefaultOptionIndex = reader.ReadByte();
		this.EntryType = (DropdownEntryType)reader.ReadByte();
		int num = reader.ReadByte();
		if (num > 0)
		{
			this.Options = new string[num];
			for (int i = 0; i < this.Options.Length; i++)
			{
				this.Options[i] = reader.ReadString();
			}
		}
		else
		{
			this.Options = new string[1] { string.Empty };
		}
	}

	public override void SerializeValue(NetworkWriter writer)
	{
		base.SerializeValue(writer);
		writer.WriteByte((byte)this.SyncSelectionIndexRaw);
	}

	public override void DeserializeValue(NetworkReader reader)
	{
		base.DeserializeValue(reader);
		this.SyncSelectionIndexRaw = reader.ReadByte();
	}
}
