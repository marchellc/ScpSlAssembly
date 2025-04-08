using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using Cryptography;
using Org.BouncyCastle.Crypto.Modes;

namespace Query
{
	internal class QueryUser : IDisposable
	{
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
			this.SenderID = string.Format("Query ({0})", this._remoteEndpoint);
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
					this.DisconnectInternal(false, false);
				}
				else if (!this.ReceiveInternal())
				{
					this.DisconnectInternal(false, true);
				}
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog(string.Format("An exception occured when processing query client {0}: {1}\n{2}", this._remoteEndpoint, ex.Message, ex.StackTrace), ConsoleColor.Gray, false);
				try
				{
					this.DisconnectInternal(false, true);
				}
				catch (Exception ex2)
				{
					ServerConsole.AddLog(string.Format("An exception occured when disconnecting query client {0}: {1}\n{2}", this._remoteEndpoint, ex2.Message, ex2.StackTrace), ConsoleColor.Gray, false);
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
					ServerConsole.AddLog(string.Format("Query connection from {0} disconnected (can't read length bytes).", this._remoteEndpoint), ConsoleColor.Gray, false);
					return false;
				}
				this._lengthToRead = BinaryPrimitives.ReadUInt16BigEndian(this._server.RxBuffer);
				if ((int)this._lengthToRead > this._server.RxBuffer.Length)
				{
					ServerConsole.AddLog(string.Format("Query connection from {0} disconnected (packet too large, limit set in gameplay config).", this._remoteEndpoint), ConsoleColor.Gray, false);
					this.Send(string.Format("Query input can't exceed {0} bytes (limit set in config).", this._server.RxBuffer.Length), QueryMessage.ClientReceivedContentType.QueryMessage);
					return false;
				}
			}
			if (this._c.Available < (int)this._lengthToRead)
			{
				return true;
			}
			if (this._s.Read(this._server.RxBuffer, 0, (int)this._lengthToRead) != (int)this._lengthToRead)
			{
				ServerConsole.AddLog(string.Format("Query connection from {0} disconnected (can't read length bytes).", this._remoteEndpoint), ConsoleColor.Gray, false);
				return false;
			}
			AES.ReadNonce(this._server.RxNonceBuffer, this._server.RxBuffer, 0);
			int num;
			GcmBlockCipher gcmBlockCipher = AES.AesGcmDecryptInit(this._server.RxNonceBuffer, this._server.PasswordHash, (int)this._lengthToRead, out num);
			if (this._server.RxDecryptionBuffer.Length < num)
			{
				ServerConsole.AddLog(string.Format("Query connection from {0} disconnected (data to decrypt too large, limit set in gameplay config).", this._remoteEndpoint), ConsoleColor.Gray, false);
				this.Send(string.Format("Query decrypted data size can't exceed {0} bytes (limit set in config).", this._server.RxBuffer.Length), QueryMessage.ClientReceivedContentType.QueryMessage);
				return false;
			}
			try
			{
				AES.AesGcmDecrypt(gcmBlockCipher, this._server.RxBuffer, this._server.RxDecryptionBuffer, 0, (int)this._lengthToRead, 0);
			}
			catch (Exception ex)
			{
				if (this._authenticated)
				{
					ServerConsole.AddLog(string.Format("Query connection from {0} disconnected (can't decrypt data): {1}", this._remoteEndpoint, ex.Message), ConsoleColor.Gray, false);
					ServerLogs.AddLog(ServerLogs.Modules.Networking, string.Format("Query message from {0} can't be decrypted", this._remoteEndpoint), ServerLogs.ServerLogType.Query, false);
				}
				else
				{
					ServerConsole.AddLog(string.Format("Query connection from {0} disconnected (can't decrypt handshake, likely invalid password was used)): {1}", this._remoteEndpoint, ex.Message), ConsoleColor.Gray, false);
					ServerLogs.AddLog(ServerLogs.Modules.Networking, string.Format("Query handshake from {0} can't be decrypted (likely invalid password was used).", this._remoteEndpoint), ServerLogs.ServerLogType.Query, false);
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
				return this.HandleMessage(num);
			}
			return this.HandleHandshake(num);
		}

		private bool HandleMessage(int outputSize)
		{
			QueryUser.<>c__DisplayClass22_0 CS$<>8__locals1 = new QueryUser.<>c__DisplayClass22_0();
			CS$<>8__locals1.<>4__this = this;
			CS$<>8__locals1.qm = QueryMessage.Deserialize(new ReadOnlySpan<byte>(this._server.RxDecryptionBuffer, 0, outputSize));
			QueryUser.<>c__DisplayClass22_0 CS$<>8__locals2 = CS$<>8__locals1;
			uint rxCounter = this._rxCounter;
			this._rxCounter = rxCounter + 1U;
			if (!CS$<>8__locals2.qm.Validate(rxCounter, QueryServer.MaximumTimeDifference))
			{
				ServerConsole.AddLog(string.Format("Query message from {0} failed validation - invalid time, timezone (on server or client) or a reply attack.", this._remoteEndpoint), ConsoleColor.Gray, false);
				ServerLogs.AddLog(ServerLogs.Modules.Networking, string.Format("Query message from {0} failed validation (invalid timestamp).", this._remoteEndpoint), ServerLogs.ServerLogType.Query, false);
				this.Send("Message failed validation!", QueryMessage.ClientReceivedContentType.QueryMessage);
				return false;
			}
			try
			{
				if (CS$<>8__locals1.qm.QueryContentType == 0)
				{
					MainThreadDispatcher.Dispatch(delegate
					{
						ServerConsole.EnterCommand(CS$<>8__locals1.qm.ToString(), CS$<>8__locals1.<>4__this._printer);
					}, MainThreadDispatcher.DispatchTime.FixedUpdate);
				}
				else
				{
					this.Send("Unknown query message content type!", QueryMessage.ClientReceivedContentType.CommandException);
				}
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog(string.Format("Error during processing query command from {0}: {1}\n{2}", this._remoteEndpoint, ex.Message, ex.StackTrace), ConsoleColor.Gray, false);
				this.Send("Error during processing your command. Check server console for more info.", QueryMessage.ClientReceivedContentType.CommandException);
			}
			return true;
		}

		private bool HandleHandshake(int outputSize)
		{
			QueryHandshake queryHandshake = QueryHandshake.Deserialize(new ReadOnlySpan<byte>(this._server.RxDecryptionBuffer, 0, outputSize), false);
			if (!queryHandshake.Validate(QueryServer.MaximumTimeDifference))
			{
				ServerConsole.AddLog(string.Format("Query handshake from {0} failed validation - invalid time or timezone (on server or client).", this._remoteEndpoint), ConsoleColor.Gray, false);
				ServerLogs.AddLog(ServerLogs.Modules.Networking, string.Format("Query client {0} failed authentication (invalid timestamp).", this._remoteEndpoint), ServerLogs.ServerLogType.Query, false);
				this.Send("Message failed validation - invalid timestamp!", QueryMessage.ClientReceivedContentType.QueryMessage);
				return false;
			}
			if (!queryHandshake.AuthChallenge.SequenceEqual(this._authChallenge))
			{
				ServerConsole.AddLog(string.Format("Query handshake from {0} failed validation - invalid auth challenge.", this._remoteEndpoint), ConsoleColor.Gray, false);
				ServerLogs.AddLog(ServerLogs.Modules.Networking, string.Format("Query client {0} failed authentication (invalid auth challenge).", this._remoteEndpoint), ServerLogs.ServerLogType.Query, false);
				this.Send("Message failed validation - invalid auth challenge!", QueryMessage.ClientReceivedContentType.QueryMessage);
				return false;
			}
			ServerConsole.AddLog(string.Format("Query client {0} has successfully authenticated.", this._remoteEndpoint), ConsoleColor.Gray, false);
			ServerLogs.AddLog(ServerLogs.Modules.Networking, string.Format("Query client {0} has successfully authenticated.", this._remoteEndpoint), ServerLogs.ServerLogType.Query, false);
			this._authenticated = true;
			this._authChallenge = null;
			this._clientMaxSize = queryHandshake.MaxPacketSize;
			this.QueryPermissions = QueryServer.QueryPermissions & queryHandshake.Permissions;
			this.QueryKickPower = ((QueryServer.QueryKickPower < queryHandshake.KickPower) ? QueryServer.QueryKickPower : queryHandshake.KickPower);
			if (queryHandshake.Username != null)
			{
				this.SenderID = string.Format("{0} ({1})", queryHandshake.Username, this._remoteEndpoint);
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
			int num = new QueryHandshake(this._server.BufferLength, this._authChallenge, QueryHandshake.ClientFlags.None, ulong.MaxValue, byte.MaxValue, null, this._timeoutThreshold).Serialize(new Span<byte>(this._server.RxBuffer, 2, (int)(this._server.BufferLength - 2)), false);
			BinaryPrimitives.WriteUInt16BigEndian(this._server.RxBuffer, (ushort)num);
			if (this.SendRaw(this._server.RxBuffer, 0, num + 2, false))
			{
				return true;
			}
			this.DisconnectInternal(false, true);
			return false;
		}

		internal void Send(string msg, QueryMessage.ClientReceivedContentType contentType)
		{
			if (string.IsNullOrWhiteSpace(msg))
			{
				return;
			}
			if (!this._authenticated)
			{
				this.SendRaw(msg, true);
				return;
			}
			uint num = this._txCounter + 1U;
			this._txCounter = num;
			this.Send(new QueryMessage(msg, num, (byte)contentType));
		}

		internal void Send(byte[] msg, QueryMessage.ClientReceivedContentType contentType)
		{
			if (msg.Length == 0)
			{
				return;
			}
			if (!this._authenticated)
			{
				this.SendRaw(msg, true);
				return;
			}
			uint num = this._txCounter + 1U;
			this._txCounter = num;
			this.Send(new QueryMessage(msg, num, (byte)contentType));
		}

		private void Send(QueryMessage qm)
		{
			byte[] array = ArrayPool<byte>.Shared.Rent(qm.SerializedSize);
			byte[] array2 = null;
			byte[] array3 = ArrayPool<byte>.Shared.Rent(32);
			try
			{
				int num = qm.Serialize(array);
				AES.GenerateNonce(array3, this._server.Random);
				int num2;
				GcmBlockCipher gcmBlockCipher = AES.AesGcmEncryptInit(num, this._server.PasswordHash, array3, out num2);
				int num3 = num2 + 2;
				if (num3 > (int)this._clientMaxSize)
				{
					ServerConsole.AddLog(string.Format("Query message to {0} exceeds client's max size ({1} > {2}).", this._remoteEndpoint, num3, this._clientMaxSize), ConsoleColor.Yellow, false);
					this._txCounter -= 1U;
				}
				else
				{
					array2 = ArrayPool<byte>.Shared.Rent(num3);
					BinaryPrimitives.WriteUInt16BigEndian(array2, (ushort)num2);
					AES.AesGcmEncrypt(gcmBlockCipher, array3, array, 0, num, array2, 2);
					if (!this.SendRaw(array2, 0, num3, false))
					{
						this._txCounter -= 1U;
					}
				}
			}
			catch (Exception ex)
			{
				this._txCounter -= 1U;
				ServerConsole.AddLog(string.Format("Can't send query response (string) to {0}: {1}\n", this._remoteEndpoint, ex.Message) + ex.StackTrace, ConsoleColor.Yellow, false);
			}
			finally
			{
				if (array2 != null)
				{
					ArrayPool<byte>.Shared.Return(array2, false);
				}
				ArrayPool<byte>.Shared.Return(array, false);
				ArrayPool<byte>.Shared.Return(array3, false);
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
			bool flag;
			try
			{
				if (addLength)
				{
					byte[] array = ArrayPool<byte>.Shared.Rent(len - offset + 2);
					try
					{
						BinaryPrimitives.WriteUInt16BigEndian(array, (ushort)(len - offset));
						Array.Copy(msg, offset, array, 2, len);
						object obj = this._writeLock;
						lock (obj)
						{
							this._s.Write(msg, offset, len);
						}
					}
					finally
					{
						ArrayPool<byte>.Shared.Return(array, false);
					}
					flag = true;
				}
				else
				{
					object obj = this._writeLock;
					lock (obj)
					{
						this._s.Write(msg, offset, len);
					}
					flag = true;
				}
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog(string.Format("Can't send query response (byte[]) to {0}: {1}\n", this._remoteEndpoint, ex.Message) + ex.StackTrace, ConsoleColor.Gray, false);
				flag = false;
			}
			return flag;
		}

		internal bool Connected
		{
			get
			{
				return this._c.Connected && this._s.CanRead && this._s.CanWrite && this._lastRx.ElapsedMilliseconds < (long)((ulong)this._timeoutThreshold);
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
					this._c.Client.LingerState = new LingerOption(true, 0);
				}
				if (serverShutdown)
				{
					this.Send("Server is shutting down...", QueryMessage.ClientReceivedContentType.QueryMessage);
				}
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog(string.Format("Error closing query connection from {0}: {1}\n{2}", this._remoteEndpoint, ex.Message, ex.StackTrace), ConsoleColor.Gray, false);
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
				NetworkStream s = this._s;
				if (s != null)
				{
					s.Dispose();
				}
			}
			catch (Exception ex)
			{
				ServerConsole.AddLog(string.Format("Error disposing query socket connection from {0}: {1}\n{2}", this._remoteEndpoint, ex.Message, ex.StackTrace), ConsoleColor.Gray, false);
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
				ServerConsole.AddLog("Error removing query user from list: " + ex2.Message + "\n" + ex2.StackTrace, ConsoleColor.Gray, false);
			}
			try
			{
				IOutput output;
				ServerConsole.ConsoleOutputs.TryRemove(this.SenderID, out output);
			}
			catch (Exception ex3)
			{
				ServerConsole.AddLog("Error removing query user from console outputs: " + ex3.Message + "\n" + ex3.StackTrace, ConsoleColor.Gray, false);
			}
			try
			{
				IOutput output;
				ServerLogs.LiveLogOutput.TryRemove(this.SenderID, out output);
			}
			catch (Exception ex4)
			{
				ServerConsole.AddLog("Error removing query user from live server log outputs: " + ex4.Message + "\n" + ex4.StackTrace, ConsoleColor.Gray, false);
			}
		}

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
	}
}
