using System.Collections.Generic;
using System.Net;

namespace LiteNetLib4Mirror.Open.Nat;

internal class CreatePortMappingRequestMessage : RequestMessageBase
{
	private readonly Mapping _mapping;

	public CreatePortMappingRequestMessage(Mapping mapping)
	{
		this._mapping = mapping;
	}

	public override IDictionary<string, object> ToXml()
	{
		string value = (this._mapping.PublicIP.Equals(IPAddress.None) ? string.Empty : this._mapping.PublicIP.ToString());
		return new Dictionary<string, object>
		{
			{ "NewRemoteHost", value },
			{
				"NewExternalPort",
				this._mapping.PublicPort
			},
			{
				"NewProtocol",
				(this._mapping.NetworkProtocolType == NetworkProtocolType.Tcp) ? "TCP" : "UDP"
			},
			{
				"NewInternalPort",
				this._mapping.PrivatePort
			},
			{
				"NewInternalClient",
				this._mapping.PrivateIP
			},
			{ "NewEnabled", 1 },
			{
				"NewPortMappingDescription",
				this._mapping.Description
			},
			{
				"NewLeaseDuration",
				this._mapping.Lifetime
			}
		};
	}
}
