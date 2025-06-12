using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoiceChat.Networking;

public class PlaybackBuffer : IDisposable
{
	private static readonly Dictionary<int, Queue<float[]>> PoolsOfSize = new Dictionary<int, Queue<float[]>>();

	private static float[] _organizerArr;

	private static float _organizerSize;

	private readonly int _bufferSize;

	private readonly bool _endless;

	public long ReadHead;

	public long WriteHead;

	public readonly float[] Buffer;

	public int Length => (int)(this.WriteHead - this.ReadHead);

	public PlaybackBuffer(int capacity = 24000, bool endlessTapeMode = false)
	{
		this._bufferSize = capacity;
		this.Buffer = ((PlaybackBuffer.PoolsOfSize.TryGetValue(capacity, out var value) && value.TryDequeue(out var result)) ? result : new float[this._bufferSize]);
		this._endless = endlessTapeMode;
		this.ReadHead = 0L;
		this.WriteHead = 0L;
	}

	public void Write(float[] f, int length, int sourceIndex)
	{
		long num = this.WriteHead + length - this.ReadHead - this._bufferSize;
		if (num > 0)
		{
			if (this._endless)
			{
				this.ReadHead += num;
			}
			else
			{
				this.Clear();
			}
		}
		long num2 = this.HeadToIndex(this.WriteHead);
		long num3 = this._bufferSize - num2 - length;
		if (num3 >= 0)
		{
			Array.Copy(f, sourceIndex, this.Buffer, num2, length);
		}
		else
		{
			long num4 = length + num3;
			Array.Copy(f, sourceIndex, this.Buffer, num2, num4);
			Array.Copy(f, num4 + sourceIndex, this.Buffer, 0L, -num3);
		}
		this.WriteHead += length;
	}

	public void Write(float[] f, int length)
	{
		this.Write(f, length, 0);
	}

	public void Write(float f)
	{
		if (this.WriteHead >= this.ReadHead + this._bufferSize)
		{
			this.Clear();
		}
		this.Buffer[this.HeadToIndex(this.WriteHead++)] = f;
	}

	public float Read()
	{
		if (this.ReadHead < this.WriteHead)
		{
			return this.Buffer[this.HeadToIndex(this.ReadHead++)];
		}
		return 0f;
	}

	public void ReadTo(float[] arr, long readLength, long destinationIndex = 0L)
	{
		Array.Copy(this.Buffer, this.HeadToIndex(this.ReadHead), arr, destinationIndex, readLength);
		this.ReadHead += readLength;
	}

	public void Clear()
	{
		this.ReadHead = 0L;
		this.WriteHead = 0L;
	}

	public void Reorganize()
	{
		if (this.ReadHead == 0L)
		{
			return;
		}
		int length = this.Length;
		if (length == 0)
		{
			this.Clear();
			return;
		}
		if ((float)length > PlaybackBuffer._organizerSize)
		{
			PlaybackBuffer._organizerArr = new float[length];
			PlaybackBuffer._organizerSize = length;
		}
		long num = this.HeadToIndex(this.ReadHead);
		long num2 = this._bufferSize - num;
		if (num2 >= length)
		{
			Array.Copy(this.Buffer, num, PlaybackBuffer._organizerArr, 0L, length);
		}
		else
		{
			Array.Copy(this.Buffer, num, PlaybackBuffer._organizerArr, 0L, num2);
			Array.Copy(this.Buffer, num + num2 - this._bufferSize, PlaybackBuffer._organizerArr, num2, length - num2);
		}
		Array.Copy(PlaybackBuffer._organizerArr, this.Buffer, length);
		this.ReadHead = 0L;
		this.WriteHead = length;
	}

	public int AddDelay(int samples, bool force = false)
	{
		if (!force)
		{
			samples = Mathf.Min(Mathf.Abs(samples), this._bufferSize - this.Length);
		}
		for (int i = 0; i < samples; i++)
		{
			this.Buffer[this.HeadToIndex(--this.ReadHead)] = 0f;
		}
		return samples;
	}

	public long SyncWith(PlaybackBuffer buffer, int delay = 0)
	{
		long num = this.Length - buffer.Length - delay;
		if (num >= 0)
		{
			this.ReadHead += num;
		}
		else
		{
			this.AddDelay((int)(-num), force: true);
		}
		return num;
	}

	public long HeadToIndex(long headPosition)
	{
		return (headPosition % this._bufferSize + this._bufferSize) % this._bufferSize;
	}

	public void Dispose()
	{
		PlaybackBuffer.PoolsOfSize.GetOrAddNew(this._bufferSize).Enqueue(this.Buffer);
	}
}
