using GameObjectPools;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Thirdperson;

public abstract class ThirdpersonItemBase : PoolObject, IPoolResettable, IIdentifierProvider
{
	private static readonly int HashOverrideBlend = Animator.StringToHash("ItemOverrideBlend");

	private static readonly int HashTriggerOverrideInstant = Animator.StringToHash("ItemOverrideTriggerInstant");

	private static readonly int HashTriggerOverrideSoft = Animator.StringToHash("ItemOverrideTriggerSoft");

	private static readonly int HashAdditiveBlend = Animator.StringToHash("ItemAdditiveBlend");

	private static readonly int HashTriggerAdditiveInstant = Animator.StringToHash("ItemAdditiveTriggerInstant");

	private static readonly int HashTriggerAdditiveSoft = Animator.StringToHash("ItemAdditiveTriggerSoft");

	private Transform _tr;

	protected float OverrideBlend
	{
		get
		{
			return this.Animator.GetFloat(ThirdpersonItemBase.HashOverrideBlend);
		}
		set
		{
			this.Animator.SetFloat(ThirdpersonItemBase.HashOverrideBlend, value);
		}
	}

	protected float AdditiveBlend
	{
		get
		{
			return this.Animator.GetFloat(ThirdpersonItemBase.HashAdditiveBlend);
		}
		set
		{
			this.Animator.SetFloat(ThirdpersonItemBase.HashAdditiveBlend, value);
		}
	}

	public ItemIdentifier ItemId { get; private set; }

	public AnimatedCharacterModel TargetModel { get; private set; }

	public InventorySubcontroller TargetSubcontroller { get; private set; }

	public Animator Animator => this.TargetModel.Animator;

	public ReferenceHub OwnerHub => this.TargetModel.OwnerHub;

	public virtual void ResetObject()
	{
	}

	public virtual float GetTransitionTime(ItemIdentifier iid)
	{
		return 0.5f;
	}

	public abstract ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer);

	internal virtual void OnFadeChanged(float newFade)
	{
		this._tr.localScale = Vector3.one * newFade;
	}

	internal virtual void OnAnimIK(int layerIndex, float ikScale)
	{
	}

	internal virtual void Initialize(InventorySubcontroller subcontroller, ItemIdentifier id)
	{
		this.ItemId = id;
		this.TargetModel = subcontroller.Model;
		this.TargetSubcontroller = subcontroller;
		this._tr = base.transform;
		this._tr.parent = subcontroller.ItemSpawnpoint;
		this._tr.ResetTransform();
		this.OverrideBlend = 0f;
		this.AdditiveBlend = 0f;
		this.OnFadeChanged(subcontroller.Model.Fade);
	}

	protected void SetAnim(AnimState3p anim, AnimationClip clip)
	{
		ThirdpersonItemAnimationManager.SetAnimation(this.TargetModel, anim, clip);
	}

	protected void SetAnim(AnimOverrideState3pPair pair)
	{
		ThirdpersonItemAnimationManager.SetAnimation(this.TargetModel, pair);
	}

	protected void ReplayOverrideBlend(bool soft)
	{
		this.Animator.SetTrigger(soft ? ThirdpersonItemBase.HashTriggerOverrideSoft : ThirdpersonItemBase.HashTriggerOverrideInstant);
	}

	protected void ReplayAdditiveBlend(bool soft)
	{
		this.Animator.SetTrigger(soft ? ThirdpersonItemBase.HashTriggerAdditiveSoft : ThirdpersonItemBase.HashTriggerAdditiveInstant);
	}

	protected virtual void Update()
	{
	}
}
