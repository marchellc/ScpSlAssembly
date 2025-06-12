using System.Collections.Generic;

namespace LiteNetLib4Mirror.Open.Nat;

internal class DeletePortMappingRequestMessage : RequestMessageBase
{
	private readonly Mapping _mapping;

	public DeletePortMappingRequestMessage(Mapping mapping)
	{
		this._mapping = mapping;
	}

	public override IDictionary<string, object> ToXml()
	{
		return new Dictionary<string, object>
		{
			{
				"NewRemoteHost",
				string.Empty
			},
			{
				"NewExternalPort",
				this._mapping.PublicPort
			},
			{
				"NewProtocol",
				(this._mapping.NetworkProtocolType == NetworkProtocolType.Tcp) ? "TCP" : "UDP"
			}
		};
	}
}
