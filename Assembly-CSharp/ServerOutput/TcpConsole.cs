using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Threading;
using UnityEngine;

namespace ServerOutput;

public class TcpConsole : IServerOutput, IDisposable
{
	public readonly int SpecifiedReceiveBufferSize;

	public readonly int SpecifiedSendBufferSize;

	public const int DefaultReceiveBufferSize = 25000;

	public const int DefaultSendBufferSize = 200000;

	private bool _disposing;

	private readonly ushort _port;

	private readonly int _maxTextLogSize;

	private readonly TcpClient _client;

	private NetworkStream _stream;

	private readonly Thread _receiveThread;

	private readonly Thread _queueThread;

	private readonly ConcurrentQueue<IOutputEntry> _prompterQueue = new ConcurrentQueue<IOutputEntry>();

	public int ReceiveBufferSize => this._client.ReceiveBufferSize;

	public int SendBufferSize => this._client.SendBufferSize;

	public TcpConsole(ushort port, int receiveBufferSize = 25000, int sendBufferSize = 200000)
	{
		this._client = new TcpClient();
		this._client.NoDelay = true;
		this._receiveThread = new Thread(Receive)
		{
			Priority = System.Threading.ThreadPriority.Lowest,
			IsBackground = true,
			Name = "Dedicated server console input"
		};
		this._queueThread = new Thread(Send)
		{
			Priority = System.Threading.ThreadPriority.Lowest,
			IsBackground = true,
			Name = "Dedicated server console output"
		};
		this._port = port;
		if (receiveBufferSize <= 100)
		{
			receiveBufferSize = 25000;
		}
		if (sendBufferSize <= 350)
		{
			sendBufferSize = 200000;
		}
		this.SpecifiedReceiveBufferSize = receiveBufferSize;
		this.SpecifiedSendBufferSize = sendBufferSize;
		this._client.ReceiveBufferSize = receiveBufferSize;
		this._client.SendBufferSize = sendBufferSize;
		this._maxTextLogSize = sendBufferSize - 5;
	}

	public void Start()
	{
		this._client.Connect(new IPEndPoint(IPAddress.Loopback, this._port));
		this._stream = this._client.GetStream();
		this._queueThread.Start();
		this._receiveThread.Start();
	}

	public void Dispose()
	{
		this._disposing = true;
		try
		{
			if (this._receiveThread.IsAlive)
			{
				this._receiveThread.Abort();
			}
		}
		catch
		{
		}
		try
		{
			if (this._queueThread.IsAlive)
			{
				this._queueThread.Abort();
			}
		}
		catch
		{
		}
		this._stream?.Dispose();
		this._client.Dispose();
	}

	private void Receive()
	{
		byte[] array = new byte[4];
		while (!this._disposing)
		{
			try
			{
				int num = this._stream.Read(array, 0, 4);
				if (num != 4)
				{
					ServerConsole.AddLog($"[TcpConsole] Received header length is NOT {4}! Received data amount: {num}.", ConsoleColor.Red);
					continue;
				}
				int num2 = MemoryMarshal.Cast<byte, int>(array)[0];
				while (this._client.Available < num2)
				{
					Thread.Sleep(20);
				}
				byte[] array2 = ArrayPool<byte>.Shared.Rent(num2);
				num = this._stream.Read(array2, 0, num2);
				if (num != num2)
				{
					ServerConsole.AddLog($"[TcpConsole] Received data length is NOT {num2}! Received data amount: {num}.", ConsoleColor.Red);
					continue;
				}
				string item = Utf8.GetString(array2, 0, num2);
				ArrayPool<byte>.Shared.Return(array2);
				ServerConsole.PrompterQueue.Enqueue(item);
			}
			catch (Exception ex)
			{
				this.AddLog("[TcpClient] Receive exception: " + ex.Message);
				this.AddLog("[TcpClient] " + ex.StackTrace);
				Debug.LogException(ex);
			}
		}
	}

	public void AddLog(string text, ConsoleColor color)
	{
		if (!string.IsNullOrWhiteSpace(text))
		{
			while (text.Length > this._maxTextLogSize)
			{
				this._prompterQueue.Enqueue(new TextOutputEntry(text.Substring(0, this._maxTextLogSize), color));
				text = text.Substring(this._maxTextLogSize);
			}
			this._prompterQueue.Enqueue(new TextOutputEntry(text, color));
		}
	}

	public void AddLog(string text)
	{
		this.AddLog(text, ConsoleColor.Gray);
	}

	public void AddOutput(IOutputEntry entry)
	{
		if (entry.GetBytesLength() > this.SpecifiedSendBufferSize)
		{
			throw new Exception("Output size is greater than TCP Console Specified TX Buffer Size!");
		}
		this._prompterQueue.Enqueue(entry);
	}

	private void Send()
	{
		while (!this._disposing)
		{
			if (this._prompterQueue.Count == 0)
			{
				Thread.Sleep(25);
				continue;
			}
			try
			{
				if (this._prompterQueue.TryDequeue(out var result))
				{
					byte[] buffer = ArrayPool<byte>.Shared.Rent(result.GetBytesLength());
					result.GetBytes(ref buffer, out var length);
					this._stream.Write(buffer, 0, length);
					ArrayPool<byte>.Shared.Return(buffer);
				}
			}
			catch (Exception ex)
			{
				this.AddLog("[TcpClient] Send exception: " + ex.Message);
				this.AddLog("[TcpClient] " + ex.StackTrace);
				Debug.LogException(ex);
			}
		}
	}
}
