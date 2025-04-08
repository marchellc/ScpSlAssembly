using System;
using Mirror;
using UnityEngine;

namespace UserSettings.ServerSpecific
{
	public class SSDropdownSetting : ServerSpecificSettingBase
	{
		public string[] Options { get; private set; }

		public int DefaultOptionIndex { get; private set; }

		public SSDropdownSetting.DropdownEntryType EntryType { get; private set; }

		public int SyncSelectionIndexRaw { get; internal set; }

		public string SyncSelectionText
		{
			get
			{
				SSDropdownSetting ssdropdownSetting = base.OriginalDefinition as SSDropdownSetting;
				if (ssdropdownSetting == null)
				{
					return string.Empty;
				}
				int num = ssdropdownSetting.Options.Length - 1;
				int num2 = Mathf.Clamp(this.SyncSelectionIndexRaw, 0, num);
				return ssdropdownSetting.Options[num2];
			}
		}

		public int SyncSelectionIndexValidated
		{
			get
			{
				SSDropdownSetting ssdropdownSetting = base.OriginalDefinition as SSDropdownSetting;
				if (ssdropdownSetting == null)
				{
					return 0;
				}
				int num = ssdropdownSetting.Options.Length - 1;
				return Mathf.Clamp(this.SyncSelectionIndexRaw, 0, num);
			}
		}

		public override string DebugValue
		{
			get
			{
				return string.Format("{0} ({1})", this.SyncSelectionIndexRaw, this.SyncSelectionText);
			}
		}

		public SSDropdownSetting(int? id, string label, string[] options, int defaultOptionIndex = 0, SSDropdownSetting.DropdownEntryType entryType = SSDropdownSetting.DropdownEntryType.Regular, string hint = null)
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
			this.Options.ForEach(new Action<string>(writer.WriteString));
		}

		public override void DeserializeEntry(NetworkReader reader)
		{
			base.DeserializeEntry(reader);
			this.DefaultOptionIndex = (int)reader.ReadByte();
			this.EntryType = (SSDropdownSetting.DropdownEntryType)reader.ReadByte();
			int num = (int)reader.ReadByte();
			if (num > 0)
			{
				this.Options = new string[num];
				for (int i = 0; i < this.Options.Length; i++)
				{
					this.Options[i] = reader.ReadString();
				}
				return;
			}
			this.Options = new string[] { string.Empty };
		}

		public override void SerializeValue(NetworkWriter writer)
		{
			base.SerializeValue(writer);
			writer.WriteByte((byte)this.SyncSelectionIndexRaw);
		}

		public override void DeserializeValue(NetworkReader reader)
		{
			base.DeserializeValue(reader);
			this.SyncSelectionIndexRaw = (int)reader.ReadByte();
		}

		public enum DropdownEntryType
		{
			Regular,
			Scrollable,
			ScrollableLoop,
			Hybrid,
			HybridLoop
		}
	}
}
