using Mirror;
using UnityEngine;

namespace UserSettings.ServerSpecific;

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
			if (!SyncIsPressed)
			{
				return "Released";
			}
			return "Pressed";
		}
	}

	public SSKeybindSetting(int? id, string label, KeyCode suggestedKey = KeyCode.None, bool preventInteractionOnGui = true, string hint = null)
	{
		SetId(id, label);
		base.Label = label;
		SuggestedKey = suggestedKey;
		PreventInteractionOnGUI = preventInteractionOnGui;
		base.HintDescription = hint;
	}

	public override void ApplyDefaultValues()
	{
		SyncIsPressed = false;
	}

	public override void DeserializeValue(NetworkReader reader)
	{
		SyncIsPressed = reader.ReadBool();
	}

	public override void SerializeValue(NetworkWriter writer)
	{
		writer.WriteBool(SyncIsPressed);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		PreventInteractionOnGUI = reader.ReadBool();
		SuggestedKey = (KeyCode)reader.ReadInt();
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteBool(PreventInteractionOnGUI);
		writer.WriteInt((int)SuggestedKey);
	}
}
