using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Cryptography;
using Org.BouncyCastle.Crypto.Modes;

namespace Query;

internal class QueryUser : IDisposable
{
	private readonly IPEndPoint _remoteEndpoint;

	private readonly TcpClient _c;

	private readonly NetworkStream _s;

	private readonly QueryServer _server;

	private readonly Stopwatch _lastRx;

	private readonly QueryCommandSender _printer;

	private readonly ushort _timeoutThreshold;

	private readonly object _writeLock = new object();

	private bool _authenticated;

	private bool _disconnect;

	private ushort _lengthToRead;

	private ushort _clientMaxSize;

	private uint _rxCounter;

	private uint _txCounter;

	private byte[] _authChallenge = new byte[24];

	internal QueryHandshake.ClientFlags ClientFlags;

	internal ulong QueryPermissions;

	internal byte QueryKickPower;

	internal string SenderID;

	internal bool Connected
	{
		get
		{
			if (this._c.Connected && this._s.CanRead && this._s.CanWrite)
			{
				return this._lastRx.ElapsedMilliseconds < this._timeoutThreshold;
			}
			return false;
		}
	}

	internal QueryUser(QueryServer s, TcpClient c)
	{
		this._c = c;
		this._server = s;
		this._s = c.GetStream();
		c.NoDelay = true;
		this._s.ReadTimeout = 150;
		this._s.WriteTimeout = 150;
		this._remoteEndpoint = (IPEndPoint)c.Client.RemoteEndPoint;
		this._timeoutThreshold = QueryServer.TimeoutThreshold;
		this.SenderID = $"Query ({this._remoteEndpoint})";
		this._printer = new QueryCommandSender(this);
		this._server.Random.NextBytes(this._authChallenge);
		this._lastRx = new Stopwatch();
		this._lastRx.Start();
	}

	internal void Receive()
	{
		try
		{
			if (this._disconnect)
			{
				this.Send("Closing connection...", QueryMessage.ClientReceivedContentType.QueryMessage);
				this.DisconnectInternal();
			}
			else if (!this.ReceiveInternal())
			{
				this.DisconnectInternal(serverShutdown: false, force: true);
			}
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog($"An exception occured when processing query client {this._remoteEndpoint}: {ex.Message}\n{ex.StackTrace}");
			try
			{
				this.DisconnectInternal(serverShutdown: false, force: true);
			}
			catch (Exception ex2)
			{
				ServerConsole.AddLog($"An exception occured when disconnecting query client {this._remoteEndpoint}: {ex2.Message}\n{ex2.StackTrace}");
			}
		}
	}

	private bool ReceiveInternal()
	{
		if (!this.Connected)
		{
			return false;
		}
		if (this._lengthToRead == 0)
		{
			if (this._c.Available < 2)
			{
				return true;
			}
			if (this._s.Read(this._server.RxBuffer, 0, 2) != 2)
			{
				ServerConsole.AddLog($"Query connection from {this._remoteEndpoint} disconnected (can't read length bytes).");
				return false;
			}
			this._lengthToRead = BinaryPrimitives.ReadUInt16BigEndian(this._server.RxBuffer);
			if (this._lengthToRead > this._server.RxBuffer.Length)
			{
				ServerConsole.AddLog($"Query connection from {this._remoteEndpoint} disconnected (packet too large, limit set in gameplay config).");
				this.Send($"Query input can't exceed {this._server.RxBuffer.Length} bytes (limit set in config).", QueryMessage.ClientReceivedContentType.QueryMessage);
				return false;
			}
		}
		if (this._c.Available < this._lengthToRead)
		{
			return true;
		}
		if (this._s.Read(this._server.RxBuffer, 0, this._lengthToRead) != this._lengthToRead)
		{
			ServerConsole.AddLog($"Query connection from {this._remoteEndpoint} disconnected (can't read length bytes).");
			return false;
		}
		AES.ReadNonce(this._server.RxNonceBuffer, this._server.RxBuffer);
		int outputSize;
		GcmBlockCipher cipher = AES.AesGcmDecryptInit(this._server.RxNonceBuffer, this._server.PasswordHash, this._lengthToRead, out outputSize);
		if (this._server.RxDecryptionBuffer.Length < outputSize)
		{
			ServerConsole.AddLog($"Query connection from {this._remoteEndpoint} disconnected (data to decrypt too large, limit set in gameplay config).");
			this.Send($"Query decrypted data size can't exceed {this._server.RxBuffer.Length} bytes (limit set in config).", QueryMessage.ClientReceivedContentType.QueryMessage);
			return false;
		}
		try
		{
			AES.AesGcmDecrypt(cipher, this._server.RxBuffer, this._server.RxDecryptionBuffer, 0, this._lengthToRead);
		}
		catch (Exception ex)
		{
			if (this._authenticated)
			{
				ServerConsole.AddLog($"Query connection from {this._remoteEndpoint} disconnected (can't decrypt data): {ex.Message}");
				ServerLogs.AddLog(ServerLogs.Modules.Networking, $"Query message from {this._remoteEndpoint} can't be decrypted", ServerLogs.ServerLogType.Query);
			}
			else
			{
				ServerConsole.AddLog($"Query connection from {this._remoteEndpoint} disconnected (can't decrypt handshake, likely invalid password was used)): {ex.Message}");
				ServerLogs.AddLog(ServerLogs.Modules.Networking, $"Query handshake from {this._remoteEndpoint} can't be decrypted (likely invalid password was used).", ServerLogs.ServerLogType.Query);
			}
			this.Send("Decryption failed!", QueryMessage.ClientReceivedContentType.QueryMessage);
			return false;
		}
		finally
		{
			this._lengthToRead = 0;
			this._lastRx.Restart();
		}
		if (this._authenticated)
		{
			return this.HandleMessage(outputSize);
		}
		return this.HandleHandshake(outputSize);
	}

	private bool HandleMessage(int outputSize)
	{
		QueryMessage qm = QueryMessage.Deserialize(new ReadOnlySpan<byte>(this._server.RxDecryptionBuffer, 0, outputSize));
		if (!qm.Validate(this._rxCounter++, QueryServer.MaximumTimeDifference))
		{
			ServerConsole.AddLog($"Query message from {this._remoteEndpoint} failed validation - invalid time, timezone (on server or client) or a reply attack.");
			ServerLogs.AddLog(ServerLogs.Modules.Networking, $"Query message from {this._remoteEndpoint} failed validation (invalid timestamp).", ServerLogs.ServerLogType.Query);
			this.Send("Message failed validation!", QueryMessage.ClientReceivedContentType.QueryMessage);
			return false;
		}
		try
		{
			if (qm.QueryContentType == 0)
			{
				MainThreadDispatcher.Dispatch(delegate
				{
					ServerConsole.EnterCommand(qm.ToString(), this._printer);
				}, MainThreadDispatcher.DispatchTime.FixedUpdate);
			}
			else
			{
				this.Send("Unknown query message content type!", QueryMessage.ClientReceivedContentType.CommandException);
			}
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog($"Error during processing query command from {this._remoteEndpoint}: {ex.Message}\n{ex.StackTrace}");
			this.Send("Error during processing your command. Check server console for more info.", QueryMessage.ClientReceivedContentType.CommandException);
		}
		return true;
	}

	private bool HandleHandshake(int outputSize)
	{
		QueryHandshake queryHandshake = QueryHandshake.Deserialize(new ReadOnlySpan<byte>(this._server.RxDecryptionBuffer, 0, outputSize), toServer: false);
		if (!queryHandshake.Validate(QueryServer.MaximumTimeDifference))
		{
			ServerConsole.AddLog($"Query handshake from {this._remoteEndpoint} failed validation - invalid time or timezone (on server or client).");
			ServerLogs.AddLog(ServerLogs.Modules.Networking, $"Query client {this._remoteEndpoint} failed authentication (invalid timestamp).", ServerLogs.ServerLogType.Query);
			this.Send("Message failed validation - invalid timestamp!", QueryMessage.ClientReceivedContentType.QueryMessage);
			return false;
		}
		if (!queryHandshake.AuthChallenge.SequenceEqual(this._authChallenge))
		{
			ServerConsole.AddLog($"Query handshake from {this._remoteEndpoint} failed validation - invalid auth challenge.");
			ServerLogs.AddLog(ServerLogs.Modules.Networking, $"Query client {this._remoteEndpoint} failed authentication (invalid auth challenge).", ServerLogs.ServerLogType.Query);
			this.Send("Message failed validation - invalid auth challenge!", QueryMessage.ClientReceivedContentType.QueryMessage);
			return false;
		}
		ServerConsole.AddLog($"Query client {this._remoteEndpoint} has successfully authenticated.");
		ServerLogs.AddLog(ServerLogs.Modules.Networking, $"Query client {this._remoteEndpoint} has successfully authenticated.", ServerLogs.ServerLogType.Query);
		this._authenticated = true;
		this._authChallenge = null;
		this._clientMaxSize = queryHandshake.MaxPacketSize;
		this.QueryPermissions = QueryServer.QueryPermissions & queryHandshake.Permissions;
		this.QueryKickPower = ((QueryServer.QueryKickPower < queryHandshake.KickPower) ? QueryServer.QueryKickPower : queryHandshake.KickPower);
		if (queryHandshake.Username != null)
		{
			this.SenderID = $"{queryHandshake.Username} ({this._remoteEndpoint})";
		}
		this.ClientFlags = queryHandshake.Flags;
		this.Send("Authentication successful!", QueryMessage.ClientReceivedContentType.QueryMessage);
		if (this.ClientFlags.HasFlagFast(QueryHandshake.ClientFlags.SubscribeServerConsole))
		{
			if (PermissionsHandler.IsPermitted(this.QueryPermissions, PlayerPermissions.ServerLogLiveFeed) && PermissionsHandler.IsPermitted(this.QueryPermissions, PlayerPermissions.ServerConsoleCommands))
			{
				ServerConsole.ConsoleOutputs.TryAdd(this.SenderID, this._printer);
				this.Send("You have been subscribed to server console output.", QueryMessage.ClientReceivedContentType.QueryMessage);
			}
			else
			{
				this.Send("You don't have permissions to subscribe to server console output!", QueryMessage.ClientReceivedContentType.QueryMessage);
			}
		}
		if (this.ClientFlags.HasFlagFast(QueryHandshake.ClientFlags.SubscribeServerLogs))
		{
			if (PermissionsHandler.IsPermitted(this.QueryPermissions, PlayerPermissions.ServerLogLiveFeed))
			{
				ServerLogs.LiveLogOutput.TryAdd(this.SenderID, this._printer);
				this.Send("You have been subscribed to live server logs.", QueryMessage.ClientReceivedContentType.QueryMessage);
			}
			else
			{
				this.Send("You don't have permissions to subscribe to live server logs!", QueryMessage.ClientReceivedContentType.QueryMessage);
			}
		}
		return true;
	}

	internal bool SendHandshake()
	{
		int num = new QueryHandshake(this._server.BufferLength, this._authChallenge, QueryHandshake.ClientFlags.None, ulong.MaxValue, byte.MaxValue, null, this._timeoutThreshold).Serialize(new Span<byte>(this._server.RxBuffer, 2, this._server.BufferLength - 2), toServer: false);
		BinaryPrimitives.WriteUInt16BigEndian(this._server.RxBuffer, (ushort)num);
		if (this.SendRaw(this._server.RxBuffer, 0, num + 2, addLength: false))
		{
			return true;
		}
		this.DisconnectInternal(serverShutdown: false, force: true);
		return false;
	}

	internal void Send(string msg, QueryMessage.ClientReceivedContentType contentType)
	{
		if (!string.IsNullOrWhiteSpace(msg))
		{
			if (!this._authenticated)
			{
				this.SendRaw(msg);
			}
			else
			{
				this.Send(new QueryMessage(msg, ++this._txCounter, (byte)contentType));
			}
		}
	}

	internal void Send(byte[] msg, QueryMessage.ClientReceivedContentType contentType)
	{
		if (msg.Length != 0)
		{
			if (!this._authenticated)
			{
				this.SendRaw(msg, addLength: true);
			}
			else
			{
				this.Send(new QueryMessage(msg, ++this._txCounter, (byte)contentType));
			}
		}
	}

	private void Send(QueryMessage qm)
	{
		byte[] array = ArrayPool<byte>.Shared.Rent(qm.SerializedSize);
		byte[] array2 = null;
		byte[] array3 = ArrayPool<byte>.Shared.Rent(32);
		try
		{
			int dataLength = qm.Serialize(array);
			AES.GenerateNonce(array3, this._server.Random);
			int outputSize;
			GcmBlockCipher cipher = AES.AesGcmEncryptInit(dataLength, this._server.PasswordHash, array3, out outputSize);
			int num = outputSize + 2;
			if (num > this._clientMaxSize)
			{
				ServerConsole.AddLog($"Query message to {this._remoteEndpoint} exceeds client's max size ({num} > {this._clientMaxSize}).", ConsoleColor.Yellow);
				this._txCounter--;
				return;
			}
			array2 = ArrayPool<byte>.Shared.Rent(num);
			BinaryPrimitives.WriteUInt16BigEndian(array2, (ushort)outputSize);
			AES.AesGcmEncrypt(cipher, array3, array, 0, dataLength, array2, 2);
			if (!this.SendRaw(array2, 0, num, addLength: false))
			{
				this._txCounter--;
			}
		}
		catch (Exception ex)
		{
			this._txCounter--;
			ServerConsole.AddLog($"Can't send query response (string) to {this._remoteEndpoint}: {ex.Message}\n" + ex.StackTrace, ConsoleColor.Yellow);
		}
		finally
		{
			if (array2 != null)
			{
				ArrayPool<byte>.Shared.Return(array2);
			}
			ArrayPool<byte>.Shared.Return(array);
			ArrayPool<byte>.Shared.Return(array3);
		}
	}

	private bool SendRaw(string msg, bool addLength = true)
	{
		return this.SendRaw(Utf8.GetBytes(msg), addLength);
	}

	private bool SendRaw(byte[] msg, bool addLength)
	{
		return this.SendRaw(msg, 0, msg.Length, addLength);
	}

	private bool SendRaw(byte[] msg, int offset, int len, bool addLength)
	{
		try
		{
			if (addLength)
			{
				byte[] array = ArrayPool<byte>.Shared.Rent(len - offset + 2);
				try
				{
					BinaryPrimitives.WriteUInt16BigEndian(array, (ushort)(len - offset));
					Array.Copy(msg, offset, array, 2, len);
					lock (this._writeLock)
					{
						this._s.Write(msg, offset, len);
					}
				}
				finally
				{
					ArrayPool<byte>.Shared.Return(array);
				}
				return true;
			}
			lock (this._writeLock)
			{
				this._s.Write(msg, offset, len);
			}
			return true;
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog($"Can't send query response (byte[]) to {this._remoteEndpoint}: {ex.Message}\n" + ex.StackTrace);
			return false;
		}
	}

	public void Disconnect()
	{
		this._disconnect = true;
	}

	internal void DisconnectInternal(bool serverShutdown = false, bool force = false)
	{
		try
		{
			if (force)
			{
				this._c.Client.LingerState = new LingerOption(enable: true, 0);
			}
			if (serverShutdown)
			{
				this.Send("Server is shutting down...", QueryMessage.ClientReceivedContentType.QueryMessage);
			}
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog($"Error closing query connection from {this._remoteEndpoint}: {ex.Message}\n{ex.StackTrace}");
		}
		this.Dispose();
	}

	public override string ToString()
	{
		if (!this._authenticated)
		{
			return this.SenderID + " [UNAUTHENTICATED]";
		}
		return this.SenderID;
	}

	public void Dispose()
	{
		try
		{
			this._s?.Dispose();
		}
		catch (Exception ex)
		{
			ServerConsole.AddLog($"Error disposing query socket connection from {this._remoteEndpoint}: {ex.Message}\n{ex.StackTrace}");
		}
		try
		{
			if (this._server.Users.Contains(this))
			{
				this._server.Users.Remove(this);
			}
		}
		catch (Exception ex2)
		{
			ServerConsole.AddLog("Error removing query user from list: " + ex2.Message + "\n" + ex2.StackTrace);
		}
		IOutput value;
		try
		{
			ServerConsole.ConsoleOutputs.TryRemove(this.SenderID, out value);
		}
		catch (Exception ex3)
		{
			ServerConsole.AddLog("Error removing query user from console outputs: " + ex3.Message + "\n" + ex3.StackTrace);
		}
		try
		{
			ServerLogs.LiveLogOutput.TryRemove(this.SenderID, out value);
		}
		catch (Exception ex4)
		{
			ServerConsole.AddLog("Error removing query user from live server log outputs: " + ex4.Message + "\n" + ex4.StackTrace);
		}
	}
}
