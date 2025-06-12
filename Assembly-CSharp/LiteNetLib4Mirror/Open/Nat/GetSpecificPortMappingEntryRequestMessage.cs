using System.Collections.Generic;

namespace LiteNetLib4Mirror.Open.Nat;

internal class GetSpecificPortMappingEntryRequestMessage : RequestMessageBase
{
	private readonly int _externalPort;

	private readonly NetworkProtocolType _networkProtocolType;

	public GetSpecificPortMappingEntryRequestMessage(NetworkProtocolType networkProtocolType, int externalPort)
	{
		this._networkProtocolType = networkProtocolType;
		this._externalPort = externalPort;
	}

	public override IDictionary<string, object> ToXml()
	{
		return new Dictionary<string, object>
		{
			{
				"NewRemoteHost",
				string.Empty
			},
			{ "NewExternalPort", this._externalPort },
			{
				"NewProtocol",
				(this._networkProtocolType == NetworkProtocolType.Tcp) ? "TCP" : "UDP"
			}
		};
	}
}
