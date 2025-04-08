using System;

namespace InventorySystem.Items.Firearms.Modules
{
	public interface ISwayModifierModule
	{
		float WalkSwayScale
		{
			get
			{
				return 1f;
			}
		}

		float JumpSwayScale
		{
			get
			{
				return 1f;
			}
		}

		float BobbingSwayScale
		{
			get
			{
				return 1f;
			}
		}
	}
}
