using System;
using Utf8Json;

[Serializable]
public struct ServerListItem : IEquatable<ServerListItem>, IJsonSerializable
{
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
		NameFilterPoints = 0;
	}

	public bool Equals(ServerListItem other)
	{
		if (serverId == other.serverId && ip == other.ip && port == other.port && players == other.players && info == other.info && pastebin == other.pastebin && version == other.version && friendlyFire == other.friendlyFire && modded == other.modded && whitelist == other.whitelist)
		{
			return officialCode == other.officialCode;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ServerListItem other)
		{
			return Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		uint num = serverId;
		int num2 = ((num.GetHashCode() * 397) ^ ((ip != null) ? ip.GetHashCode() : 0)) * 397;
		ushort num3 = port;
		int num4 = (((((((((num2 ^ num3.GetHashCode()) * 397) ^ ((players != null) ? players.GetHashCode() : 0)) * 397) ^ ((info != null) ? info.GetHashCode() : 0)) * 397) ^ ((pastebin != null) ? pastebin.GetHashCode() : 0)) * 397) ^ ((version != null) ? version.GetHashCode() : 0)) * 397;
		bool flag = friendlyFire;
		int num5 = (num4 ^ flag.GetHashCode()) * 397;
		flag = modded;
		int num6 = (num5 ^ flag.GetHashCode()) * 397;
		flag = whitelist;
		int num7 = (num6 ^ flag.GetHashCode()) * 397;
		byte b = officialCode;
		return num7 ^ b.GetHashCode();
	}

	public static bool operator ==(ServerListItem left, ServerListItem right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ServerListItem left, ServerListItem right)
	{
		return !left.Equals(right);
	}
}
