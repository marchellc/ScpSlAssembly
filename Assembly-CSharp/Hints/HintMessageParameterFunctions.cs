using Mirror;
using UnityEngine;

namespace Hints;

public static class HintMessageParameterFunctions
{
	private enum HintMessageTypes : byte
	{
		Unknown,
		TextHint,
		TranslationHint
	}

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
		Hint content;
		switch ((HintMessageTypes)reader.ReadByte())
		{
		case HintMessageTypes.Unknown:
			Debug.LogError("Unknown type of HintMessage has been received!");
			return default(HintMessage);
		case HintMessageTypes.TextHint:
			content = TextHint.FromNetwork(reader);
			break;
		case HintMessageTypes.TranslationHint:
			content = TranslationHint.FromNetwork(reader);
			break;
		default:
			Debug.LogError("Invalid type of HintMessage has been received!");
			return default(HintMessage);
		}
		return new HintMessage(content);
	}
}
