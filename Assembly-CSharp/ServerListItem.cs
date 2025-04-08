using System;
using Utf8Json;

[Serializable]
public struct ServerListItem : IEquatable<ServerListItem>, IJsonSerializable
{
	[SerializationConstructor]
	public ServerListItem(uint serverId, string ip, ushort port, string players, string info, string pastebin, string version, bool friendlyFire, bool modded, bool whitelist, byte officialCode)
	{
		this.serverId = serverId;
		this.ip = ip;
		this.port = port;
		this.players = players;
		this.info = info;
		this.pastebin = pastebin;
		this.version = version;
		this.friendlyFire = friendlyFire;
		this.modded = modded;
		this.whitelist = whitelist;
		this.officialCode = officialCode;
		this.NameFilterPoints = 0;
	}

	public bool Equals(ServerListItem other)
	{
		return this.serverId == other.serverId && this.ip == other.ip && this.port == other.port && this.players == other.players && this.info == other.info && this.pastebin == other.pastebin && this.version == other.version && this.friendlyFire == other.friendlyFire && this.modded == other.modded && this.whitelist == other.whitelist && this.officialCode == other.officialCode;
	}

	public override bool Equals(object obj)
	{
		if (obj is ServerListItem)
		{
			ServerListItem serverListItem = (ServerListItem)obj;
			return this.Equals(serverListItem);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((((((((((((((((this.serverId.GetHashCode() * 397) ^ ((this.ip != null) ? this.ip.GetHashCode() : 0)) * 397) ^ this.port.GetHashCode()) * 397) ^ ((this.players != null) ? this.players.GetHashCode() : 0)) * 397) ^ ((this.info != null) ? this.info.GetHashCode() : 0)) * 397) ^ ((this.pastebin != null) ? this.pastebin.GetHashCode() : 0)) * 397) ^ ((this.version != null) ? this.version.GetHashCode() : 0)) * 397) ^ this.friendlyFire.GetHashCode()) * 397) ^ this.modded.GetHashCode()) * 397) ^ this.whitelist.GetHashCode()) * 397) ^ this.officialCode.GetHashCode();
	}

	public static bool operator ==(ServerListItem left, ServerListItem right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ServerListItem left, ServerListItem right)
	{
		return !left.Equals(right);
	}

	public readonly uint serverId;

	public readonly string ip;

	public readonly ushort port;

	public readonly string players;

	public readonly string info;

	public readonly string pastebin;

	public readonly string version;

	public readonly bool friendlyFire;

	public readonly bool modded;

	public readonly bool whitelist;

	public readonly byte officialCode;

	public int NameFilterPoints;
}
