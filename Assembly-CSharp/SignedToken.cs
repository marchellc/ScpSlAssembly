using System;
using Cryptography;
using GameCore;
using Mirror;
using NorthwoodLib;
using UnityEngine;
using Utf8Json;

public class SignedToken : IJsonSerializable
{
	[SerializationConstructor]
	public SignedToken(string token, string signature)
	{
		this.token = token;
		this.signature = signature;
	}

	public void Serialize(NetworkWriter writer)
	{
		writer.WriteString(this.token);
		writer.WriteString(this.signature);
	}

	public static SignedToken Deserialize(NetworkReader reader)
	{
		return new SignedToken(reader.ReadString(), reader.ReadString());
	}

	public bool TryGetToken<T>(string usage, out T token, out string error, out string userId, int expTimeOffset = 0) where T : Token, IJsonSerializable
	{
		bool flag;
		try
		{
			token = default(T);
			error = null;
			userId = null;
			if (!ECDSA.Verify(this.token, this.signature, ServerConsole.PublicKey))
			{
				error = "invalid signature of token used for " + usage;
				flag = false;
			}
			else
			{
				T t = JsonSerialize.FromJson<T>(StringUtils.Base64Decode(this.token));
				userId = t.UserId;
				if (t.TokenVersion != 2)
				{
					error = string.Format("invalid token version. Expected: {0}, got: {1}", 2, t.TokenVersion);
					flag = false;
				}
				else if (!t.Usage.Equals(usage, StringComparison.Ordinal))
				{
					error = "invalid token usage. Expected: " + usage + ", got: " + t.Usage;
					flag = false;
				}
				else if (t.ExpirationTime < DateTime.UtcNow.AddMinutes((double)(-(double)expTimeOffset)))
				{
					error = "expired token used for " + usage;
					flag = false;
				}
				else if (t.IssuanceTime > DateTime.UtcNow.AddMinutes(20.0))
				{
					error = "non-issued token used for " + usage;
					flag = false;
				}
				else if (t.TestSignature && !CentralServer.TestServer)
				{
					error = "test-only token used for " + usage;
					flag = false;
				}
				else
				{
					token = t;
					flag = true;
				}
			}
		}
		catch (Exception ex)
		{
			global::GameCore.Console.AddLog("Failed to TryGetToken<>: " + ex.Message, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			global::GameCore.Console.AddLog(ex.StackTrace, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			throw;
		}
		return flag;
	}

	public readonly string token;

	public readonly string signature;
}
