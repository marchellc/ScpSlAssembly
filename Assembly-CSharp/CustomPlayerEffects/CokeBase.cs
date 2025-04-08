using System;
using System.Collections.Generic;
using Mirror;
using PlayerRoles.FirstPersonControl;
using UnityEngine;
using Utils;

namespace CustomPlayerEffects
{
	public abstract class CokeBase<TStack> : CokeBase, IHealableEffect, IMovementSpeedModifier, IConflictableEffect where TStack : ICokeStack
	{
		public abstract Dictionary<PlayerMovementState, float> StateMultipliers { get; }

		public abstract float MovementSpeedMultiplier { get; }

		public bool MovementModifierActive
		{
			get
			{
				return base.IsEnabled;
			}
		}

		public float MovementSpeedLimit
		{
			get
			{
				return float.MaxValue;
			}
		}

		private protected TStack[] StackMultipliers { protected get; private set; }

		private protected TStack CurrentStack { protected get; private set; }

		private protected bool GoingToExplode
		{
			protected get
			{
				return this._goingToExplode;
			}
			private set
			{
				this._goingToExplode = value;
				this._explosionTimer = 0f;
			}
		}

		public bool IsHealable(ItemType it)
		{
			return it == ItemType.SCP500;
		}

		protected float GetMovementStateMultiplier()
		{
			IFpcRole fpcRole = base.Hub.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				return 1f;
			}
			PlayerMovementState playerMovementState = fpcRole.FpcModule.CurrentMovementState;
			float num = base.Hub.GetVelocity().SqrMagnitudeIgnoreY();
			if (playerMovementState == PlayerMovementState.Walking && num == 0f)
			{
				playerMovementState = PlayerMovementState.Crouching;
			}
			float num2;
			if (!this.StateMultipliers.TryGetValue(playerMovementState, out num2))
			{
				return 1f;
			}
			return num2;
		}

		protected override void OnAwake()
		{
			this.CurrentStack = this.StackMultipliers[0];
		}

		protected override void IntensityChanged(byte prevState, byte newState)
		{
			this.CurrentStack = this.StackMultipliers[Mathf.Clamp((int)newState, 0, this.StackMultipliers.Length - 1)];
		}

		protected override void Enabled()
		{
			base.Enabled();
			this.GoingToExplode = false;
		}

		protected override void Disabled()
		{
			base.Disabled();
			this.GoingToExplode = false;
		}

		protected override void OnEffectUpdate()
		{
			base.OnEffectUpdate();
			if (!NetworkServer.active)
			{
				return;
			}
			if (!this.GoingToExplode)
			{
				return;
			}
			this._explosionTimer += Time.deltaTime;
			if (this._explosionTimer < 1.05f)
			{
				return;
			}
			ExplosionUtils.ServerExplode(base.Hub, ExplosionType.Cola);
			this.GoingToExplode = false;
		}

		public virtual bool CheckConflicts(StatusEffectBase other)
		{
			if (other is Scp1853)
			{
				Poisoned poisoned;
				if (!base.Hub.playerEffectsController.TryGetEffect<Poisoned>(out poisoned))
				{
					return false;
				}
				if (!poisoned.IsEnabled)
				{
					poisoned.ForceIntensity(1);
				}
				return true;
			}
			else
			{
				CokeBase cokeBase = other as CokeBase;
				if (cokeBase == null)
				{
					return false;
				}
				this.GoingToExplode = true;
				cokeBase.ServerDisable();
				return true;
			}
		}

		private const float ConflictExplosionDelay = 1.05f;

		private bool _goingToExplode;

		private float _explosionTimer;
	}
}
