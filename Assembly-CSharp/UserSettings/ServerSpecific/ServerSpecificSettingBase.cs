using System.Text;
using Mirror;

namespace UserSettings.ServerSpecific;

public abstract class ServerSpecificSettingBase
{
	public enum UserResponseMode
	{
		None,
		ChangeOnly,
		AcquisitionAndChange
	}

	private static readonly StringBuilder KeyGeneratorSb = new StringBuilder();

	public int SettingId { get; private set; }

	public string Label { get; protected set; }

	public string HintDescription { get; protected set; }

	public string PlayerPrefsKey { get; private set; }

	public virtual UserResponseMode ResponseMode => UserResponseMode.AcquisitionAndChange;

	public abstract string DebugValue { get; }

	public ServerSpecificSettingBase OriginalDefinition
	{
		get
		{
			ServerSpecificSettingBase[] definedSettings = ServerSpecificSettingsSync.DefinedSettings;
			foreach (ServerSpecificSettingBase serverSpecificSettingBase in definedSettings)
			{
				if (serverSpecificSettingBase.SettingId == SettingId && !(serverSpecificSettingBase.GetType() != GetType()))
				{
					return serverSpecificSettingBase;
				}
			}
			return null;
		}
	}

	public void ClientSendValue()
	{
		if (NetworkClient.active)
		{
			NetworkClient.Send(new SSSClientResponse(this));
		}
	}

	public virtual void SerializeEntry(NetworkWriter writer)
	{
		writer.WriteInt(SettingId);
		writer.WriteString(Label);
		writer.WriteString(HintDescription);
	}

	public virtual void DeserializeEntry(NetworkReader reader)
	{
		SettingId = reader.ReadInt();
		PlayerPrefsKey = GeneratePrefsKey();
		Label = reader.ReadString();
		HintDescription = reader.ReadString();
	}

	public virtual void OnUpdate()
	{
	}

	public virtual void SerializeValue(NetworkWriter writer)
	{
	}

	public virtual void DeserializeValue(NetworkReader reader)
	{
	}

	public abstract void ApplyDefaultValues();

	public override string ToString()
	{
		return $"{GetType().Name} [ID: {SettingId}] Value: {DebugValue}";
	}

	internal void SetId(int? id, string labelFallback)
	{
		if (!id.HasValue)
		{
			id = labelFallback.GetStableHashCode();
		}
		SettingId = id.Value;
	}

	private string GeneratePrefsKey()
	{
		KeyGeneratorSb.Clear();
		KeyGeneratorSb.Append("SrvSp_");
		KeyGeneratorSb.Append(ServerSpecificSettingsSync.CurServerPrefsKey);
		KeyGeneratorSb.Append('_');
		KeyGeneratorSb.Append(ServerSpecificSettingsSync.GetCodeFromType(GetType()));
		KeyGeneratorSb.Append('_');
		KeyGeneratorSb.Append(SettingId);
		return KeyGeneratorSb.ToString();
	}
}
