using System;
using Mirror;
using UnityEngine;

namespace UserSettings.ServerSpecific
{
	public class SSKeybindSetting : ServerSpecificSettingBase
	{
		public bool SyncIsPressed { get; private set; }

		public bool PreventInteractionOnGUI { get; private set; }

		public KeyCode SuggestedKey { get; private set; }

		public KeyCode AssignedKeyCode { get; internal set; }

		public override string DebugValue
		{
			get
			{
				if (!this.SyncIsPressed)
				{
					return "Released";
				}
				return "Pressed";
			}
		}

		public SSKeybindSetting(int? id, string label, KeyCode suggestedKey = KeyCode.None, bool preventInteractionOnGui = true, string hint = null)
		{
			base.SetId(id, label);
			base.Label = label;
			this.SuggestedKey = suggestedKey;
			this.PreventInteractionOnGUI = preventInteractionOnGui;
			base.HintDescription = hint;
		}

		public override void ApplyDefaultValues()
		{
			this.SyncIsPressed = false;
		}

		public override void DeserializeValue(NetworkReader reader)
		{
			this.SyncIsPressed = reader.ReadBool();
		}

		public override void SerializeValue(NetworkWriter writer)
		{
			writer.WriteBool(this.SyncIsPressed);
		}

		public override void DeserializeEntry(NetworkReader reader)
		{
			base.DeserializeEntry(reader);
			this.PreventInteractionOnGUI = reader.ReadBool();
			this.SuggestedKey = (KeyCode)reader.ReadInt();
		}

		public override void SerializeEntry(NetworkWriter writer)
		{
			base.SerializeEntry(writer);
			writer.WriteBool(this.PreventInteractionOnGUI);
			writer.WriteInt((int)this.SuggestedKey);
		}
	}
}
