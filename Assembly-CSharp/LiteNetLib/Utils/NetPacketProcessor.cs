using System;
using System.Collections.Generic;

namespace LiteNetLib.Utils
{
	public class NetPacketProcessor
	{
		public NetPacketProcessor()
		{
			this._netSerializer = new NetSerializer();
		}

		public NetPacketProcessor(int maxStringLength)
		{
			this._netSerializer = new NetSerializer(maxStringLength);
		}

		protected virtual ulong GetHash<T>()
		{
			return NetPacketProcessor.HashCache<T>.Id;
		}

		protected virtual NetPacketProcessor.SubscribeDelegate GetCallbackFromData(NetDataReader reader)
		{
			ulong @ulong = reader.GetULong();
			NetPacketProcessor.SubscribeDelegate subscribeDelegate;
			if (!this._callbacks.TryGetValue(@ulong, out subscribeDelegate))
			{
				throw new ParseException("Undefined packet in NetDataReader");
			}
			return subscribeDelegate;
		}

		protected virtual void WriteHash<T>(NetDataWriter writer)
		{
			writer.Put(this.GetHash<T>());
		}

		public void RegisterNestedType<T>() where T : struct, INetSerializable
		{
			this._netSerializer.RegisterNestedType<T>();
		}

		public void RegisterNestedType<T>(Action<NetDataWriter, T> writeDelegate, Func<NetDataReader, T> readDelegate)
		{
			this._netSerializer.RegisterNestedType<T>(writeDelegate, readDelegate);
		}

		public void RegisterNestedType<T>(Func<T> constructor) where T : class, INetSerializable
		{
			this._netSerializer.RegisterNestedType<T>(constructor);
		}

		public void ReadAllPackets(NetDataReader reader)
		{
			while (reader.AvailableBytes > 0)
			{
				this.ReadPacket(reader);
			}
		}

		public void ReadAllPackets(NetDataReader reader, object userData)
		{
			while (reader.AvailableBytes > 0)
			{
				this.ReadPacket(reader, userData);
			}
		}

		public void ReadPacket(NetDataReader reader)
		{
			this.ReadPacket(reader, null);
		}

		public void Write<T>(NetDataWriter writer, T packet) where T : class, new()
		{
			this.WriteHash<T>(writer);
			this._netSerializer.Serialize<T>(writer, packet);
		}

		public void WriteNetSerializable<T>(NetDataWriter writer, ref T packet) where T : INetSerializable
		{
			this.WriteHash<T>(writer);
			packet.Serialize(writer);
		}

		public void ReadPacket(NetDataReader reader, object userData)
		{
			this.GetCallbackFromData(reader)(reader, userData);
		}

		public void Subscribe<T>(Action<T> onReceive, Func<T> packetConstructor) where T : class, new()
		{
			this._netSerializer.Register<T>();
			this._callbacks[this.GetHash<T>()] = delegate(NetDataReader reader, object userData)
			{
				T t = packetConstructor();
				this._netSerializer.Deserialize<T>(reader, t);
				onReceive(t);
			};
		}

		public void Subscribe<T, TUserData>(Action<T, TUserData> onReceive, Func<T> packetConstructor) where T : class, new()
		{
			this._netSerializer.Register<T>();
			this._callbacks[this.GetHash<T>()] = delegate(NetDataReader reader, object userData)
			{
				T t = packetConstructor();
				this._netSerializer.Deserialize<T>(reader, t);
				onReceive(t, (TUserData)((object)userData));
			};
		}

		public void SubscribeReusable<T>(Action<T> onReceive) where T : class, new()
		{
			this._netSerializer.Register<T>();
			T reference = new T();
			this._callbacks[this.GetHash<T>()] = delegate(NetDataReader reader, object userData)
			{
				this._netSerializer.Deserialize<T>(reader, reference);
				onReceive(reference);
			};
		}

		public void SubscribeReusable<T, TUserData>(Action<T, TUserData> onReceive) where T : class, new()
		{
			this._netSerializer.Register<T>();
			T reference = new T();
			this._callbacks[this.GetHash<T>()] = delegate(NetDataReader reader, object userData)
			{
				this._netSerializer.Deserialize<T>(reader, reference);
				onReceive(reference, (TUserData)((object)userData));
			};
		}

		public void SubscribeNetSerializable<T, TUserData>(Action<T, TUserData> onReceive, Func<T> packetConstructor) where T : INetSerializable
		{
			this._callbacks[this.GetHash<T>()] = delegate(NetDataReader reader, object userData)
			{
				T t = packetConstructor();
				t.Deserialize(reader);
				onReceive(t, (TUserData)((object)userData));
			};
		}

		public void SubscribeNetSerializable<T>(Action<T> onReceive, Func<T> packetConstructor) where T : INetSerializable
		{
			this._callbacks[this.GetHash<T>()] = delegate(NetDataReader reader, object userData)
			{
				T t = packetConstructor();
				t.Deserialize(reader);
				onReceive(t);
			};
		}

		public void SubscribeNetSerializable<T, TUserData>(Action<T, TUserData> onReceive) where T : INetSerializable, new()
		{
			T reference = new T();
			this._callbacks[this.GetHash<T>()] = delegate(NetDataReader reader, object userData)
			{
				reference.Deserialize(reader);
				onReceive(reference, (TUserData)((object)userData));
			};
		}

		public void SubscribeNetSerializable<T>(Action<T> onReceive) where T : INetSerializable, new()
		{
			T reference = new T();
			this._callbacks[this.GetHash<T>()] = delegate(NetDataReader reader, object userData)
			{
				reference.Deserialize(reader);
				onReceive(reference);
			};
		}

		public bool RemoveSubscription<T>()
		{
			return this._callbacks.Remove(this.GetHash<T>());
		}

		private readonly NetSerializer _netSerializer;

		private readonly Dictionary<ulong, NetPacketProcessor.SubscribeDelegate> _callbacks = new Dictionary<ulong, NetPacketProcessor.SubscribeDelegate>();

		private static class HashCache<T>
		{
			static HashCache()
			{
				ulong num = 14695981039346656037UL;
				string text = typeof(T).ToString();
				for (int i = 0; i < text.Length; i++)
				{
					num ^= (ulong)text[i];
					num *= 1099511628211UL;
				}
				NetPacketProcessor.HashCache<T>.Id = num;
			}

			public static readonly ulong Id;
		}

		protected delegate void SubscribeDelegate(NetDataReader reader, object userData);
	}
}
