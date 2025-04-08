using System;
using Mirror;
using TMPro;
using Utils.Networking;

namespace UserSettings.ServerSpecific
{
	public class SSTextArea : ServerSpecificSettingBase, ISSUpdatable
	{
		internal event Action OnTextUpdated;

		public override ServerSpecificSettingBase.UserResponseMode ResponseMode
		{
			get
			{
				return ServerSpecificSettingBase.UserResponseMode.None;
			}
		}

		public SSTextArea.FoldoutMode Foldout { get; private set; }

		public TextAlignmentOptions AlignmentOptions { get; private set; }

		public override string DebugValue
		{
			get
			{
				return "N/A";
			}
		}

		public SSTextArea(int? id, string content, SSTextArea.FoldoutMode foldoutMode = SSTextArea.FoldoutMode.NotCollapsable, string collapsedText = null, TextAlignmentOptions textAlignment = TextAlignmentOptions.TopLeft)
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
			SSSUpdateMessage sssupdateMessage = new SSSUpdateMessage(this, delegate(NetworkWriter writer)
			{
				writer.WriteString(newText);
			});
			if (receiveFilter == null)
			{
				sssupdateMessage.SendToAuthenticated(0);
				return;
			}
			sssupdateMessage.SendToHubsConditionally(receiveFilter, 0);
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
			this.Foldout = (SSTextArea.FoldoutMode)reader.ReadByte();
			this.AlignmentOptions = (TextAlignmentOptions)reader.ReadInt();
		}

		public override void ApplyDefaultValues()
		{
		}

		public void DeserializeUpdate(NetworkReader reader)
		{
			base.Label = reader.ReadString();
			Action onTextUpdated = this.OnTextUpdated;
			if (onTextUpdated == null)
			{
				return;
			}
			onTextUpdated();
		}

		public enum FoldoutMode
		{
			NotCollapsable,
			CollapseOnEntry,
			ExtendOnEntry,
			CollapsedByDefault,
			ExtendedByDefault
		}
	}
}
