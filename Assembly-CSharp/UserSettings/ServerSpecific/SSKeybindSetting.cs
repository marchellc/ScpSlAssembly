using Mirror;
using PlayerRoles.Spectating;
using UnityEngine;

namespace UserSettings.ServerSpecific;

public class SSKeybindSetting : ServerSpecificSettingBase
{
	public bool SyncIsPressed { get; private set; }

	public bool PreventInteractionOnGUI { get; private set; }

	public bool AllowSpectatorTrigger { get; private set; }

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

	private bool IsLocallySpectating
	{
		get
		{
			if (!ReferenceHub.TryGetLocalHub(out var hub))
			{
				return false;
			}
			if (!(hub.roleManager.CurrentRole is SpectatorRole))
			{
				return false;
			}
			return true;
		}
	}

	public SSKeybindSetting(int? id, string label, KeyCode suggestedKey = KeyCode.None, bool preventInteractionOnGui = true, bool allowSpectatorTrigger = true, string hint = null)
	{
		base.SetId(id, label);
		base.Label = label;
		this.SuggestedKey = suggestedKey;
		this.PreventInteractionOnGUI = preventInteractionOnGui;
		this.AllowSpectatorTrigger = allowSpectatorTrigger;
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
		this.AllowSpectatorTrigger = reader.ReadBool();
		this.SuggestedKey = (KeyCode)reader.ReadInt();
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteBool(this.PreventInteractionOnGUI);
		writer.WriteBool(this.AllowSpectatorTrigger);
		writer.WriteInt((int)this.SuggestedKey);
	}
}
