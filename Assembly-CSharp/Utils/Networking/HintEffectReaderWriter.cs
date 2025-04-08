using System;
using Hints;
using Mirror;
using UnityEngine;

namespace Utils.Networking
{
	public static class HintEffectReaderWriter
	{
		public static HintEffect ReadHintEffect(this NetworkReader reader)
		{
			byte b = reader.ReadByte();
			Func<NetworkReader, HintEffect> func;
			switch (b)
			{
			case 0:
				func = new Func<NetworkReader, HintEffect>(AlphaEffect.FromNetwork);
				break;
			case 1:
				func = new Func<NetworkReader, HintEffect>(AlphaCurveHintEffect.FromNetwork);
				break;
			case 2:
				func = new Func<NetworkReader, HintEffect>(OutlineEffect.FromNetwork);
				break;
			default:
				Debug.LogWarning(string.Format("Received malformed hint parameter (type {0}).", b));
				return null;
			}
			return func(reader);
		}

		public static void WriteHintEffect(this NetworkWriter writer, HintEffect effect)
		{
			if (effect == null)
			{
				throw new ArgumentNullException("effect");
			}
			HintEffectReaderWriter.HintEffectType hintEffectType;
			if (!(effect is AlphaEffect))
			{
				if (!(effect is AlphaCurveHintEffect))
				{
					if (!(effect is OutlineEffect))
					{
						throw new ArgumentException("Hint effect was of an unknown type. This type should be added to the pattern switch (needed for polymorphism to work).", "effect");
					}
					hintEffectType = HintEffectReaderWriter.HintEffectType.Outline;
				}
				else
				{
					hintEffectType = HintEffectReaderWriter.HintEffectType.AlphaCurve;
				}
			}
			else
			{
				hintEffectType = HintEffectReaderWriter.HintEffectType.Alpha;
			}
			writer.WriteByte((byte)hintEffectType);
			effect.Serialize(writer);
		}

		public enum HintEffectType : byte
		{
			Alpha,
			AlphaCurve,
			Outline
		}
	}
}
