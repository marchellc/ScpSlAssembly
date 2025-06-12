using System;
using Mirror;
using TMPro;
using Utils.Networking;

namespace UserSettings.ServerSpecific;

public class SSPlaintextSetting : ServerSpecificSettingBase, ISSUpdatable
{
	private int? _characterLimitOriginalCache;

	public string SyncInputText { get; internal set; }

	public string Placeholder { get; private set; }

	public TMP_InputField.ContentType ContentType { get; private set; }

	public int CharacterLimit { get; private set; }

	public override string DebugValue => this.SyncInputText;

	private int CharacterLimitOriginal
	{
		get
		{
			int valueOrDefault = this._characterLimitOriginalCache.GetValueOrDefault();
			if (!this._characterLimitOriginalCache.HasValue)
			{
				valueOrDefault = (base.OriginalDefinition as SSPlaintextSetting).CharacterLimit;
				this._characterLimitOriginalCache = valueOrDefault;
			}
			return this._characterLimitOriginalCache.Value;
		}
	}

	internal event Action OnClearRequested;

	public SSPlaintextSetting(int? id, string label, string placeholder = "...", int characterLimit = 64, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard, string hint = null)
	{
		base.SetId(id, label);
		base.Label = label;
		base.HintDescription = hint;
		this.Placeholder = placeholder;
		this.CharacterLimit = characterLimit;
		this.ContentType = contentType;
	}

	public void SendClearRequest(Func<ReferenceHub, bool> receiveFilter = null)
	{
		SSSUpdateMessage sSSUpdateMessage = new SSSUpdateMessage(this, null);
		if (receiveFilter == null)
		{
			sSSUpdateMessage.SendToAuthenticated();
		}
		else
		{
			sSSUpdateMessage.SendToHubsConditionally(receiveFilter);
		}
	}

	public override void ApplyDefaultValues()
	{
		this.SyncInputText = string.Empty;
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteString(this.Placeholder);
		writer.WriteUShort((ushort)this.CharacterLimit);
		writer.WriteByte((byte)this.ContentType);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		this.Placeholder = reader.ReadString();
		this.CharacterLimit = reader.ReadUShort();
		this.ContentType = (TMP_InputField.ContentType)reader.ReadByte();
	}

	public override void SerializeValue(NetworkWriter writer)
	{
		base.SerializeValue(writer);
		writer.WriteString(this.SyncInputText);
	}

	public override void DeserializeValue(NetworkReader reader)
	{
		base.DeserializeValue(reader);
		this.SyncInputText = this.ValidateInputText(reader.ReadString());
	}

	public void DeserializeUpdate(NetworkReader reader)
	{
		this.OnClearRequested?.Invoke();
	}

	private string ValidateInputText(string text)
	{
		if (text == null)
		{
			return string.Empty;
		}
		int characterLimitOriginal = this.CharacterLimitOriginal;
		if (text.Length > characterLimitOriginal)
		{
			text = text.Remove(characterLimitOriginal);
		}
		return text;
	}
}
