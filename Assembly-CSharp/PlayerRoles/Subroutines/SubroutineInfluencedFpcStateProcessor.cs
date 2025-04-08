using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.Subroutines
{
	public class SubroutineInfluencedFpcStateProcessor : FpcStateProcessor
	{
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
				IFpcRole fpcRole = currentRole as IFpcRole;
				if (fpcRole != null)
				{
					ISubroutinedRole subroutinedRole = currentRole as ISubroutinedRole;
					if (subroutinedRole != null)
					{
						SubroutineBase[] allSubroutines = subroutinedRole.SubroutineModule.AllSubroutines;
						for (int i = 0; i < allSubroutines.Length; i++)
						{
							IStaminaModifier staminaModifier = allSubroutines[i] as IStaminaModifier;
							if (staminaModifier != null)
							{
								this._modifiers.Add(staminaModifier);
							}
						}
						IStaminaModifier staminaModifier2 = fpcRole.FpcModule as IStaminaModifier;
						if (staminaModifier2 != null)
						{
							this._modifiers.Add(staminaModifier2);
						}
						return this._modifiers;
					}
				}
				Debug.LogError("Attempting to create " + base.GetType().Name + " for an invalid role.");
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
				return base.SprintingDisabled || this.Modifiers.Any(new Func<IStaminaModifier, bool>(SubroutineInfluencedFpcStateProcessor.<get_SprintingDisabled>g__IsDisabled|8_0));
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

		[CompilerGenerated]
		internal static bool <get_SprintingDisabled>g__IsDisabled|8_0(IStaminaModifier fx)
		{
			return fx.StaminaModifierActive && fx.SprintingDisabled;
		}

		private List<IStaminaModifier> _modifiers;
	}
}
