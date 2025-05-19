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
		return Mode switch
		{
			ModifierMode.Inactive => baseValue, 
			ModifierMode.Add => baseValue + Modifier, 
			ModifierMode.Multiply => baseValue * Modifier, 
			ModifierMode.Override => Modifier, 
			_ => throw new NotImplementedException("Unhadled stat mixing mode: " + Mode), 
		};
	}
}
