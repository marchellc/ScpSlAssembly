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

	public override string DebugValue => SyncInputText;

	private int CharacterLimitOriginal
	{
		get
		{
			int valueOrDefault = _characterLimitOriginalCache.GetValueOrDefault();
			if (!_characterLimitOriginalCache.HasValue)
			{
				valueOrDefault = (base.OriginalDefinition as SSPlaintextSetting).CharacterLimit;
				_characterLimitOriginalCache = valueOrDefault;
			}
			return _characterLimitOriginalCache.Value;
		}
	}

	internal event Action OnClearRequested;

	public SSPlaintextSetting(int? id, string label, string placeholder = "...", int characterLimit = 64, TMP_InputField.ContentType contentType = TMP_InputField.ContentType.Standard, string hint = null)
	{
		SetId(id, label);
		base.Label = label;
		base.HintDescription = hint;
		Placeholder = placeholder;
		CharacterLimit = characterLimit;
		ContentType = contentType;
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
		SyncInputText = string.Empty;
	}

	public override void SerializeEntry(NetworkWriter writer)
	{
		base.SerializeEntry(writer);
		writer.WriteString(Placeholder);
		writer.WriteUShort((ushort)CharacterLimit);
		writer.WriteByte((byte)ContentType);
	}

	public override void DeserializeEntry(NetworkReader reader)
	{
		base.DeserializeEntry(reader);
		Placeholder = reader.ReadString();
		CharacterLimit = reader.ReadUShort();
		ContentType = (TMP_InputField.ContentType)reader.ReadByte();
	}

	public override void SerializeValue(NetworkWriter writer)
	{
		base.SerializeValue(writer);
		writer.WriteString(SyncInputText);
	}

	public override void DeserializeValue(NetworkReader reader)
	{
		base.DeserializeValue(reader);
		SyncInputText = ValidateInputText(reader.ReadString());
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
		int characterLimitOriginal = CharacterLimitOriginal;
		if (text.Length > characterLimitOriginal)
		{
			text = text.Remove(characterLimitOriginal);
		}
		return text;
	}
}
