using System;
using Utf8Json;

public readonly struct ServerListSigned : IEquatable<ServerListSigned>, IJsonSerializable
{
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
		return this.payload == other.payload && this.timestamp == other.timestamp && this.signature == other.signature && this.nonce == other.nonce && this.error == other.error;
	}

	public override bool Equals(object obj)
	{
		if (obj is ServerListSigned)
		{
			ServerListSigned serverListSigned = (ServerListSigned)obj;
			return this.Equals(serverListSigned);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (((((((((this.payload != null) ? this.payload.GetHashCode() : 0) * 397) ^ this.timestamp.GetHashCode()) * 397) ^ ((this.signature != null) ? this.signature.GetHashCode() : 0)) * 397) ^ ((this.nonce != null) ? this.nonce.GetHashCode() : 0)) * 397) ^ ((this.error != null) ? this.error.GetHashCode() : 0);
	}

	public static bool operator ==(ServerListSigned left, ServerListSigned right)
	{
		return left.Equals(right);
	}

	public static bool operator !=(ServerListSigned left, ServerListSigned right)
	{
		return !left.Equals(right);
	}

	public readonly string payload;

	public readonly long timestamp;

	public readonly string signature;

	public readonly string nonce;

	public readonly string error;
}
