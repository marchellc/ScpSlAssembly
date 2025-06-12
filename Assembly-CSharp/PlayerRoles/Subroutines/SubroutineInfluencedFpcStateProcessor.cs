using System.Collections.Generic;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.Subroutines;

public class SubroutineInfluencedFpcStateProcessor : FpcStateProcessor
{
	private List<IStaminaModifier> _modifiers;

	private List<IStaminaModifier> Modifiers
	{
		get
		{
			if (this._modifiers != null)
			{
				return this._modifiers;
			}
			this._modifiers = new List<IStaminaModifier>();
			PlayerRoleBase currentRole = base.Hub.roleManager.CurrentRole;
			if (!(currentRole is IFpcRole fpcRole) || !(currentRole is ISubroutinedRole subroutinedRole))
			{
				Debug.LogError("Attempting to create " + base.GetType().Name + " for an invalid role.");
				return this._modifiers;
			}
			SubroutineBase[] allSubroutines = subroutinedRole.SubroutineModule.AllSubroutines;
			for (int i = 0; i < allSubroutines.Length; i++)
			{
				if (allSubroutines[i] is IStaminaModifier item)
				{
					this._modifiers.Add(item);
				}
			}
			if (fpcRole.FpcModule is IStaminaModifier item2)
			{
				this._modifiers.Add(item2);
			}
			return this._modifiers;
		}
	}

	protected override float ServerUseRate
	{
		get
		{
			float num = base.ServerUseRate;
			for (int i = 0; i < this.Modifiers.Count; i++)
			{
				IStaminaModifier staminaModifier = this.Modifiers[i];
				if (staminaModifier.StaminaModifierActive)
				{
					num *= staminaModifier.StaminaUsageMultiplier;
				}
			}
			return num;
		}
	}

	protected override float ServerRegenRate
	{
		get
		{
			float num = base.ServerRegenRate;
			for (int i = 0; i < this.Modifiers.Count; i++)
			{
				IStaminaModifier staminaModifier = this.Modifiers[i];
				if (staminaModifier.StaminaModifierActive)
				{
					num *= staminaModifier.StaminaRegenMultiplier;
				}
			}
			return num;
		}
	}

	protected override bool SprintingDisabled
	{
		get
		{
			if (base.SprintingDisabled)
			{
				return true;
			}
			return this.Modifiers.Any(IsDisabled);
			static bool IsDisabled(IStaminaModifier fx)
			{
				if (fx.StaminaModifierActive)
				{
					return fx.SprintingDisabled;
				}
				return false;
			}
		}
	}

	public SubroutineInfluencedFpcStateProcessor(ReferenceHub hub, FirstPersonMovementModule module)
		: base(hub, module)
	{
	}

	public SubroutineInfluencedFpcStateProcessor(ReferenceHub hub, FirstPersonMovementModule module, float useRate, float spawnImmunity, float regenCooldown, float regenSpeed, float rampupTime)
		: base(hub, module, useRate, spawnImmunity, regenCooldown, regenSpeed, rampupTime)
	{
	}

	public SubroutineInfluencedFpcStateProcessor(ReferenceHub hub, FirstPersonMovementModule module, float useRate, float spawnImmunity, AnimationCurve regenCurve)
		: base(hub, module, useRate, spawnImmunity, regenCurve)
	{
	}
}
