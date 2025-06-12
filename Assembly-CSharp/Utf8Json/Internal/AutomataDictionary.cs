using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Internal;

public class AutomataDictionary : IEnumerable<KeyValuePair<string, int>>, IEnumerable
{
	private class AutomataNode : IComparable<AutomataNode>
	{
		private static readonly AutomataNode[] emptyNodes = new AutomataNode[0];

		private static readonly ulong[] emptyKeys = new ulong[0];

		public ulong Key;

		public int Value;

		public string originalKey;

		private AutomataNode[] nexts;

		private ulong[] nextKeys;

		private int count;

		public bool HasChildren => this.count != 0;

		public AutomataNode(ulong key)
		{
			this.Key = key;
			this.Value = -1;
			this.nexts = AutomataNode.emptyNodes;
			this.nextKeys = AutomataNode.emptyKeys;
			this.count = 0;
			this.originalKey = null;
		}

		public AutomataNode Add(ulong key)
		{
			int num = Array.BinarySearch(this.nextKeys, 0, this.count, key);
			if (num < 0)
			{
				if (this.nexts.Length == this.count)
				{
					Array.Resize(ref this.nexts, (this.count == 0) ? 4 : (this.count * 2));
					Array.Resize(ref this.nextKeys, (this.count == 0) ? 4 : (this.count * 2));
				}
				this.count++;
				AutomataNode automataNode = new AutomataNode(key);
				this.nexts[this.count - 1] = automataNode;
				this.nextKeys[this.count - 1] = key;
				Array.Sort(this.nexts, 0, this.count);
				Array.Sort(this.nextKeys, 0, this.count);
				return automataNode;
			}
			return this.nexts[num];
		}

		public AutomataNode Add(ulong key, int value, string originalKey)
		{
			AutomataNode automataNode = this.Add(key);
			automataNode.Value = value;
			automataNode.originalKey = originalKey;
			return automataNode;
		}

		public unsafe AutomataNode SearchNext(ref byte* p, ref int rest)
		{
			ulong key = AutomataKeyGen.GetKey(ref p, ref rest);
			if (this.count < 4)
			{
				for (int i = 0; i < this.count; i++)
				{
					if (this.nextKeys[i] == key)
					{
						return this.nexts[i];
					}
				}
			}
			else
			{
				int num = AutomataNode.BinarySearch(this.nextKeys, 0, this.count, key);
				if (num >= 0)
				{
					return this.nexts[num];
				}
			}
			return null;
		}

		public AutomataNode SearchNextSafe(byte[] p, ref int offset, ref int rest)
		{
			ulong keySafe = AutomataKeyGen.GetKeySafe(p, ref offset, ref rest);
			if (this.count < 4)
			{
				for (int i = 0; i < this.count; i++)
				{
					if (this.nextKeys[i] == keySafe)
					{
						return this.nexts[i];
					}
				}
			}
			else
			{
				int num = AutomataNode.BinarySearch(this.nextKeys, 0, this.count, keySafe);
				if (num >= 0)
				{
					return this.nexts[num];
				}
			}
			return null;
		}

		internal static int BinarySearch(ulong[] array, int index, int length, ulong value)
		{
			int num = index;
			int num2 = index + length - 1;
			while (num <= num2)
			{
				int num3 = num + (num2 - num >> 1);
				ulong num4 = array[num3];
				int num5 = ((num4 < value) ? (-1) : ((num4 > value) ? 1 : 0));
				if (num5 == 0)
				{
					return num3;
				}
				if (num5 < 0)
				{
					num = num3 + 1;
				}
				else
				{
					num2 = num3 - 1;
				}
			}
			return ~num;
		}

		public int CompareTo(AutomataNode other)
		{
			return this.Key.CompareTo(other.Key);
		}

		public IEnumerable<AutomataNode> YieldChildren()
		{
			for (int i = 0; i < this.count; i++)
			{
				yield return this.nexts[i];
			}
		}

		public void EmitSearchNext(ILGenerator il, LocalBuilder p, LocalBuilder rest, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound)
		{
			il.EmitLdloca(p);
			il.EmitLdloca(rest);
			il.EmitCall(AutomataKeyGen.GetKeyMethod);
			il.EmitStloc(key);
			AutomataNode.EmitSearchNextCore(il, p, rest, key, onFound, onNotFound, this.nexts, this.count);
		}

		private static void EmitSearchNextCore(ILGenerator il, LocalBuilder p, LocalBuilder rest, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound, AutomataNode[] nexts, int count)
		{
			if (count < 4)
			{
				AutomataNode[] array = (from x in nexts.Take(count)
					where x.Value != -1
					select x).ToArray();
				AutomataNode[] array2 = (from x in nexts.Take(count)
					where x.HasChildren
					select x).ToArray();
				Label label = il.DefineLabel();
				Label label2 = il.DefineLabel();
				il.EmitLdloc(rest);
				if (array2.Length != 0 && array.Length == 0)
				{
					il.Emit(OpCodes.Brfalse, label2);
				}
				else
				{
					il.Emit(OpCodes.Brtrue, label);
				}
				Label[] array3 = (from _ in Enumerable.Range(0, Math.Max(array.Length - 1, 0))
					select il.DefineLabel()).ToArray();
				for (int num = 0; num < array.Length; num++)
				{
					Label label3 = il.DefineLabel();
					if (num != 0)
					{
						il.MarkLabel(array3[num - 1]);
					}
					il.EmitLdloc(key);
					il.EmitULong(array[num].Key);
					il.Emit(OpCodes.Bne_Un, label3);
					onFound(new KeyValuePair<string, int>(array[num].originalKey, array[num].Value));
					il.MarkLabel(label3);
					if (num != array.Length - 1)
					{
						il.Emit(OpCodes.Br, array3[num]);
					}
					else
					{
						onNotFound();
					}
				}
				il.MarkLabel(label);
				Label[] array4 = (from _ in Enumerable.Range(0, Math.Max(array2.Length - 1, 0))
					select il.DefineLabel()).ToArray();
				for (int num2 = 0; num2 < array2.Length; num2++)
				{
					Label label4 = il.DefineLabel();
					if (num2 != 0)
					{
						il.MarkLabel(array4[num2 - 1]);
					}
					il.EmitLdloc(key);
					il.EmitULong(array2[num2].Key);
					il.Emit(OpCodes.Bne_Un, label4);
					array2[num2].EmitSearchNext(il, p, rest, key, onFound, onNotFound);
					il.MarkLabel(label4);
					if (num2 != array2.Length - 1)
					{
						il.Emit(OpCodes.Br, array4[num2]);
					}
					else
					{
						onNotFound();
					}
				}
				il.MarkLabel(label2);
				onNotFound();
			}
			else
			{
				int num3 = count / 2;
				ulong key2 = nexts[num3].Key;
				AutomataNode[] array5 = nexts.Take(count).Take(num3).ToArray();
				AutomataNode[] array6 = nexts.Take(count).Skip(num3).ToArray();
				Label label5 = il.DefineLabel();
				il.EmitLdloc(key);
				il.EmitULong(key2);
				il.Emit(OpCodes.Bge, label5);
				AutomataNode.EmitSearchNextCore(il, p, rest, key, onFound, onNotFound, array5, array5.Length);
				il.MarkLabel(label5);
				AutomataNode.EmitSearchNextCore(il, p, rest, key, onFound, onNotFound, array6, array6.Length);
			}
		}
	}

	private readonly AutomataNode root;

	public AutomataDictionary()
	{
		this.root = new AutomataNode(0uL);
	}

	public void Add(string str, int value)
	{
		this.Add(JsonWriter.GetEncodedPropertyNameWithoutQuotation(str), value);
	}

	public void Add(byte[] bytes, int value)
	{
		int offset = 0;
		AutomataNode automataNode = this.root;
		int rest = bytes.Length;
		while (rest != 0)
		{
			ulong keySafe = AutomataKeyGen.GetKeySafe(bytes, ref offset, ref rest);
			automataNode = ((rest != 0) ? automataNode.Add(keySafe) : automataNode.Add(keySafe, value, Encoding.UTF8.GetString(bytes)));
		}
	}

	public bool TryGetValueSafe(ArraySegment<byte> key, out int value)
	{
		AutomataNode automataNode = this.root;
		byte[] array = key.Array;
		int offset = key.Offset;
		int rest = key.Count;
		while (rest != 0 && automataNode != null)
		{
			automataNode = automataNode.SearchNextSafe(array, ref offset, ref rest);
		}
		if (automataNode == null)
		{
			value = -1;
			return false;
		}
		value = automataNode.Value;
		return true;
	}

	public override string ToString()
	{
		StringBuilder stringBuilder = new StringBuilder();
		AutomataDictionary.ToStringCore(this.root.YieldChildren(), stringBuilder, 0);
		return stringBuilder.ToString();
	}

	private static void ToStringCore(IEnumerable<AutomataNode> nexts, StringBuilder sb, int depth)
	{
		foreach (AutomataNode next in nexts)
		{
			if (depth != 0)
			{
				sb.Append(' ', depth * 2);
			}
			sb.Append("[" + next.Key + "]");
			if (next.Value != -1)
			{
				sb.Append("(" + next.originalKey + ")");
				sb.Append(" = ");
				sb.Append(next.Value);
			}
			sb.AppendLine();
			AutomataDictionary.ToStringCore(next.YieldChildren(), sb, depth + 1);
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return this.GetEnumerator();
	}

	public IEnumerator<KeyValuePair<string, int>> GetEnumerator()
	{
		return AutomataDictionary.YieldCore(this.root.YieldChildren()).GetEnumerator();
	}

	private static IEnumerable<KeyValuePair<string, int>> YieldCore(IEnumerable<AutomataNode> nexts)
	{
		foreach (AutomataNode item in nexts)
		{
			if (item.Value != -1)
			{
				yield return new KeyValuePair<string, int>(item.originalKey, item.Value);
			}
			foreach (KeyValuePair<string, int> item2 in AutomataDictionary.YieldCore(item.YieldChildren()))
			{
				yield return item2;
			}
		}
	}

	public void EmitMatch(ILGenerator il, LocalBuilder p, LocalBuilder rest, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound)
	{
		this.root.EmitSearchNext(il, p, rest, key, onFound, onNotFound);
	}
}
