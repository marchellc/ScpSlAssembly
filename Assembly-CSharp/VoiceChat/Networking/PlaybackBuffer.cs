using System;
using System.Collections.Generic;
using UnityEngine;

namespace VoiceChat.Networking
{
	public class PlaybackBuffer : IDisposable
	{
		public float ReplayVolumeScale { get; set; }

		public PlaybackBuffer(int capacity = 24000, bool endlessTapeMode = false)
		{
			this._bufferSize = capacity;
			Queue<float[]> queue;
			float[] array;
			this.Buffer = ((PlaybackBuffer.PoolsOfSize.TryGetValue(capacity, out queue) && queue.TryDequeue(out array)) ? array : new float[this._bufferSize]);
			this._endless = endlessTapeMode;
			this.ReadHead = 0L;
			this.WriteHead = 0L;
		}

		public int Length
		{
			get
			{
				return (int)(this.WriteHead - this.ReadHead);
			}
		}

		public void Write(float[] f, int length, int sourceIndex)
		{
			long num = this.WriteHead + (long)length - this.ReadHead - (long)this._bufferSize;
			if (num > 0L)
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
			long num3 = (long)this._bufferSize - num2 - (long)length;
			if (num3 >= 0L)
			{
				Array.Copy(f, (long)sourceIndex, this.Buffer, num2, (long)length);
			}
			else
			{
				long num4 = (long)length + num3;
				Array.Copy(f, (long)sourceIndex, this.Buffer, num2, num4);
				Array.Copy(f, num4 + (long)sourceIndex, this.Buffer, 0L, -num3);
			}
			this.WriteHead += (long)length;
		}

		public void Write(float[] f, int length)
		{
			this.Write(f, length, 0);
		}

		public void Write(float f)
		{
			if (this.WriteHead >= this.ReadHead + (long)this._bufferSize)
			{
				this.Clear();
			}
			float[] buffer = this.Buffer;
			long writeHead = this.WriteHead;
			this.WriteHead = writeHead + 1L;
			buffer[(int)(checked((IntPtr)this.HeadToIndex(writeHead)))] = f;
		}

		public float Read()
		{
			if (this.ReadHead < this.WriteHead)
			{
				float[] buffer = this.Buffer;
				long readHead = this.ReadHead;
				this.ReadHead = readHead + 1L;
				return buffer[(int)(checked((IntPtr)this.HeadToIndex(readHead)))];
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
				PlaybackBuffer._organizerSize = (float)length;
			}
			long num = this.HeadToIndex(this.ReadHead);
			long num2 = (long)this._bufferSize - num;
			if (num2 >= (long)length)
			{
				Array.Copy(this.Buffer, num, PlaybackBuffer._organizerArr, 0L, (long)length);
			}
			else
			{
				Array.Copy(this.Buffer, num, PlaybackBuffer._organizerArr, 0L, num2);
				Array.Copy(this.Buffer, num + num2 - (long)this._bufferSize, PlaybackBuffer._organizerArr, num2, (long)length - num2);
			}
			Array.Copy(PlaybackBuffer._organizerArr, this.Buffer, length);
			this.ReadHead = 0L;
			this.WriteHead = (long)length;
		}

		public int AddDelay(int samples, bool force = false)
		{
			if (!force)
			{
				samples = Mathf.Min(Mathf.Abs(samples), this._bufferSize - this.Length);
			}
			for (int i = 0; i < samples; i++)
			{
				float[] buffer = this.Buffer;
				long num = this.ReadHead - 1L;
				this.ReadHead = num;
				buffer[(int)(checked((IntPtr)this.HeadToIndex(num)))] = 0f;
			}
			return samples;
		}

		public long SyncWith(PlaybackBuffer buffer, int delay = 0)
		{
			long num = (long)(this.Length - buffer.Length - delay);
			if (num >= 0L)
			{
				this.ReadHead += num;
			}
			else
			{
				this.AddDelay((int)(-(int)num), true);
			}
			return num;
		}

		public long HeadToIndex(long headPosition)
		{
			return (headPosition % (long)this._bufferSize + (long)this._bufferSize) % (long)this._bufferSize;
		}

		public void Dispose()
		{
			PlaybackBuffer.PoolsOfSize.GetOrAdd(this._bufferSize, () => new Queue<float[]>()).Enqueue(this.Buffer);
		}

		private static readonly Dictionary<int, Queue<float[]>> PoolsOfSize = new Dictionary<int, Queue<float[]>>();

		private static float[] _organizerArr;

		private static float _organizerSize;

		private readonly int _bufferSize;

		private readonly bool _endless;

		public long ReadHead;

		public long WriteHead;

		public readonly float[] Buffer;
	}
}
