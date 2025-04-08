using System;
using UnityEngine;

namespace CustomPlayerEffects
{
	public abstract class SubEffectBase : MonoBehaviour
	{
		private protected StatusEffectBase MainEffect { protected get; private set; }

		protected ReferenceHub Hub
		{
			get
			{
				return this.MainEffect.Hub;
			}
		}

		protected bool IsLocalPlayer
		{
			get
			{
				return this.MainEffect.IsLocalPlayer;
			}
		}

		public virtual bool IsActive
		{
			get
			{
				return base.gameObject.activeInHierarchy && this.MainEffect.IsEnabled;
			}
		}

		public virtual void DisableEffect()
		{
		}

		internal virtual void Init(StatusEffectBase mainEffect)
		{
			this.MainEffect = mainEffect;
		}

		internal virtual void UpdateEffect()
		{
		}
	}
}
