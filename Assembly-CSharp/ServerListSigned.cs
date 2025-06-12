using System;
using Utf8Json;

public readonly struct ServerListSigned : IEquatable<ServerListSigned>, IJsonSerializable
{
	public readonly string payload;

	public readonly long timestamp;

	public readonly string signature;

	public readonly string nonce;

	public readonly string error;

	[SerializationConstructor]
	public ServerListSigned(string payload, long timestamp, string signature, string nonce, string error)
	{
		this.payload = payload;
		this.timestamp = timestamp;
		this.signature = signature;
		this.nonce = nonce;
		this.error = error;
	}

	public bool Equals(ServerListSigned other)
	{
		if (this.payload == other.payload && this.timestamp == other.timestamp && this.signature == other.signature && this.nonce == other.nonce)
		{
			return this.error == other.error;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is ServerListSigned other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		int num = ((this.payload != null) ? this.payload.GetHashCode() : 0) * 397;
		long num2 = this.timestamp;
		return ((((((num ^ num2.GetHashCode()) * 397) ^ ((this.signature != null) ? this.signature.GetHashCode() : 0)) * 397) ^ ((this.nonce != null) ? this.nonce.GetHashCode() : 0)) * 397) ^ ((this.error != null) ? this.error.GetHashCode() : 0);
	}

	public static bool operator ==(ServerListSigned left, ServerListSigned right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ServerListSigned left, ServerListSigned right)
	{
		return !left.Equals(right);
	}
}
