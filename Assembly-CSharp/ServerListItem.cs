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
		this.NameFilterPoints = 0;
	}

	public bool Equals(ServerListItem other)
	{
		if (this.serverId == other.serverId && this.ip == other.ip && this.port == other.port && this.players == other.players && this.info == other.info && this.pastebin == other.pastebin && this.version == other.version && this.friendlyFire == other.friendlyFire && this.modded == other.modded && this.whitelist == other.whitelist)
		{
			return this.officialCode == other.officialCode;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ServerListItem other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		uint num = this.serverId;
		int num2 = ((num.GetHashCode() * 397) ^ ((this.ip != null) ? this.ip.GetHashCode() : 0)) * 397;
		ushort num3 = this.port;
		int num4 = (((((((((num2 ^ num3.GetHashCode()) * 397) ^ ((this.players != null) ? this.players.GetHashCode() : 0)) * 397) ^ ((this.info != null) ? this.info.GetHashCode() : 0)) * 397) ^ ((this.pastebin != null) ? this.pastebin.GetHashCode() : 0)) * 397) ^ ((this.version != null) ? this.version.GetHashCode() : 0)) * 397;
		bool flag = this.friendlyFire;
		int num5 = (num4 ^ flag.GetHashCode()) * 397;
		flag = this.modded;
		int num6 = (num5 ^ flag.GetHashCode()) * 397;
		flag = this.whitelist;
		int num7 = (num6 ^ flag.GetHashCode()) * 397;
		byte b = this.officialCode;
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
