using System;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	[Serializable]
	public class StatModifier
	{
		public float Process(float baseValue)
		{
			switch (this.Mode)
			{
			case StatModifier.ModifierMode.Inactive:
				return baseValue;
			case StatModifier.ModifierMode.Add:
				return baseValue + this.Modifier;
			case StatModifier.ModifierMode.Multiply:
				return baseValue * this.Modifier;
			case StatModifier.ModifierMode.Override:
				return this.Modifier;
			default:
				throw new NotImplementedException("Unhadled stat mixing mode: " + this.Mode.ToString());
			}
		}

		public StatModifier.ModifierMode Mode;

		public float Modifier;

		public enum ModifierMode
		{
			Inactive,
			Add,
			Multiply,
			Override
		}
	}
}
