using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using Utf8Json.Internal.Emit;

namespace Utf8Json.Internal
{
	public class AutomataDictionary : IEnumerable<KeyValuePair<string, int>>, IEnumerable
	{
		public AutomataDictionary()
		{
			this.root = new AutomataDictionary.AutomataNode(0UL);
		}

		public void Add(string str, int value)
		{
			this.Add(JsonWriter.GetEncodedPropertyNameWithoutQuotation(str), value);
		}

		public void Add(byte[] bytes, int value)
		{
			int num = 0;
			AutomataDictionary.AutomataNode automataNode = this.root;
			int num2 = bytes.Length;
			while (num2 != 0)
			{
				ulong keySafe = AutomataKeyGen.GetKeySafe(bytes, ref num, ref num2);
				if (num2 == 0)
				{
					automataNode = automataNode.Add(keySafe, value, Encoding.UTF8.GetString(bytes));
				}
				else
				{
					automataNode = automataNode.Add(keySafe);
				}
			}
		}

		public bool TryGetValueSafe(ArraySegment<byte> key, out int value)
		{
			AutomataDictionary.AutomataNode automataNode = this.root;
			byte[] array = key.Array;
			int offset = key.Offset;
			int count = key.Count;
			while (count != 0 && automataNode != null)
			{
				automataNode = automataNode.SearchNextSafe(array, ref offset, ref count);
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

		private static void ToStringCore(IEnumerable<AutomataDictionary.AutomataNode> nexts, StringBuilder sb, int depth)
		{
			foreach (AutomataDictionary.AutomataNode automataNode in nexts)
			{
				if (depth != 0)
				{
					sb.Append(' ', depth * 2);
				}
				sb.Append("[" + automataNode.Key.ToString() + "]");
				if (automataNode.Value != -1)
				{
					sb.Append("(" + automataNode.originalKey + ")");
					sb.Append(" = ");
					sb.Append(automataNode.Value);
				}
				sb.AppendLine();
				AutomataDictionary.ToStringCore(automataNode.YieldChildren(), sb, depth + 1);
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

		private static IEnumerable<KeyValuePair<string, int>> YieldCore(IEnumerable<AutomataDictionary.AutomataNode> nexts)
		{
			foreach (AutomataDictionary.AutomataNode item in nexts)
			{
				if (item.Value != -1)
				{
					yield return new KeyValuePair<string, int>(item.originalKey, item.Value);
				}
				foreach (KeyValuePair<string, int> keyValuePair in AutomataDictionary.YieldCore(item.YieldChildren()))
				{
					yield return keyValuePair;
				}
				IEnumerator<KeyValuePair<string, int>> enumerator2 = null;
				item = null;
			}
			IEnumerator<AutomataDictionary.AutomataNode> enumerator = null;
			yield break;
			yield break;
		}

		public void EmitMatch(ILGenerator il, LocalBuilder p, LocalBuilder rest, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound)
		{
			this.root.EmitSearchNext(il, p, rest, key, onFound, onNotFound);
		}

		private readonly AutomataDictionary.AutomataNode root;

		private class AutomataNode : IComparable<AutomataDictionary.AutomataNode>
		{
			public bool HasChildren
			{
				get
				{
					return this.count != 0;
				}
			}

			public AutomataNode(ulong key)
			{
				this.Key = key;
				this.Value = -1;
				this.nexts = AutomataDictionary.AutomataNode.emptyNodes;
				this.nextKeys = AutomataDictionary.AutomataNode.emptyKeys;
				this.count = 0;
				this.originalKey = null;
			}

			public AutomataDictionary.AutomataNode Add(ulong key)
			{
				int num = Array.BinarySearch<ulong>(this.nextKeys, 0, this.count, key);
				if (num < 0)
				{
					if (this.nexts.Length == this.count)
					{
						Array.Resize<AutomataDictionary.AutomataNode>(ref this.nexts, (this.count == 0) ? 4 : (this.count * 2));
						Array.Resize<ulong>(ref this.nextKeys, (this.count == 0) ? 4 : (this.count * 2));
					}
					this.count++;
					AutomataDictionary.AutomataNode automataNode = new AutomataDictionary.AutomataNode(key);
					this.nexts[this.count - 1] = automataNode;
					this.nextKeys[this.count - 1] = key;
					Array.Sort<AutomataDictionary.AutomataNode>(this.nexts, 0, this.count);
					Array.Sort<ulong>(this.nextKeys, 0, this.count);
					return automataNode;
				}
				return this.nexts[num];
			}

			public AutomataDictionary.AutomataNode Add(ulong key, int value, string originalKey)
			{
				AutomataDictionary.AutomataNode automataNode = this.Add(key);
				automataNode.Value = value;
				automataNode.originalKey = originalKey;
				return automataNode;
			}

			public unsafe AutomataDictionary.AutomataNode SearchNext(ref byte* p, ref int rest)
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
					int num = AutomataDictionary.AutomataNode.BinarySearch(this.nextKeys, 0, this.count, key);
					if (num >= 0)
					{
						return this.nexts[num];
					}
				}
				return null;
			}

			public AutomataDictionary.AutomataNode SearchNextSafe(byte[] p, ref int offset, ref int rest)
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
					int num = AutomataDictionary.AutomataNode.BinarySearch(this.nextKeys, 0, this.count, keySafe);
					if (num >= 0)
					{
						return this.nexts[num];
					}
				}
				return null;
			}

			internal static int BinarySearch(ulong[] array, int index, int length, ulong value)
			{
				int i = index;
				int num = index + length - 1;
				while (i <= num)
				{
					int num2 = i + (num - i >> 1);
					ulong num3 = array[num2];
					int num4;
					if (num3 < value)
					{
						num4 = -1;
					}
					else if (num3 > value)
					{
						num4 = 1;
					}
					else
					{
						num4 = 0;
					}
					if (num4 == 0)
					{
						return num2;
					}
					if (num4 < 0)
					{
						i = num2 + 1;
					}
					else
					{
						num = num2 - 1;
					}
				}
				return ~i;
			}

			public int CompareTo(AutomataDictionary.AutomataNode other)
			{
				return this.Key.CompareTo(other.Key);
			}

			public IEnumerable<AutomataDictionary.AutomataNode> YieldChildren()
			{
				int num;
				for (int i = 0; i < this.count; i = num + 1)
				{
					yield return this.nexts[i];
					num = i;
				}
				yield break;
			}

			public void EmitSearchNext(ILGenerator il, LocalBuilder p, LocalBuilder rest, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound)
			{
				il.EmitLdloca(p);
				il.EmitLdloca(rest);
				il.EmitCall(AutomataKeyGen.GetKeyMethod);
				il.EmitStloc(key);
				AutomataDictionary.AutomataNode.EmitSearchNextCore(il, p, rest, key, onFound, onNotFound, this.nexts, this.count);
			}

			private static void EmitSearchNextCore(ILGenerator il, LocalBuilder p, LocalBuilder rest, LocalBuilder key, Action<KeyValuePair<string, int>> onFound, Action onNotFound, AutomataDictionary.AutomataNode[] nexts, int count)
			{
				if (count < 4)
				{
					AutomataDictionary.AutomataNode[] array = (from x in nexts.Take(count)
						where x.Value != -1
						select x).ToArray<AutomataDictionary.AutomataNode>();
					AutomataDictionary.AutomataNode[] array2 = (from x in nexts.Take(count)
						where x.HasChildren
						select x).ToArray<AutomataDictionary.AutomataNode>();
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
						select il.DefineLabel()).ToArray<Label>();
					for (int i = 0; i < array.Length; i++)
					{
						Label label3 = il.DefineLabel();
						if (i != 0)
						{
							il.MarkLabel(array3[i - 1]);
						}
						il.EmitLdloc(key);
						il.EmitULong(array[i].Key);
						il.Emit(OpCodes.Bne_Un, label3);
						onFound(new KeyValuePair<string, int>(array[i].originalKey, array[i].Value));
						il.MarkLabel(label3);
						if (i != array.Length - 1)
						{
							il.Emit(OpCodes.Br, array3[i]);
						}
						else
						{
							onNotFound();
						}
					}
					il.MarkLabel(label);
					Label[] array4 = (from _ in Enumerable.Range(0, Math.Max(array2.Length - 1, 0))
						select il.DefineLabel()).ToArray<Label>();
					for (int j = 0; j < array2.Length; j++)
					{
						Label label4 = il.DefineLabel();
						if (j != 0)
						{
							il.MarkLabel(array4[j - 1]);
						}
						il.EmitLdloc(key);
						il.EmitULong(array2[j].Key);
						il.Emit(OpCodes.Bne_Un, label4);
						array2[j].EmitSearchNext(il, p, rest, key, onFound, onNotFound);
						il.MarkLabel(label4);
						if (j != array2.Length - 1)
						{
							il.Emit(OpCodes.Br, array4[j]);
						}
						else
						{
							onNotFound();
						}
					}
					il.MarkLabel(label2);
					onNotFound();
					return;
				}
				int num = count / 2;
				ulong key2 = nexts[num].Key;
				AutomataDictionary.AutomataNode[] array5 = nexts.Take(count).Take(num).ToArray<AutomataDictionary.AutomataNode>();
				AutomataDictionary.AutomataNode[] array6 = nexts.Take(count).Skip(num).ToArray<AutomataDictionary.AutomataNode>();
				Label label5 = il.DefineLabel();
				il.EmitLdloc(key);
				il.EmitULong(key2);
				il.Emit(OpCodes.Bge, label5);
				AutomataDictionary.AutomataNode.EmitSearchNextCore(il, p, rest, key, onFound, onNotFound, array5, array5.Length);
				il.MarkLabel(label5);
				AutomataDictionary.AutomataNode.EmitSearchNextCore(il, p, rest, key, onFound, onNotFound, array6, array6.Length);
			}

			private static readonly AutomataDictionary.AutomataNode[] emptyNodes = new AutomataDictionary.AutomataNode[0];

			private static readonly ulong[] emptyKeys = new ulong[0];

			public ulong Key;

			public int Value;

			public string originalKey;

			private AutomataDictionary.AutomataNode[] nexts;

			private ulong[] nextKeys;

			private int count;
		}
	}
}
