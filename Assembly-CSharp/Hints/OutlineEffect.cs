using System;
using Mirror;
using UnityEngine;

namespace Hints
{
	public class OutlineEffect : HintEffect
	{
		public static OutlineEffect FromNetwork(NetworkReader reader)
		{
			OutlineEffect outlineEffect = new OutlineEffect();
			outlineEffect.Deserialize(reader);
			return outlineEffect;
		}

		private protected Color32 OutlineColor { protected get; private set; }

		private protected float OutlineWidth { protected get; private set; }

		private OutlineEffect()
			: base(0f, 1f)
		{
		}

		public OutlineEffect(Color32 outlineColor, float outlineWidth, float startScalar = 0f, float durationScalar = 1f)
			: base(startScalar, durationScalar)
		{
			this.OutlineColor = outlineColor;
			this.OutlineWidth = outlineWidth;
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			this.OutlineColor = reader.ReadColor32();
			this.OutlineWidth = reader.ReadFloat();
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			writer.WriteColor32(this.OutlineColor);
			writer.WriteFloat(this.OutlineWidth);
		}
	}
}
