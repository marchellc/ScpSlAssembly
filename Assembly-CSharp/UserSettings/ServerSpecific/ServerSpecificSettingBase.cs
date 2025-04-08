using System;
using System.Text;
using Mirror;

namespace UserSettings.ServerSpecific
{
	public abstract class ServerSpecificSettingBase
	{
		public int SettingId { get; private set; }

		public string Label { get; protected set; }

		public string HintDescription { get; protected set; }

		public string PlayerPrefsKey { get; private set; }

		public virtual ServerSpecificSettingBase.UserResponseMode ResponseMode
		{
			get
			{
				return ServerSpecificSettingBase.UserResponseMode.AcquisitionAndChange;
			}
		}

		public abstract string DebugValue { get; }

		public ServerSpecificSettingBase OriginalDefinition
		{
			get
			{
				foreach (ServerSpecificSettingBase serverSpecificSettingBase in ServerSpecificSettingsSync.DefinedSettings)
				{
					if (serverSpecificSettingBase.SettingId == this.SettingId && !(serverSpecificSettingBase.GetType() != base.GetType()))
					{
						return serverSpecificSettingBase;
					}
				}
				return null;
			}
		}

		public void ClientSendValue()
		{
			if (!NetworkClient.active)
			{
				return;
			}
			NetworkClient.Send<SSSClientResponse>(new SSSClientResponse(this), 0);
		}

		public virtual void SerializeEntry(NetworkWriter writer)
		{
			writer.WriteInt(this.SettingId);
			writer.WriteString(this.Label);
			writer.WriteString(this.HintDescription);
		}

		public virtual void DeserializeEntry(NetworkReader reader)
		{
			this.SettingId = reader.ReadInt();
			this.PlayerPrefsKey = this.GeneratePrefsKey();
			this.Label = reader.ReadString();
			this.HintDescription = reader.ReadString();
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
			return string.Format("{0} [ID: {1}] Value: {2}", base.GetType().Name, this.SettingId, this.DebugValue);
		}

		internal void SetId(int? id, string labelFallback)
		{
			if (id == null)
			{
				id = new int?(labelFallback.GetStableHashCode());
			}
			this.SettingId = id.Value;
		}

		private string GeneratePrefsKey()
		{
			ServerSpecificSettingBase.KeyGeneratorSb.Clear();
			ServerSpecificSettingBase.KeyGeneratorSb.Append("SrvSp_");
			ServerSpecificSettingBase.KeyGeneratorSb.Append(ServerSpecificSettingsSync.CurServerPrefsKey);
			ServerSpecificSettingBase.KeyGeneratorSb.Append('_');
			ServerSpecificSettingBase.KeyGeneratorSb.Append(ServerSpecificSettingsSync.GetCodeFromType(base.GetType()));
			ServerSpecificSettingBase.KeyGeneratorSb.Append('_');
			ServerSpecificSettingBase.KeyGeneratorSb.Append(this.SettingId);
			return ServerSpecificSettingBase.KeyGeneratorSb.ToString();
		}

		private static readonly StringBuilder KeyGeneratorSb = new StringBuilder();

		public enum UserResponseMode
		{
			None,
			ChangeOnly,
			AcquisitionAndChange
		}
	}
}
