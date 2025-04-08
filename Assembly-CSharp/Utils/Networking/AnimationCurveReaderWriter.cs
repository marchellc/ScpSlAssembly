using System;
using Mirror;
using UnityEngine;

namespace Utils.Networking
{
	public static class AnimationCurveReaderWriter
	{
		public static AnimationCurve ReadAnimationCurve(this NetworkReader reader)
		{
			AnimationCurveReaderWriter.NetworkKeyframe[] array = new AnimationCurveReaderWriter.NetworkKeyframe[(int)(reader.ReadByte() + 2)];
			int i = 0;
			while (i < array.Length)
			{
				byte b = reader.ReadByte();
				byte b2 = 0;
				do
				{
					array[i].ReadMetaTable(in b, ref b2);
					i++;
				}
				while (b2 < 8 && i < array.Length);
			}
			for (int j = 0; j < array.Length; j++)
			{
				array[j].ReadData(reader);
			}
			return new AnimationCurve(Array.ConvertAll<AnimationCurveReaderWriter.NetworkKeyframe, Keyframe>(array, (AnimationCurveReaderWriter.NetworkKeyframe input) => input.Keyframe))
			{
				postWrapMode = WrapMode.Loop
			};
		}

		public static void WriteAnimationCurve(this NetworkWriter writer, AnimationCurve animationCurve)
		{
			if (animationCurve.length > 257)
			{
				throw new ArgumentException("Curve cannot have more than " + 257.ToString() + " keys.", "animationCurve");
			}
			AnimationCurveReaderWriter.NetworkKeyframe[] array = Array.ConvertAll<Keyframe, AnimationCurveReaderWriter.NetworkKeyframe>(animationCurve.keys, (Keyframe input) => new AnimationCurveReaderWriter.NetworkKeyframe(input));
			writer.WriteByte((byte)(array.Length - 2));
			int i = 0;
			while (i < array.Length)
			{
				byte b = 0;
				byte b2 = 0;
				do
				{
					array[i].WriteMetaTable(ref b, ref b2);
					i++;
				}
				while (b2 < 8 && i < array.Length);
				writer.WriteByte(b);
			}
			for (int j = 0; j < array.Length; j++)
			{
				array[j].WriteData(writer);
			}
		}

		public const byte KeyCountOffset = 2;

		private struct NetworkKeyframe
		{
			private static void GetFlagsFromOffset(ref byte bitOffset, out byte tangentalFlag, out byte weightedFlag)
			{
				int num = 1;
				byte b = bitOffset;
				bitOffset = b + 1;
				tangentalFlag = num << (int)(b & 31);
				int num2 = 1;
				b = bitOffset;
				bitOffset = b + 1;
				weightedFlag = num2 << (int)(b & 31);
			}

			public Keyframe Keyframe { readonly get; private set; }

			public bool Tangental { readonly get; private set; }

			public bool Weighted { readonly get; private set; }

			public NetworkKeyframe(Keyframe keyframe)
			{
				this.Keyframe = keyframe;
				this.Tangental = Mathf.Abs(keyframe.inTangent) > float.Epsilon || Mathf.Abs(keyframe.inTangent) > float.Epsilon;
				this.Weighted = Mathf.Abs(keyframe.inWeight) > float.Epsilon || Mathf.Abs(keyframe.outWeight) > float.Epsilon;
			}

			public void ReadMetaTable(in byte flag, ref byte bitOffset)
			{
				byte b;
				byte b2;
				AnimationCurveReaderWriter.NetworkKeyframe.GetFlagsFromOffset(ref bitOffset, out b, out b2);
				this.Tangental = (flag & b) == b;
				this.Weighted = (flag & b2) == b2;
			}

			public void WriteMetaTable(ref byte flag, ref byte bitOffset)
			{
				byte b;
				byte b2;
				AnimationCurveReaderWriter.NetworkKeyframe.GetFlagsFromOffset(ref bitOffset, out b, out b2);
				if (this.Tangental)
				{
					flag |= b;
				}
				if (this.Weighted)
				{
					flag |= b2;
				}
			}

			public void ReadData(NetworkReader reader)
			{
				float num = reader.ReadFloat();
				float num2 = reader.ReadFloat();
				float num3;
				float num4;
				if (this.Tangental)
				{
					num3 = reader.ReadFloat();
					num4 = reader.ReadFloat();
				}
				else
				{
					num3 = 0f;
					num4 = 0f;
				}
				float num5;
				float num6;
				if (this.Weighted)
				{
					num5 = reader.ReadFloat();
					num6 = reader.ReadFloat();
				}
				else
				{
					num5 = 0f;
					num6 = 0f;
				}
				this.Keyframe = new Keyframe(num, num2, num3, num4, num5, num6);
			}

			public void WriteData(NetworkWriter writer)
			{
				writer.WriteFloat(this.Keyframe.time);
				writer.WriteFloat(this.Keyframe.value);
				if (this.Tangental)
				{
					writer.WriteFloat(this.Keyframe.inTangent);
					writer.WriteFloat(this.Keyframe.outTangent);
				}
				if (this.Weighted)
				{
					writer.WriteFloat(this.Keyframe.inWeight);
					writer.WriteFloat(this.Keyframe.outWeight);
				}
			}
		}
	}
}
