using System;
using System.Collections.Generic;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;

namespace LiteNetLib.Utils;

public class NetSerializer
{
	private enum CallType
	{
		Basic,
		Array,
		List
	}

	private abstract class FastCall<T>
	{
		public CallType Type;

		public virtual void Init(MethodInfo getMethod, MethodInfo setMethod, CallType type)
		{
			this.Type = type;
		}

		public abstract void Read(T inf, NetDataReader r);

		public abstract void Write(T inf, NetDataWriter w);

		public abstract void ReadArray(T inf, NetDataReader r);

		public abstract void WriteArray(T inf, NetDataWriter w);

		public abstract void ReadList(T inf, NetDataReader r);

		public abstract void WriteList(T inf, NetDataWriter w);
	}

	private abstract class FastCallSpecific<TClass, TProperty> : FastCall<TClass>
	{
		protected Func<TClass, TProperty> Getter;

		protected Action<TClass, TProperty> Setter;

		protected Func<TClass, TProperty[]> GetterArr;

		protected Action<TClass, TProperty[]> SetterArr;

		protected Func<TClass, List<TProperty>> GetterList;

		protected Action<TClass, List<TProperty>> SetterList;

		public override void ReadArray(TClass inf, NetDataReader r)
		{
			throw new InvalidTypeException("Unsupported type: " + typeof(TProperty)?.ToString() + "[]");
		}

		public override void WriteArray(TClass inf, NetDataWriter w)
		{
			throw new InvalidTypeException("Unsupported type: " + typeof(TProperty)?.ToString() + "[]");
		}

		public override void ReadList(TClass inf, NetDataReader r)
		{
			throw new InvalidTypeException("Unsupported type: List<" + typeof(TProperty)?.ToString() + ">");
		}

		public override void WriteList(TClass inf, NetDataWriter w)
		{
			throw new InvalidTypeException("Unsupported type: List<" + typeof(TProperty)?.ToString() + ">");
		}

		protected TProperty[] ReadArrayHelper(TClass inf, NetDataReader r)
		{
			ushort uShort = r.GetUShort();
			TProperty[] array = this.GetterArr(inf);
			array = ((array == null || array.Length != uShort) ? new TProperty[uShort] : array);
			this.SetterArr(inf, array);
			return array;
		}

		protected TProperty[] WriteArrayHelper(TClass inf, NetDataWriter w)
		{
			TProperty[] array = this.GetterArr(inf);
			w.Put((ushort)array.Length);
			return array;
		}

		protected List<TProperty> ReadListHelper(TClass inf, NetDataReader r, out int len)
		{
			len = r.GetUShort();
			List<TProperty> list = this.GetterList(inf);
			if (list == null)
			{
				list = new List<TProperty>(len);
				this.SetterList(inf, list);
			}
			return list;
		}

		protected List<TProperty> WriteListHelper(TClass inf, NetDataWriter w, out int len)
		{
			List<TProperty> list = this.GetterList(inf);
			if (list == null)
			{
				len = 0;
				w.Put(0);
				return null;
			}
			len = list.Count;
			w.Put((ushort)len);
			return list;
		}

		public override void Init(MethodInfo getMethod, MethodInfo setMethod, CallType type)
		{
			base.Init(getMethod, setMethod, type);
			switch (type)
			{
			case CallType.Array:
				this.GetterArr = (Func<TClass, TProperty[]>)Delegate.CreateDelegate(typeof(Func<TClass, TProperty[]>), getMethod);
				this.SetterArr = (Action<TClass, TProperty[]>)Delegate.CreateDelegate(typeof(Action<TClass, TProperty[]>), setMethod);
				break;
			case CallType.List:
				this.GetterList = (Func<TClass, List<TProperty>>)Delegate.CreateDelegate(typeof(Func<TClass, List<TProperty>>), getMethod);
				this.SetterList = (Action<TClass, List<TProperty>>)Delegate.CreateDelegate(typeof(Action<TClass, List<TProperty>>), setMethod);
				break;
			default:
				this.Getter = (Func<TClass, TProperty>)Delegate.CreateDelegate(typeof(Func<TClass, TProperty>), getMethod);
				this.Setter = (Action<TClass, TProperty>)Delegate.CreateDelegate(typeof(Action<TClass, TProperty>), setMethod);
				break;
			}
		}
	}

	private abstract class FastCallSpecificAuto<TClass, TProperty> : FastCallSpecific<TClass, TProperty>
	{
		protected abstract void ElementRead(NetDataReader r, out TProperty prop);

		protected abstract void ElementWrite(NetDataWriter w, ref TProperty prop);

		public override void Read(TClass inf, NetDataReader r)
		{
			this.ElementRead(r, out var prop);
			base.Setter(inf, prop);
		}

		public override void Write(TClass inf, NetDataWriter w)
		{
			TProperty prop = base.Getter(inf);
			this.ElementWrite(w, ref prop);
		}

		public override void ReadArray(TClass inf, NetDataReader r)
		{
			TProperty[] array = base.ReadArrayHelper(inf, r);
			for (int i = 0; i < array.Length; i++)
			{
				this.ElementRead(r, out array[i]);
			}
		}

		public override void WriteArray(TClass inf, NetDataWriter w)
		{
			TProperty[] array = base.WriteArrayHelper(inf, w);
			for (int i = 0; i < array.Length; i++)
			{
				this.ElementWrite(w, ref array[i]);
			}
		}
	}

	private sealed class FastCallStatic<TClass, TProperty> : FastCallSpecific<TClass, TProperty>
	{
		private readonly Action<NetDataWriter, TProperty> _writer;

		private readonly Func<NetDataReader, TProperty> _reader;

		public FastCallStatic(Action<NetDataWriter, TProperty> write, Func<NetDataReader, TProperty> read)
		{
			this._writer = write;
			this._reader = read;
		}

		public override void Read(TClass inf, NetDataReader r)
		{
			base.Setter(inf, this._reader(r));
		}

		public override void Write(TClass inf, NetDataWriter w)
		{
			this._writer(w, base.Getter(inf));
		}

		public override void ReadList(TClass inf, NetDataReader r)
		{
			int len;
			List<TProperty> list = base.ReadListHelper(inf, r, out len);
			int count = list.Count;
			for (int i = 0; i < len; i++)
			{
				if (i < count)
				{
					list[i] = this._reader(r);
				}
				else
				{
					list.Add(this._reader(r));
				}
			}
			if (len < count)
			{
				list.RemoveRange(len, count - len);
			}
		}

		public override void WriteList(TClass inf, NetDataWriter w)
		{
			int len;
			List<TProperty> list = base.WriteListHelper(inf, w, out len);
			for (int i = 0; i < len; i++)
			{
				this._writer(w, list[i]);
			}
		}

		public override void ReadArray(TClass inf, NetDataReader r)
		{
			TProperty[] array = base.ReadArrayHelper(inf, r);
			int num = array.Length;
			for (int i = 0; i < num; i++)
			{
				array[i] = this._reader(r);
			}
		}

		public override void WriteArray(TClass inf, NetDataWriter w)
		{
			TProperty[] array = base.WriteArrayHelper(inf, w);
			int num = array.Length;
			for (int i = 0; i < num; i++)
			{
				this._writer(w, array[i]);
			}
		}
	}

	private sealed class FastCallStruct<TClass, TProperty> : FastCallSpecific<TClass, TProperty> where TProperty : struct, INetSerializable
	{
		private TProperty _p;

		public override void Read(TClass inf, NetDataReader r)
		{
			this._p.Deserialize(r);
			base.Setter(inf, this._p);
		}

		public override void Write(TClass inf, NetDataWriter w)
		{
			this._p = base.Getter(inf);
			this._p.Serialize(w);
		}

		public override void ReadList(TClass inf, NetDataReader r)
		{
			int len;
			List<TProperty> list = base.ReadListHelper(inf, r, out len);
			int count = list.Count;
			for (int i = 0; i < len; i++)
			{
				TProperty val = default(TProperty);
				val.Deserialize(r);
				if (i < count)
				{
					list[i] = val;
				}
				else
				{
					list.Add(val);
				}
			}
			if (len < count)
			{
				list.RemoveRange(len, count - len);
			}
		}

		public override void WriteList(TClass inf, NetDataWriter w)
		{
			int len;
			List<TProperty> list = base.WriteListHelper(inf, w, out len);
			for (int i = 0; i < len; i++)
			{
				list[i].Serialize(w);
			}
		}

		public override void ReadArray(TClass inf, NetDataReader r)
		{
			TProperty[] array = base.ReadArrayHelper(inf, r);
			int num = array.Length;
			for (int i = 0; i < num; i++)
			{
				array[i].Deserialize(r);
			}
		}

		public override void WriteArray(TClass inf, NetDataWriter w)
		{
			TProperty[] array = base.WriteArrayHelper(inf, w);
			int num = array.Length;
			for (int i = 0; i < num; i++)
			{
				array[i].Serialize(w);
			}
		}
	}

	private sealed class FastCallClass<TClass, TProperty> : FastCallSpecific<TClass, TProperty> where TProperty : class, INetSerializable
	{
		private readonly Func<TProperty> _constructor;

		public FastCallClass(Func<TProperty> constructor)
		{
			this._constructor = constructor;
		}

		public override void Read(TClass inf, NetDataReader r)
		{
			TProperty val = this._constructor();
			val.Deserialize(r);
			base.Setter(inf, val);
		}

		public override void Write(TClass inf, NetDataWriter w)
		{
			base.Getter(inf)?.Serialize(w);
		}

		public override void ReadList(TClass inf, NetDataReader r)
		{
			int len;
			List<TProperty> list = base.ReadListHelper(inf, r, out len);
			int count = list.Count;
			for (int i = 0; i < len; i++)
			{
				if (i < count)
				{
					list[i].Deserialize(r);
					continue;
				}
				TProperty val = this._constructor();
				val.Deserialize(r);
				list.Add(val);
			}
			if (len < count)
			{
				list.RemoveRange(len, count - len);
			}
		}

		public override void WriteList(TClass inf, NetDataWriter w)
		{
			int len;
			List<TProperty> list = base.WriteListHelper(inf, w, out len);
			for (int i = 0; i < len; i++)
			{
				list[i].Serialize(w);
			}
		}

		public override void ReadArray(TClass inf, NetDataReader r)
		{
			TProperty[] array = base.ReadArrayHelper(inf, r);
			int num = array.Length;
			for (int i = 0; i < num; i++)
			{
				array[i] = this._constructor();
				array[i].Deserialize(r);
			}
		}

		public override void WriteArray(TClass inf, NetDataWriter w)
		{
			TProperty[] array = base.WriteArrayHelper(inf, w);
			int num = array.Length;
			for (int i = 0; i < num; i++)
			{
				array[i].Serialize(w);
			}
		}
	}

	private class IntSerializer<T> : FastCallSpecific<T, int>
	{
		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetInt());
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetIntArray());
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutArray(base.GetterArr(inf));
		}
	}

	private class UIntSerializer<T> : FastCallSpecific<T, uint>
	{
		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetUInt());
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetUIntArray());
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutArray(base.GetterArr(inf));
		}
	}

	private class ShortSerializer<T> : FastCallSpecific<T, short>
	{
		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetShort());
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetShortArray());
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutArray(base.GetterArr(inf));
		}
	}

	private class UShortSerializer<T> : FastCallSpecific<T, ushort>
	{
		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetUShort());
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetUShortArray());
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutArray(base.GetterArr(inf));
		}
	}

	private class LongSerializer<T> : FastCallSpecific<T, long>
	{
		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetLong());
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetLongArray());
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutArray(base.GetterArr(inf));
		}
	}

	private class ULongSerializer<T> : FastCallSpecific<T, ulong>
	{
		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetULong());
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetULongArray());
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutArray(base.GetterArr(inf));
		}
	}

	private class ByteSerializer<T> : FastCallSpecific<T, byte>
	{
		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetByte());
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetBytesWithLength());
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutBytesWithLength(base.GetterArr(inf));
		}
	}

	private class SByteSerializer<T> : FastCallSpecific<T, sbyte>
	{
		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetSByte());
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetSBytesWithLength());
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutSBytesWithLength(base.GetterArr(inf));
		}
	}

	private class FloatSerializer<T> : FastCallSpecific<T, float>
	{
		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetFloat());
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetFloatArray());
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutArray(base.GetterArr(inf));
		}
	}

	private class DoubleSerializer<T> : FastCallSpecific<T, double>
	{
		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetDouble());
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetDoubleArray());
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutArray(base.GetterArr(inf));
		}
	}

	private class BoolSerializer<T> : FastCallSpecific<T, bool>
	{
		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetBool());
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetBoolArray());
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutArray(base.GetterArr(inf));
		}
	}

	private class CharSerializer<T> : FastCallSpecificAuto<T, char>
	{
		protected override void ElementWrite(NetDataWriter w, ref char prop)
		{
			w.Put(prop);
		}

		protected override void ElementRead(NetDataReader r, out char prop)
		{
			prop = r.GetChar();
		}
	}

	private class IPEndPointSerializer<T> : FastCallSpecificAuto<T, IPEndPoint>
	{
		protected override void ElementWrite(NetDataWriter w, ref IPEndPoint prop)
		{
			w.Put(prop);
		}

		protected override void ElementRead(NetDataReader r, out IPEndPoint prop)
		{
			prop = r.GetNetEndPoint();
		}
	}

	private class StringSerializer<T> : FastCallSpecific<T, string>
	{
		private readonly int _maxLength;

		public StringSerializer(int maxLength)
		{
			this._maxLength = ((maxLength > 0) ? maxLength : 32767);
		}

		public override void Read(T inf, NetDataReader r)
		{
			base.Setter(inf, r.GetString(this._maxLength));
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put(base.Getter(inf), this._maxLength);
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			base.SetterArr(inf, r.GetStringArray(this._maxLength));
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			w.PutArray(base.GetterArr(inf), this._maxLength);
		}
	}

	private class EnumByteSerializer<T> : FastCall<T>
	{
		protected readonly PropertyInfo Property;

		protected readonly Type PropertyType;

		public EnumByteSerializer(PropertyInfo property, Type propertyType)
		{
			this.Property = property;
			this.PropertyType = propertyType;
		}

		public override void Read(T inf, NetDataReader r)
		{
			this.Property.SetValue(inf, Enum.ToObject(this.PropertyType, r.GetByte()), null);
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put((byte)this.Property.GetValue(inf, null));
		}

		public override void ReadArray(T inf, NetDataReader r)
		{
			throw new InvalidTypeException("Unsupported type: Enum[]");
		}

		public override void WriteArray(T inf, NetDataWriter w)
		{
			throw new InvalidTypeException("Unsupported type: Enum[]");
		}

		public override void ReadList(T inf, NetDataReader r)
		{
			throw new InvalidTypeException("Unsupported type: List<Enum>");
		}

		public override void WriteList(T inf, NetDataWriter w)
		{
			throw new InvalidTypeException("Unsupported type: List<Enum>");
		}
	}

	private class EnumIntSerializer<T> : EnumByteSerializer<T>
	{
		public EnumIntSerializer(PropertyInfo property, Type propertyType)
			: base(property, propertyType)
		{
		}

		public override void Read(T inf, NetDataReader r)
		{
			base.Property.SetValue(inf, Enum.ToObject(base.PropertyType, r.GetInt()), null);
		}

		public override void Write(T inf, NetDataWriter w)
		{
			w.Put((int)base.Property.GetValue(inf, null));
		}
	}

	private sealed class ClassInfo<T>
	{
		public static ClassInfo<T> Instance;

		private readonly FastCall<T>[] _serializers;

		private readonly int _membersCount;

		public ClassInfo(List<FastCall<T>> serializers)
		{
			this._membersCount = serializers.Count;
			this._serializers = serializers.ToArray();
		}

		public void Write(T obj, NetDataWriter writer)
		{
			for (int i = 0; i < this._membersCount; i++)
			{
				FastCall<T> fastCall = this._serializers[i];
				if (fastCall.Type == CallType.Basic)
				{
					fastCall.Write(obj, writer);
				}
				else if (fastCall.Type == CallType.Array)
				{
					fastCall.WriteArray(obj, writer);
				}
				else
				{
					fastCall.WriteList(obj, writer);
				}
			}
		}

		public void Read(T obj, NetDataReader reader)
		{
			for (int i = 0; i < this._membersCount; i++)
			{
				FastCall<T> fastCall = this._serializers[i];
				if (fastCall.Type == CallType.Basic)
				{
					fastCall.Read(obj, reader);
				}
				else if (fastCall.Type == CallType.Array)
				{
					fastCall.ReadArray(obj, reader);
				}
				else
				{
					fastCall.ReadList(obj, reader);
				}
			}
		}
	}

	private abstract class CustomType
	{
		public abstract FastCall<T> Get<T>();
	}

	private sealed class CustomTypeStruct<TProperty> : CustomType where TProperty : struct, INetSerializable
	{
		public override FastCall<T> Get<T>()
		{
			return new FastCallStruct<T, TProperty>();
		}
	}

	private sealed class CustomTypeClass<TProperty> : CustomType where TProperty : class, INetSerializable
	{
		private readonly Func<TProperty> _constructor;

		public CustomTypeClass(Func<TProperty> constructor)
		{
			this._constructor = constructor;
		}

		public override FastCall<T> Get<T>()
		{
			return new FastCallClass<T, TProperty>(this._constructor);
		}
	}

	private sealed class CustomTypeStatic<TProperty> : CustomType
	{
		private readonly Action<NetDataWriter, TProperty> _writer;

		private readonly Func<NetDataReader, TProperty> _reader;

		public CustomTypeStatic(Action<NetDataWriter, TProperty> writer, Func<NetDataReader, TProperty> reader)
		{
			this._writer = writer;
			this._reader = reader;
		}

		public override FastCall<T> Get<T>()
		{
			return new FastCallStatic<T, TProperty>(this._writer, this._reader);
		}
	}

	private NetDataWriter _writer;

	private readonly int _maxStringLength;

	private readonly Dictionary<Type, CustomType> _registeredTypes = new Dictionary<Type, CustomType>();

	public void RegisterNestedType<T>() where T : struct, INetSerializable
	{
		this._registeredTypes.Add(typeof(T), new CustomTypeStruct<T>());
	}

	public void RegisterNestedType<T>(Func<T> constructor) where T : class, INetSerializable
	{
		this._registeredTypes.Add(typeof(T), new CustomTypeClass<T>(constructor));
	}

	public void RegisterNestedType<T>(Action<NetDataWriter, T> writer, Func<NetDataReader, T> reader)
	{
		this._registeredTypes.Add(typeof(T), new CustomTypeStatic<T>(writer, reader));
	}

	public NetSerializer()
		: this(0)
	{
	}

	public NetSerializer(int maxStringLength)
	{
		this._maxStringLength = maxStringLength;
	}

	private ClassInfo<T> RegisterInternal<T>()
	{
		if (ClassInfo<T>.Instance != null)
		{
			return ClassInfo<T>.Instance;
		}
		PropertyInfo[] properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.GetProperty | BindingFlags.SetProperty);
		List<FastCall<T>> list = new List<FastCall<T>>();
		foreach (PropertyInfo propertyInfo in properties)
		{
			Type propertyType = propertyInfo.PropertyType;
			Type type = (propertyType.IsArray ? propertyType.GetElementType() : propertyType);
			CallType type2 = (propertyType.IsArray ? CallType.Array : CallType.Basic);
			if (propertyType.IsGenericType && propertyType.GetGenericTypeDefinition() == typeof(List<>))
			{
				type = propertyType.GetGenericArguments()[0];
				type2 = CallType.List;
			}
			if (Attribute.IsDefined(propertyInfo, typeof(IgnoreDataMemberAttribute)))
			{
				continue;
			}
			MethodInfo getMethod = propertyInfo.GetGetMethod();
			MethodInfo setMethod = propertyInfo.GetSetMethod();
			if (getMethod == null || setMethod == null)
			{
				continue;
			}
			FastCall<T> fastCall = null;
			if (propertyType.IsEnum)
			{
				Type underlyingType = Enum.GetUnderlyingType(propertyType);
				if (underlyingType == typeof(byte))
				{
					fastCall = new EnumByteSerializer<T>(propertyInfo, propertyType);
				}
				else
				{
					if (!(underlyingType == typeof(int)))
					{
						throw new InvalidTypeException("Not supported enum underlying type: " + underlyingType.Name);
					}
					fastCall = new EnumIntSerializer<T>(propertyInfo, propertyType);
				}
			}
			else if (type == typeof(string))
			{
				fastCall = new StringSerializer<T>(this._maxStringLength);
			}
			else if (type == typeof(bool))
			{
				fastCall = new BoolSerializer<T>();
			}
			else if (type == typeof(byte))
			{
				fastCall = new ByteSerializer<T>();
			}
			else if (type == typeof(sbyte))
			{
				fastCall = new SByteSerializer<T>();
			}
			else if (type == typeof(short))
			{
				fastCall = new ShortSerializer<T>();
			}
			else if (type == typeof(ushort))
			{
				fastCall = new UShortSerializer<T>();
			}
			else if (type == typeof(int))
			{
				fastCall = new IntSerializer<T>();
			}
			else if (type == typeof(uint))
			{
				fastCall = new UIntSerializer<T>();
			}
			else if (type == typeof(long))
			{
				fastCall = new LongSerializer<T>();
			}
			else if (type == typeof(ulong))
			{
				fastCall = new ULongSerializer<T>();
			}
			else if (type == typeof(float))
			{
				fastCall = new FloatSerializer<T>();
			}
			else if (type == typeof(double))
			{
				fastCall = new DoubleSerializer<T>();
			}
			else if (type == typeof(char))
			{
				fastCall = new CharSerializer<T>();
			}
			else if (type == typeof(IPEndPoint))
			{
				fastCall = new IPEndPointSerializer<T>();
			}
			else
			{
				this._registeredTypes.TryGetValue(type, out var value);
				if (value != null)
				{
					fastCall = value.Get<T>();
				}
			}
			if (fastCall != null)
			{
				fastCall.Init(getMethod, setMethod, type2);
				list.Add(fastCall);
				continue;
			}
			throw new InvalidTypeException("Unknown property type: " + propertyType.FullName);
		}
		ClassInfo<T>.Instance = new ClassInfo<T>(list);
		return ClassInfo<T>.Instance;
	}

	public void Register<T>()
	{
		this.RegisterInternal<T>();
	}

	public T Deserialize<T>(NetDataReader reader) where T : class, new()
	{
		ClassInfo<T> classInfo = this.RegisterInternal<T>();
		T val = new T();
		try
		{
			classInfo.Read(val, reader);
			return val;
		}
		catch
		{
			return null;
		}
	}

	public bool Deserialize<T>(NetDataReader reader, T target) where T : class, new()
	{
		ClassInfo<T> classInfo = this.RegisterInternal<T>();
		try
		{
			classInfo.Read(target, reader);
		}
		catch
		{
			return false;
		}
		return true;
	}

	public void Serialize<T>(NetDataWriter writer, T obj) where T : class, new()
	{
		this.RegisterInternal<T>().Write(obj, writer);
	}

	public byte[] Serialize<T>(T obj) where T : class, new()
	{
		if (this._writer == null)
		{
			this._writer = new NetDataWriter();
		}
		this._writer.Reset();
		this.Serialize(this._writer, obj);
		return this._writer.CopyData();
	}
}
