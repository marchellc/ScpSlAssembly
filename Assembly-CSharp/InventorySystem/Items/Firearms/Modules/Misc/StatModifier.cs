using System;

namespace InventorySystem.Items.Firearms.Modules.Misc;

[Serializable]
public class StatModifier
{
	public enum ModifierMode
	{
		Inactive,
		Add,
		Multiply,
		Override
	}

	public ModifierMode Mode;

	public float Modifier;

	public float Process(float baseValue)
	{
		return this.Mode switch
		{
			ModifierMode.Inactive => baseValue, 
			ModifierMode.Add => baseValue + this.Modifier, 
			ModifierMode.Multiply => baseValue * this.Modifier, 
			ModifierMode.Override => this.Modifier, 
			_ => throw new NotImplementedException("Unhadled stat mixing mode: " + this.Mode), 
		};
	}
}
