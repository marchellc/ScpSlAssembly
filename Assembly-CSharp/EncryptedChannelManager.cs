using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Cryptography;
using GameCore;
using Mirror;
using Org.BouncyCastle.Crypto;
using Org.BouncyCastle.Crypto.Agreement;
using Org.BouncyCastle.Security;
using UnityEngine;

public class EncryptedChannelManager : NetworkBehaviour
{
	internal AsymmetricCipherKeyPair EcdhKeys { get; private set; }

	internal void PrepareExchange()
	{
		if (this._exchange != null)
		{
			return;
		}
		this.EcdhKeys = ECDH.GenerateKeys(384);
		this._exchange = ECDH.Init(this.EcdhKeys);
	}

	internal void ServerProcessExchange(string publicKey)
	{
		if (this.EncryptionKey != null)
		{
			return;
		}
		if (EncryptedChannelManager.CryptographyDebug)
		{
			ReferenceHub referenceHub;
			ServerConsole.AddLog("Received ECDH parameters from " + (ReferenceHub.TryGetHub(base.gameObject, out referenceHub) ? referenceHub.LoggedNameFromRefHub() : "(unknown)") + ".", ConsoleColor.Gray, false);
		}
		this.EncryptionKey = ECDH.DeriveKey(this._exchange, ECDSA.PublicKeyFromString(publicKey));
	}

	public static void ReplaceServerHandler(EncryptedChannelManager.EncryptedChannel channel, EncryptedChannelManager.EncryptedMessageServerHandler handler)
	{
		EncryptedChannelManager.ServerChannelHandlers[channel] = handler;
	}

	public static void ReplaceClientHandler(EncryptedChannelManager.EncryptedChannel channel, EncryptedChannelManager.EncryptedMessageClientHandler clientHandler)
	{
		EncryptedChannelManager.ClientChannelHandlers[channel] = clientHandler;
	}

	private void Start()
	{
		if (!base.isLocalPlayer)
		{
			return;
		}
		if (NetworkServer.active)
		{
			if (string.IsNullOrEmpty(this.ServerRandom))
			{
				this.NetworkServerRandom = RandomGenerator.GetStringSecure(32);
				ServerConsole.AddLog("Generated random salt: " + this.ServerRandom, ConsoleColor.Gray, false);
			}
			NetworkServer.ReplaceHandler<EncryptedChannelManager.EncryptedMessageOutside>(new Action<NetworkConnectionToClient, EncryptedChannelManager.EncryptedMessageOutside>(EncryptedChannelManager.ServerReceivePackedMessage), true);
		}
		NetworkClient.ReplaceHandler<EncryptedChannelManager.EncryptedMessageOutside>(new Action<EncryptedChannelManager.EncryptedMessageOutside>(EncryptedChannelManager.ClientReceivePackedMessage), true);
	}

	private void ReceivedSaltHook(string p, string n)
	{
		if (!NetworkServer.active && n != null)
		{
			global::GameCore.Console.AddLog("Received random salt from game server: " + n, Color.gray, false, global::GameCore.Console.ConsoleLogType.Log);
		}
	}

	private static void ServerReceivePackedMessage(NetworkConnection conn, EncryptedChannelManager.EncryptedMessageOutside packed)
	{
		ReferenceHub referenceHub;
		if (!NetworkServer.active || !ReferenceHub.TryGetHub(conn, out referenceHub))
		{
			return;
		}
		EncryptedChannelManager.EncryptedMessage encryptedMessage;
		EncryptedChannelManager.SecurityLevel securityLevel;
		if (!referenceHub.encryptedChannelManager.TryUnpack(packed, out encryptedMessage, out securityLevel, NetworkServer.active && conn.identity.isLocalPlayer))
		{
			return;
		}
		EncryptedChannelManager.EncryptedMessageServerHandler encryptedMessageServerHandler;
		if (!EncryptedChannelManager.ServerChannelHandlers.TryGetValue(encryptedMessage.Channel, out encryptedMessageServerHandler))
		{
			global::GameCore.Console.AddLog(string.Format("No handler is registered for encrypted channel {0} (server).", encryptedMessage.Channel), Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			return;
		}
		try
		{
			encryptedMessageServerHandler(referenceHub, encryptedMessage, securityLevel);
		}
		catch (Exception ex)
		{
			global::GameCore.Console.AddLog(string.Format("Exception while handling encrypted message on channel {0} (server, running a handler). Exception: {1}", encryptedMessage.Channel, ex.Message), Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			global::GameCore.Console.AddLog(ex.StackTrace, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
		}
	}

	private static void ClientReceivePackedMessage(EncryptedChannelManager.EncryptedMessageOutside packed)
	{
		ReferenceHub referenceHub;
		EncryptedChannelManager.EncryptedMessage encryptedMessage;
		EncryptedChannelManager.SecurityLevel securityLevel;
		if (!ReferenceHub.TryGetLocalHub(out referenceHub) || !referenceHub.encryptedChannelManager.TryUnpack(packed, out encryptedMessage, out securityLevel, NetworkServer.active))
		{
			return;
		}
		EncryptedChannelManager.EncryptedMessageClientHandler encryptedMessageClientHandler;
		if (!EncryptedChannelManager.ClientChannelHandlers.TryGetValue(encryptedMessage.Channel, out encryptedMessageClientHandler))
		{
			global::GameCore.Console.AddLog(string.Format("No handler is registered for encrypted channel {0} (client).", encryptedMessage.Channel), Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			return;
		}
		try
		{
			encryptedMessageClientHandler(encryptedMessage, securityLevel);
		}
		catch (Exception ex)
		{
			global::GameCore.Console.AddLog(string.Format("Exception while handling encrypted message on channel {0} (client, running a handler). Exception: {1}", encryptedMessage.Channel, ex.Message), Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			global::GameCore.Console.AddLog(ex.StackTrace, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
		}
	}

	public bool TrySendMessageToServer(string content, EncryptedChannelManager.EncryptedChannel channel)
	{
		if (this._txCounter == 4294967295U)
		{
			this._txCounter = 0U;
		}
		uint num = this._txCounter + 1U;
		this._txCounter = num;
		EncryptedChannelManager.EncryptedMessageOutside encryptedMessageOutside;
		if (!this.TryPack(new EncryptedChannelManager.EncryptedMessage(channel, content, num), out encryptedMessageOutside, NetworkServer.active))
		{
			return false;
		}
		NetworkClient.Send<EncryptedChannelManager.EncryptedMessageOutside>(encryptedMessageOutside, 0);
		return true;
	}

	public bool TrySendMessageToServer(byte[] content, EncryptedChannelManager.EncryptedChannel channel)
	{
		if (this._txCounter == 4294967295U)
		{
			this._txCounter = 0U;
		}
		uint num = this._txCounter + 1U;
		this._txCounter = num;
		EncryptedChannelManager.EncryptedMessageOutside encryptedMessageOutside;
		if (!this.TryPack(new EncryptedChannelManager.EncryptedMessage(channel, content, num), out encryptedMessageOutside, NetworkServer.active))
		{
			return false;
		}
		NetworkClient.Send<EncryptedChannelManager.EncryptedMessageOutside>(encryptedMessageOutside, 0);
		return true;
	}

	public static bool TrySendMessageToClient(ReferenceHub hub, string content, EncryptedChannelManager.EncryptedChannel channel)
	{
		return hub.encryptedChannelManager.TrySendMessageToClient(content, channel);
	}

	public static bool TrySendMessageToClient(ReferenceHub hub, byte[] content, EncryptedChannelManager.EncryptedChannel channel)
	{
		return hub.encryptedChannelManager.TrySendMessageToClient(content, channel);
	}

	[Server]
	public bool TrySendMessageToClient(string content, EncryptedChannelManager.EncryptedChannel channel)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean EncryptedChannelManager::TrySendMessageToClient(System.String,EncryptedChannelManager/EncryptedChannel)' called when server was not active");
			return default(bool);
		}
		if (this._txCounter == 4294967295U)
		{
			this._txCounter = 0U;
		}
		uint num = this._txCounter + 1U;
		this._txCounter = num;
		EncryptedChannelManager.EncryptedMessageOutside encryptedMessageOutside;
		if (!this.TryPack(new EncryptedChannelManager.EncryptedMessage(channel, content, num), out encryptedMessageOutside, base.isLocalPlayer))
		{
			return false;
		}
		base.connectionToClient.Send<EncryptedChannelManager.EncryptedMessageOutside>(encryptedMessageOutside, 0);
		return true;
	}

	[Server]
	public bool TrySendMessageToClient(byte[] content, EncryptedChannelManager.EncryptedChannel channel)
	{
		if (!NetworkServer.active)
		{
			Debug.LogWarning("[Server] function 'System.Boolean EncryptedChannelManager::TrySendMessageToClient(System.Byte[],EncryptedChannelManager/EncryptedChannel)' called when server was not active");
			return default(bool);
		}
		if (this._txCounter == 4294967295U)
		{
			this._txCounter = 0U;
		}
		uint num = this._txCounter + 1U;
		this._txCounter = num;
		EncryptedChannelManager.EncryptedMessageOutside encryptedMessageOutside;
		if (!this.TryPack(new EncryptedChannelManager.EncryptedMessage(channel, content, num), out encryptedMessageOutside, base.isLocalPlayer))
		{
			return false;
		}
		base.connectionToClient.Send<EncryptedChannelManager.EncryptedMessageOutside>(encryptedMessageOutside, 0);
		return true;
	}

	private bool TryPack(EncryptedChannelManager.EncryptedMessage msg, out EncryptedChannelManager.EncryptedMessageOutside packed, bool localClient = false)
	{
		bool flag = this.EncryptionKey != null && !localClient;
		if (!localClient && EncryptedChannelManager.RequiredSecurityLevels[msg.Channel] == EncryptedChannelManager.SecurityLevel.EncryptedAndAuthenticated && !flag)
		{
			global::GameCore.Console.AddLog(string.Format("Failed to send encrypted message to {0} channel. Encryption key is not available.", msg.Channel), Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			packed = default(EncryptedChannelManager.EncryptedMessageOutside);
			return false;
		}
		byte[] array = new byte[msg.GetLength];
		msg.Serialize(array);
		if (!flag)
		{
			packed = new EncryptedChannelManager.EncryptedMessageOutside(EncryptedChannelManager.SecurityLevel.Unsecured, array);
			return true;
		}
		packed = new EncryptedChannelManager.EncryptedMessageOutside(EncryptedChannelManager.SecurityLevel.EncryptedAndAuthenticated, AES.AesGcmEncrypt(array, this.EncryptionKey, EncryptedChannelManager.SecureRandom, 0, 0));
		return true;
	}

	private bool TryUnpack(EncryptedChannelManager.EncryptedMessageOutside packed, out EncryptedChannelManager.EncryptedMessage msg, out EncryptedChannelManager.SecurityLevel level, bool localClient)
	{
		level = packed.Level;
		bool flag = packed.Level == EncryptedChannelManager.SecurityLevel.EncryptedAndAuthenticated;
		if (flag && this.EncryptionKey == null)
		{
			global::GameCore.Console.AddLog("Failed to decrypt received message. Encryption key is not available.", Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			msg = default(EncryptedChannelManager.EncryptedMessage);
			return false;
		}
		if (flag)
		{
			try
			{
				msg = EncryptedChannelManager.EncryptedMessage.Deserialize(AES.AesGcmDecrypt(packed.Data, this.EncryptionKey, 0, 0));
			}
			catch (Exception ex)
			{
				global::GameCore.Console.AddLog("Failed to decrypt received message. Exception: " + ex.Message, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
				global::GameCore.Console.AddLog(ex.StackTrace, Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
				msg = default(EncryptedChannelManager.EncryptedMessage);
				return false;
			}
			if (this._rxCounter == 4294967295U)
			{
				this._rxCounter = 0U;
			}
			if (msg.Counter <= this._rxCounter)
			{
				global::GameCore.Console.AddLog(string.Format("Received message with counter {0}, which is lower or equal to last received message counter {1}. Discarding message!", msg.Counter, this._rxCounter), Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
				msg = default(EncryptedChannelManager.EncryptedMessage);
				return false;
			}
			this._rxCounter = msg.Counter;
			return true;
		}
		msg = EncryptedChannelManager.EncryptedMessage.Deserialize(packed.Data);
		if (!localClient && EncryptedChannelManager.RequiredSecurityLevels[msg.Channel] == EncryptedChannelManager.SecurityLevel.EncryptedAndAuthenticated)
		{
			global::GameCore.Console.AddLog(string.Format("Message on channel {0} was sent without encryption. Discarding message!", msg.Channel), Color.red, false, global::GameCore.Console.ConsoleLogType.Log);
			msg = default(EncryptedChannelManager.EncryptedMessage);
			return false;
		}
		return true;
	}

	public override bool Weaved()
	{
		return true;
	}

	public string NetworkServerRandom
	{
		get
		{
			return this.ServerRandom;
		}
		[param: In]
		set
		{
			base.GeneratedSyncVarSetter<string>(value, ref this.ServerRandom, 1UL, new Action<string, string>(this.ReceivedSaltHook));
		}
	}

	public override void SerializeSyncVars(NetworkWriter writer, bool forceAll)
	{
		base.SerializeSyncVars(writer, forceAll);
		if (forceAll)
		{
			writer.WriteString(this.ServerRandom);
			return;
		}
		writer.WriteULong(base.syncVarDirtyBits);
		if ((base.syncVarDirtyBits & 1UL) != 0UL)
		{
			writer.WriteString(this.ServerRandom);
		}
	}

	public override void DeserializeSyncVars(NetworkReader reader, bool initialState)
	{
		base.DeserializeSyncVars(reader, initialState);
		if (initialState)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this.ServerRandom, new Action<string, string>(this.ReceivedSaltHook), reader.ReadString());
			return;
		}
		long num = (long)reader.ReadULong();
		if ((num & 1L) != 0L)
		{
			base.GeneratedSyncVarDeserialize<string>(ref this.ServerRandom, new Action<string, string>(this.ReceivedSaltHook), reader.ReadString());
		}
	}

	internal byte[] EncryptionKey;

	internal static bool CryptographyDebug;

	[SyncVar(hook = "ReceivedSaltHook")]
	[SerializeField]
	[HideInInspector]
	internal string ServerRandom;

	private uint _txCounter;

	private uint _rxCounter;

	private ECDHBasicAgreement _exchange;

	private static readonly SecureRandom SecureRandom = new SecureRandom();

	private static readonly Dictionary<EncryptedChannelManager.EncryptedChannel, EncryptedChannelManager.EncryptedMessageClientHandler> ClientChannelHandlers = new Dictionary<EncryptedChannelManager.EncryptedChannel, EncryptedChannelManager.EncryptedMessageClientHandler>();

	private static readonly Dictionary<EncryptedChannelManager.EncryptedChannel, EncryptedChannelManager.EncryptedMessageServerHandler> ServerChannelHandlers = new Dictionary<EncryptedChannelManager.EncryptedChannel, EncryptedChannelManager.EncryptedMessageServerHandler>();

	private static readonly Dictionary<EncryptedChannelManager.EncryptedChannel, EncryptedChannelManager.SecurityLevel> RequiredSecurityLevels = new Dictionary<EncryptedChannelManager.EncryptedChannel, EncryptedChannelManager.SecurityLevel>
	{
		{
			EncryptedChannelManager.EncryptedChannel.RemoteAdmin,
			EncryptedChannelManager.SecurityLevel.EncryptedAndAuthenticated
		},
		{
			EncryptedChannelManager.EncryptedChannel.GameConsole,
			EncryptedChannelManager.SecurityLevel.Unsecured
		},
		{
			EncryptedChannelManager.EncryptedChannel.AdminChat,
			EncryptedChannelManager.SecurityLevel.EncryptedAndAuthenticated
		}
	};

	public delegate void EncryptedMessageClientHandler(EncryptedChannelManager.EncryptedMessage message, EncryptedChannelManager.SecurityLevel level);

	public delegate void EncryptedMessageServerHandler(ReferenceHub hub, EncryptedChannelManager.EncryptedMessage message, EncryptedChannelManager.SecurityLevel level);

	public enum EncryptedChannel : byte
	{
		RemoteAdmin,
		GameConsole,
		AdminChat
	}

	public enum SecurityLevel : byte
	{
		Unsecured,
		EncryptedAndAuthenticated
	}

	public readonly struct EncryptedMessage
	{
		public EncryptedMessage(EncryptedChannelManager.EncryptedChannel channel, string content, uint counter)
		{
			this.Channel = channel;
			this.Content = Utf8.GetBytes(content);
			this.Counter = counter;
		}

		public EncryptedMessage(EncryptedChannelManager.EncryptedChannel channel, byte[] content, uint counter)
		{
			this.Channel = channel;
			this.Content = content;
			this.Counter = counter;
		}

		public int GetLength
		{
			get
			{
				return this.Content.Length + 5;
			}
		}

		public override string ToString()
		{
			return Utf8.GetString(this.Content);
		}

		internal void Serialize(byte[] array)
		{
			array[0] = (byte)this.Channel;
			BinaryPrimitives.WriteUInt32BigEndian(new Span<byte>(array, 1, 4), this.Counter);
			Array.Copy(this.Content, 0, array, 5, this.Content.Length);
		}

		internal static EncryptedChannelManager.EncryptedMessage Deserialize(byte[] array)
		{
			byte[] array2 = new byte[array.Length - 5];
			Array.Copy(array, 5, array2, 0, array.Length - 5);
			return new EncryptedChannelManager.EncryptedMessage((EncryptedChannelManager.EncryptedChannel)array[0], array2, BinaryPrimitives.ReadUInt32BigEndian(new ReadOnlySpan<byte>(array, 1, 4)));
		}

		public readonly EncryptedChannelManager.EncryptedChannel Channel;

		public readonly byte[] Content;

		internal readonly uint Counter;

		private const int HeaderSize = 5;
	}

	internal readonly struct EncryptedMessageOutside : NetworkMessage
	{
		internal EncryptedMessageOutside(EncryptedChannelManager.SecurityLevel level, byte[] data)
		{
			this.Level = level;
			this.Data = data;
		}

		internal readonly EncryptedChannelManager.SecurityLevel Level;

		internal readonly byte[] Data;
	}
}
