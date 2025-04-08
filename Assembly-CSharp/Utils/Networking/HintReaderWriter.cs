using System;
using Hints;
using Mirror;
using UnityEngine;

namespace Utils.Networking
{
	public static class HintReaderWriter
	{
		public static Hint ReadHint(this NetworkReader reader)
		{
			byte b = reader.ReadByte();
			HintReaderWriter.HintType hintType = (HintReaderWriter.HintType)b;
			Func<NetworkReader, Hint> func;
			if (hintType != HintReaderWriter.HintType.Translation)
			{
				if (hintType != HintReaderWriter.HintType.Text)
				{
					Debug.LogWarning(string.Format("Received malformed hint (type {0}).", b));
					return null;
				}
				func = new Func<NetworkReader, Hint>(TextHint.FromNetwork);
			}
			else
			{
				func = new Func<NetworkReader, Hint>(TranslationHint.FromNetwork);
			}
			return func(reader);
		}

		public static void WriteHint(this NetworkWriter writer, Hint hint)
		{
			if (hint == null)
			{
				throw new ArgumentNullException("hint");
			}
			HintReaderWriter.HintType hintType;
			if (!(hint is TranslationHint))
			{
				if (!(hint is TextHint))
				{
					throw new ArgumentException("Hint was of an unknown type. This type should be added to the pattern switch (needed for polymorphism to work).", "hint");
				}
				hintType = HintReaderWriter.HintType.Text;
			}
			else
			{
				hintType = HintReaderWriter.HintType.Translation;
			}
			writer.WriteByte((byte)hintType);
			hint.Serialize(writer);
		}

		public enum HintType : byte
		{
			Translation,
			Text
		}
	}
}
