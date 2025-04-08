using System;

namespace Christmas.Scp2536
{
	public struct Scp2536Reward
	{
		public Scp2536Reward(ItemType reward, float weight)
		{
			this.Reward = reward;
			this.Weight = weight;
		}

		public readonly ItemType Reward;

		public readonly float Weight;
	}
}
