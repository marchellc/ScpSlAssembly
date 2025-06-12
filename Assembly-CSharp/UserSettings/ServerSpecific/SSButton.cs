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
			if (!this.SyncLastPress.IsRunning)
			{
				return "Never pressed";
			}
			return $"Pressed {this.SyncLastPress.Elapsed} ago";
		}
	}

	public SSButton(int? id, string label, string buttonText, float? holdTimeSeconds = null, string hint = null)
	{
		base.SetId(id, label);
		base.Label = label;
		base.HintDescription = hint;
		this.ButtonText = buttonText;
		this.HoldTimeSeconds = Mathf.Max(holdTimeSeconds.GetValueOrDefault(), 0f);
	}

	public override void ApplyDefaultValues()
	{
		this.SyncLastPress.Reset();
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteFloat(this.HoldTimeSeconds);
		writer.WriteString(this.ButtonText);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		this.HoldTimeSeconds = reader.ReadFloat();
		this.ButtonText = reader.ReadString();
	}

	public override void DeserializeValue(NetworkReader reader)
	{
		base.DeserializeValue(reader);
		this.SyncLastPress.Restart();
	}
}
