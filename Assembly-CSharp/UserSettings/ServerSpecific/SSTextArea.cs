using System;
using Mirror;
using TMPro;
using Utils.Networking;

namespace UserSettings.ServerSpecific;

public class SSTextArea : ServerSpecificSettingBase, ISSUpdatable
{
	public enum FoldoutMode
	{
		NotCollapsable,
		CollapseOnEntry,
		ExtendOnEntry,
		CollapsedByDefault,
		ExtendedByDefault
	}

	public override UserResponseMode ResponseMode => UserResponseMode.None;

	public FoldoutMode Foldout { get; private set; }

	public TextAlignmentOptions AlignmentOptions { get; private set; }

	public override string DebugValue => "N/A";

	internal event Action OnTextUpdated;

	public SSTextArea(int? id, string content, FoldoutMode foldoutMode = FoldoutMode.NotCollapsable, string collapsedText = null, TextAlignmentOptions textAlignment = TextAlignmentOptions.TopLeft)
	{
		base.SetId(id, content);
		base.Label = content;
		base.HintDescription = collapsedText;
		this.Foldout = foldoutMode;
		this.AlignmentOptions = textAlignment;
	}

	public void SendTextUpdate(string newText, bool applyOverride = true, Func<ReferenceHub, bool> receiveFilter = null)
	{
		if (applyOverride)
		{
			base.Label = newText;
		}
		SSSUpdateMessage sSSUpdateMessage = new SSSUpdateMessage(this, delegate(NetworkWriter writer)
		{
			writer.WriteString(newText);
		});
		if (receiveFilter == null)
		{
			sSSUpdateMessage.SendToAuthenticated();
		}
		else
		{
			sSSUpdateMessage.SendToHubsConditionally(receiveFilter);
		}
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteByte((byte)this.Foldout);
		writer.WriteInt((int)this.AlignmentOptions);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		this.Foldout = (FoldoutMode)reader.ReadByte();
		this.AlignmentOptions = (TextAlignmentOptions)reader.ReadInt();
	}

	public override void ApplyDefaultValues()
	{
	}

	public void DeserializeUpdate(NetworkReader reader)
	{
		base.Label = reader.ReadString();
		this.OnTextUpdated?.Invoke();
	}
}
