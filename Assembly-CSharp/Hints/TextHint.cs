using System;
using Mirror;

namespace Hints
{
	public class TextHint : FormattableHint<TextHint>
	{
		public static TextHint FromNetwork(NetworkReader reader)
		{
			TextHint textHint = new TextHint();
			textHint.Deserialize(reader);
			return textHint;
		}

		private protected string Text { protected get; private set; }

		private TextHint()
			: base(null, null, 0f)
		{
		}

		public TextHint(string text, HintParameter[] parameters = null, HintEffect[] effects = null, float durationScalar = 3f)
			: base(parameters, effects, durationScalar)
		{
			this.Text = text;
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			this.Text = reader.ReadString();
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			writer.WriteString(this.Text);
		}
	}
}
