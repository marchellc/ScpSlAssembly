using System.Diagnostics;
using Mirror;
using UnityEngine;

namespace UserSettings.ServerSpecific;

public class SSButton : ServerSpecificSettingBase
{
	public readonly Stopwatch SyncLastPress = new Stopwatch();

	public float HoldTimeSeconds { get; private set; }

	public string ButtonText { get; private set; }

	public override UserResponseMode ResponseMode => UserResponseMode.ChangeOnly;

	public override string DebugValue
	{
		get
		{
			if (!SyncLastPress.IsRunning)
			{
				return "Never pressed";
			}
			return $"Pressed {SyncLastPress.Elapsed} ago";
		}
	}

	public SSButton(int? id, string label, string buttonText, float? holdTimeSeconds = null, string hint = null)
	{
		SetId(id, label);
		base.Label = label;
		base.HintDescription = hint;
		ButtonText = buttonText;
		HoldTimeSeconds = Mathf.Max(holdTimeSeconds.GetValueOrDefault(), 0f);
	}

	public override void ApplyDefaultValues()
	{
		SyncLastPress.Reset();
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteFloat(HoldTimeSeconds);
		writer.WriteString(ButtonText);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		HoldTimeSeconds = reader.ReadFloat();
		ButtonText = reader.ReadString();
	}

	public override void DeserializeValue(NetworkReader reader)
	{
		base.DeserializeValue(reader);
		SyncLastPress.Restart();
	}
}
