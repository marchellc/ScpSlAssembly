using System;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace LiteNetLib.Utils
{
	public class NetDataWriter
	{
		public int Capacity
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._data.Length;
			}
		}

		public byte[] Data
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._data;
			}
		}

		public int Length
		{
			[MethodImpl(MethodImplOptions.AggressiveInlining)]
			get
			{
				return this._position;
			}
		}

		public NetDataWriter()
			: this(true, 64)
		{
		}

		public NetDataWriter(bool autoResize)
			: this(autoResize, 64)
		{
		}

		public NetDataWriter(bool autoResize, int initialSize)
		{
			this._data = new byte[initialSize];
			this._autoResize = autoResize;
		}

		public static NetDataWriter FromBytes(byte[] bytes, bool copy)
		{
			if (copy)
			{
				NetDataWriter netDataWriter = new NetDataWriter(true, bytes.Length);
				netDataWriter.Put(bytes);
				return netDataWriter;
			}
			return new NetDataWriter(true, 0)
			{
				_data = bytes,
				_position = bytes.Length
			};
		}

		public static NetDataWriter FromBytes(byte[] bytes, int offset, int length)
		{
			NetDataWriter netDataWriter = new NetDataWriter(true, bytes.Length);
			netDataWriter.Put(bytes, offset, length);
			return netDataWriter;
		}

		public static NetDataWriter FromString(string value)
		{
			NetDataWriter netDataWriter = new NetDataWriter();
			netDataWriter.Put(value);
			return netDataWriter;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void ResizeIfNeed(int newSize)
		{
			if (this._data.Length < newSize)
			{
				Array.Resize<byte>(ref this._data, Math.Max(newSize, this._data.Length * 2));
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public void EnsureFit(int additionalSize)
		{
			if (this._data.Length < this._position + additionalSize)
			{
				Array.Resize<byte>(ref this._data, Math.Max(this._position + additionalSize, this._data.Length * 2));
			}
		}

		public void Reset(int size)
		{
			this.ResizeIfNeed(size);
			this._position = 0;
		}

		public void Reset()
		{
			this._position = 0;
		}

		public byte[] CopyData()
		{
			byte[] array = new byte[this._position];
			Buffer.BlockCopy(this._data, 0, array, 0, this._position);
			return array;
		}

		public int SetPosition(int position)
		{
			int position2 = this._position;
			this._position = position;
			return position2;
		}

		public void Put(float value)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 4);
			}
			FastBitConverter.GetBytes(this._data, this._position, value);
			this._position += 4;
		}

		public void Put(double value)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 8);
			}
			FastBitConverter.GetBytes(this._data, this._position, value);
			this._position += 8;
		}

		public void Put(long value)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 8);
			}
			FastBitConverter.GetBytes(this._data, this._position, value);
			this._position += 8;
		}

		public void Put(ulong value)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 8);
			}
			FastBitConverter.GetBytes(this._data, this._position, value);
			this._position += 8;
		}

		public void Put(int value)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 4);
			}
			FastBitConverter.GetBytes(this._data, this._position, value);
			this._position += 4;
		}

		public void Put(uint value)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 4);
			}
			FastBitConverter.GetBytes(this._data, this._position, value);
			this._position += 4;
		}

		public void Put(char value)
		{
			this.Put((ushort)value);
		}

		public void Put(ushort value)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 2);
			}
			FastBitConverter.GetBytes(this._data, this._position, value);
			this._position += 2;
		}

		public void Put(short value)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 2);
			}
			FastBitConverter.GetBytes(this._data, this._position, value);
			this._position += 2;
		}

		public void Put(sbyte value)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 1);
			}
			this._data[this._position] = (byte)value;
			this._position++;
		}

		public void Put(byte value)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 1);
			}
			this._data[this._position] = value;
			this._position++;
		}

		public void Put(byte[] data, int offset, int length)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + length);
			}
			Buffer.BlockCopy(data, offset, this._data, this._position, length);
			this._position += length;
		}

		public void Put(byte[] data)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + data.Length);
			}
			Buffer.BlockCopy(data, 0, this._data, this._position, data.Length);
			this._position += data.Length;
		}

		public void PutSBytesWithLength(sbyte[] data, int offset, ushort length)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 2 + (int)length);
			}
			FastBitConverter.GetBytes(this._data, this._position, length);
			Buffer.BlockCopy(data, offset, this._data, this._position + 2, (int)length);
			this._position += (int)(2 + length);
		}

		public void PutSBytesWithLength(sbyte[] data)
		{
			this.PutArray(data, 1);
		}

		public void PutBytesWithLength(byte[] data, int offset, ushort length)
		{
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + 2 + (int)length);
			}
			FastBitConverter.GetBytes(this._data, this._position, length);
			Buffer.BlockCopy(data, offset, this._data, this._position + 2, (int)length);
			this._position += (int)(2 + length);
		}

		public void PutBytesWithLength(byte[] data)
		{
			this.PutArray(data, 1);
		}

		public void Put(bool value)
		{
			this.Put(value ? 1 : 0);
		}

		public void PutArray(Array arr, int sz)
		{
			ushort num = ((arr == null) ? 0 : ((ushort)arr.Length));
			sz *= (int)num;
			if (this._autoResize)
			{
				this.ResizeIfNeed(this._position + sz + 2);
			}
			FastBitConverter.GetBytes(this._data, this._position, num);
			if (arr != null)
			{
				Buffer.BlockCopy(arr, 0, this._data, this._position + 2, sz);
			}
			this._position += sz + 2;
		}

		public void PutArray(float[] value)
		{
			this.PutArray(value, 4);
		}

		public void PutArray(double[] value)
		{
			this.PutArray(value, 8);
		}

		public void PutArray(long[] value)
		{
			this.PutArray(value, 8);
		}

		public void PutArray(ulong[] value)
		{
			this.PutArray(value, 8);
		}

		public void PutArray(int[] value)
		{
			this.PutArray(value, 4);
		}

		public void PutArray(uint[] value)
		{
			this.PutArray(value, 4);
		}

		public void PutArray(ushort[] value)
		{
			this.PutArray(value, 2);
		}

		public void PutArray(short[] value)
		{
			this.PutArray(value, 2);
		}

		public void PutArray(bool[] value)
		{
			this.PutArray(value, 1);
		}

		public void PutArray(string[] value)
		{
			ushort num = ((value == null) ? 0 : ((ushort)value.Length));
			this.Put(num);
			for (int i = 0; i < (int)num; i++)
			{
				this.Put(value[i]);
			}
		}

		public void PutArray(string[] value, int strMaxLength)
		{
			ushort num = ((value == null) ? 0 : ((ushort)value.Length));
			this.Put(num);
			for (int i = 0; i < (int)num; i++)
			{
				this.Put(value[i], strMaxLength);
			}
		}

		public void Put(IPEndPoint endPoint)
		{
			this.Put(endPoint.Address.ToString());
			this.Put(endPoint.Port);
		}

		public void Put(string value)
		{
			this.Put(value, 0);
		}

		public void Put(string value, int maxLength)
		{
			if (string.IsNullOrEmpty(value))
			{
				this.Put(0);
				return;
			}
			int num = ((maxLength > 0 && value.Length > maxLength) ? maxLength : value.Length);
			int bytes = NetDataWriter.uTF8Encoding.Value.GetBytes(value, 0, num, this._stringBuffer, 0);
			if (bytes == 0 || bytes >= 65535)
			{
				this.Put(0);
				return;
			}
			this.Put(checked((ushort)(bytes + 1)));
			this.Put(this._stringBuffer, 0, bytes);
		}

		public void Put<T>(T obj) where T : INetSerializable
		{
			obj.Serialize(this);
		}

		protected byte[] _data;

		protected int _position;

		private const int InitialSize = 64;

		private readonly bool _autoResize;

		public static readonly ThreadLocal<UTF8Encoding> uTF8Encoding = new ThreadLocal<UTF8Encoding>(() => new UTF8Encoding(false, true));

		public const int StringBufferMaxLength = 65535;

		private readonly byte[] _stringBuffer = new byte[65535];
	}
}
