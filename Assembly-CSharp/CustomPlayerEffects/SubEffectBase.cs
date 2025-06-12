using UnityEngine;

namespace CustomPlayerEffects;

public abstract class SubEffectBase : MonoBehaviour
{
	protected StatusEffectBase MainEffect { get; private set; }

	protected ReferenceHub Hub => this.MainEffect.Hub;

	protected bool IsLocalPlayer => this.MainEffect.IsLocalPlayer;

	public virtual bool IsActive
	{
		get
		{
			if (base.gameObject.activeInHierarchy)
			{
				return this.MainEffect.IsEnabled;
			}
			return false;
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
