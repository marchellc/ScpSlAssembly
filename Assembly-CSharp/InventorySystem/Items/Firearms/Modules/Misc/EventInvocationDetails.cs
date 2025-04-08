using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	public readonly struct EventInvocationDetails
	{
		public float TotalSpeedMultiplier
		{
			get
			{
				return this.StateSpeed * this.ParamSpeed;
			}
		}

		public EventInvocationDetails(AnimatorStateInfo stateInfo, Animator sourceAnimator, int callerLayer)
		{
			this.EverInvoked = true;
			this.StateSpeed = stateInfo.speed;
			this.ParamSpeed = stateInfo.speedMultiplier;
			this.Layer = callerLayer;
			this.RawAnimator = sourceAnimator;
		}

		public EventInvocationDetails(bool everInvoked, float stateSpeed, float paramSpeed, int layer, Animator rawAnimator)
		{
			this.EverInvoked = everInvoked;
			this.StateSpeed = stateSpeed;
			this.ParamSpeed = paramSpeed;
			this.Layer = layer;
			this.RawAnimator = rawAnimator;
		}

		public readonly bool EverInvoked;

		public readonly float StateSpeed;

		public readonly float ParamSpeed;

		public readonly int Layer;

		public readonly Animator RawAnimator;
	}
}
