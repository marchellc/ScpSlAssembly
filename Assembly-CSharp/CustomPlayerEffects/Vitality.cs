using System;

namespace CustomPlayerEffects
{
	public class Vitality : StatusEffectBase, ISpectatorDataPlayerEffect
	{
		public override StatusEffectBase.EffectClassification Classification
		{
			get
			{
				return StatusEffectBase.EffectClassification.Positive;
			}
		}

		public bool GetSpectatorText(out string s)
		{
			s = "Vitality";
			return true;
		}

		public static bool CheckPlayer(ReferenceHub ply)
		{
			return ply != null && ply.playerEffectsController.GetEffect<Vitality>().IsEnabled;
		}
	}
}
