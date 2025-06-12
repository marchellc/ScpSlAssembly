using System;
using Mirror;
using UnityEngine;

namespace Utils.Networking;

public static class AnimationCurveReaderWriter
{
	private struct NetworkKeyframe
	{
		public Keyframe Keyframe { get; private set; }

		public bool Tangental { get; private set; }

		public bool Weighted { get; private set; }

		private static void GetFlagsFromOffset(ref byte bitOffset, out byte tangentalFlag, out byte weightedFlag)
		{
			tangentalFlag = (byte)(1 << (int)bitOffset++);
			weightedFlag = (byte)(1 << (int)bitOffset++);
		}

		public NetworkKeyframe(Keyframe keyframe)
		{
			this.Keyframe = keyframe;
			this.Tangental = Mathf.Abs(keyframe.inTangent) > float.Epsilon || Mathf.Abs(keyframe.inTangent) > float.Epsilon;
			this.Weighted = Mathf.Abs(keyframe.inWeight) > float.Epsilon || Mathf.Abs(keyframe.outWeight) > float.Epsilon;
		}

		public void ReadMetaTable(in byte flag, ref byte bitOffset)
		{
			NetworkKeyframe.GetFlagsFromOffset(ref bitOffset, out var tangentalFlag, out var weightedFlag);
			this.Tangental = (flag & tangentalFlag) == tangentalFlag;
			this.Weighted = (flag & weightedFlag) == weightedFlag;
		}

		public void WriteMetaTable(ref byte flag, ref byte bitOffset)
		{
			NetworkKeyframe.GetFlagsFromOffset(ref bitOffset, out var tangentalFlag, out var weightedFlag);
			if (this.Tangental)
			{
				flag |= tangentalFlag;
			}
			if (this.Weighted)
			{
				flag |= weightedFlag;
			}
		}

		public void ReadData(NetworkReader reader)
		{
			float time = reader.ReadFloat();
			float value = reader.ReadFloat();
			float inTangent;
			float outTangent;
			if (this.Tangental)
			{
				inTangent = reader.ReadFloat();
				outTangent = reader.ReadFloat();
			}
			else
			{
				inTangent = 0f;
				outTangent = 0f;
			}
			float inWeight;
			float outWeight;
			if (this.Weighted)
			{
				inWeight = reader.ReadFloat();
				outWeight = reader.ReadFloat();
			}
			else
			{
				inWeight = 0f;
				outWeight = 0f;
			}
			this.Keyframe = new Keyframe(time, value, inTangent, outTangent, inWeight, outWeight);
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

	public const byte KeyCountOffset = 2;

	public static AnimationCurve ReadAnimationCurve(this NetworkReader reader)
	{
		NetworkKeyframe[] array = new NetworkKeyframe[reader.ReadByte() + 2];
		int num = 0;
		while (num < array.Length)
		{
			byte flag = reader.ReadByte();
			byte bitOffset = 0;
			do
			{
				array[num].ReadMetaTable(in flag, ref bitOffset);
				num++;
			}
			while (bitOffset < 8 && num < array.Length);
		}
		for (int i = 0; i < array.Length; i++)
		{
			array[i].ReadData(reader);
		}
		return new AnimationCurve(Array.ConvertAll(array, (NetworkKeyframe input) => input.Keyframe))
		{
			postWrapMode = WrapMode.Loop
		};
	}

	public static void WriteAnimationCurve(this NetworkWriter writer, AnimationCurve animationCurve)
	{
		if (animationCurve.length > 257)
		{
			throw new ArgumentException("Curve cannot have more than " + 257 + " keys.", "animationCurve");
		}
		NetworkKeyframe[] array = Array.ConvertAll(animationCurve.keys, (Keyframe input) => new NetworkKeyframe(input));
		writer.WriteByte((byte)(array.Length - 2));
		int num = 0;
		while (num < array.Length)
		{
			byte flag = 0;
			byte bitOffset = 0;
			do
			{
				array[num].WriteMetaTable(ref flag, ref bitOffset);
				num++;
			}
			while (bitOffset < 8 && num < array.Length);
			writer.WriteByte(flag);
		}
		for (int num2 = 0; num2 < array.Length; num2++)
		{
			array[num2].WriteData(writer);
		}
	}
}
