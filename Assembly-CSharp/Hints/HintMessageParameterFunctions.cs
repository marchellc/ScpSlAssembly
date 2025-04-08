using System;
using Mirror;
using UnityEngine;

namespace Hints
{
	public static class HintMessageParameterFunctions
	{
		public static void Serialize(this NetworkWriter writer, HintMessage value)
		{
			Hint content = value.Content;
			if (!(content is TextHint))
			{
				if (!(content is TranslationHint))
				{
					Debug.LogError("Attempted to serialize an unknown type of HintMessage!");
					writer.WriteByte(0);
					return;
				}
				writer.WriteByte(2);
			}
			else
			{
				writer.WriteByte(1);
			}
			value.Content.Serialize(writer);
		}

		public static HintMessage Deserialize(this NetworkReader reader)
		{
			Hint hint;
			switch (reader.ReadByte())
			{
			case 0:
				Debug.LogError("Unknown type of HintMessage has been received!");
				return default(HintMessage);
			case 1:
				hint = TextHint.FromNetwork(reader);
				break;
			case 2:
				hint = TranslationHint.FromNetwork(reader);
				break;
			default:
				Debug.LogError("Invalid type of HintMessage has been received!");
				return default(HintMessage);
			}
			return new HintMessage(hint);
		}

		private enum HintMessageTypes : byte
		{
			Unknown,
			TextHint,
			TranslationHint
		}
	}
}
