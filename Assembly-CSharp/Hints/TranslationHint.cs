using System;
using Mirror;

namespace Hints
{
	public class TranslationHint : FormattableHint<TranslationHint>
	{
		public static TranslationHint FromNetwork(NetworkReader reader)
		{
			TranslationHint translationHint = new TranslationHint();
			translationHint.Deserialize(reader);
			return translationHint;
		}

		private protected HintTranslations Translation { protected get; private set; }

		private TranslationHint()
			: base(null, null, 0f)
		{
		}

		public TranslationHint(HintTranslations translation, HintParameter[] parameters = null, HintEffect[] effects = null, float durationScalar = 3f)
			: base(parameters, effects, durationScalar)
		{
			this.Translation = translation;
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			this.Translation = (HintTranslations)reader.ReadByte();
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			writer.WriteByte((byte)this.Translation);
		}

		public const string TranslationFile = "GameHints";
	}
}
