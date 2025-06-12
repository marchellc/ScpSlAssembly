using System;
using Utf8Json;

public readonly struct ServerList : IEquatable<ServerList>, IJsonSerializable
{
	public readonly ServerListItem[] servers;

	[SerializationConstructor]
	public ServerList(ServerListItem[] servers)
	{
		this.servers = servers;
	}

	public bool Equals(ServerList other)
	{
		return this.servers == other.servers;
	}

	public override bool Equals(object obj)
	{
		if (obj is ServerList other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		if (this.servers == null)
		{
			return 0;
		}
		return this.servers.GetHashCode();
	}

	public static bool operator ==(ServerList left, ServerList right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ServerList left, ServerList right)
	{
		return !left.Equals(right);
	}
}
