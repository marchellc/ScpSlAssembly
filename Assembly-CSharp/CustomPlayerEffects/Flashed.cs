using System;
using Mirror;

namespace CustomPlayerEffects
{
	public class Flashed : StatusEffectBase
	{
		public override bool AllowEnabling
		{
			get
			{
				return !SpawnProtected.CheckPlayer(base.Hub);
			}
		}

		protected override void IntensityChanged(byte prevState, byte newState)
		{
			float num = (float)newState * 0.1f;
			if (NetworkServer.active)
			{
				base.TimeLeft = num;
			}
		}

		protected override void Update()
		{
			base.Update();
			if (NetworkServer.active && base.Duration == 0f)
			{
				base.TimeLeft = 1f;
			}
		}
	}
}
