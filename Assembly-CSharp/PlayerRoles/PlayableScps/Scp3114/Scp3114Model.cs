using System;
using AnimatorLayerManagement;
using PlayerRoles.FirstPersonControl;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114Model : AnimatedCharacterModel, ISubcontrollerRpcRedirector
{
	[Serializable]
	public struct HumanoidBone
	{
		public HumanBodyBones BoneType;

		public Transform Transform;
	}

	private static readonly int HashStrangling = Animator.StringToHash("Strangling");

	private static readonly int HashDanceVariant = Animator.StringToHash("DanceVariant");

	private static readonly int HashSlapTrigger = Animator.StringToHash("SlapTrigger");

	private static readonly int HashSlapMirror = Animator.StringToHash("MirrorSlap");

	private static readonly int HashStealing = Animator.StringToHash("Stealing");

	private static readonly int HashReveal = Animator.StringToHash("Reveal");

	private Scp3114Strangle _strangle;

	private Scp3114Slap _slap;

	private Scp3114Dance _dance;

	private Scp3114FakeModelManager _fakeModel;

	private bool _hadIdentity;

	[SerializeField]
	private LayerRefId _strangleLayer;

	[SerializeField]
	private LayerRefId _danceLayer;

	[SerializeField]
	private float _layerWeightAdjustSpeed;

	[SerializeField]
	private ParticleSystem _revealParticles;

	[SerializeField]
	private GameObject _skeletonFormItemRoot;

	[field: SerializeField]
	public HumanoidBone[] HumanoidBones { get; private set; }

	public override bool FootstepPlayable
	{
		get
		{
			if (base.FootstepPlayable)
			{
				return this.ScpRole.SkeletonIdle;
			}
			return false;
		}
	}

	public override bool LandingFootstepPlayable
	{
		get
		{
			if (base.LandingFootstepPlayable)
			{
				return this.ScpRole.SkeletonIdle;
			}
			return false;
		}
	}

	public AnimatedCharacterModel RpcTarget => this._fakeModel.RpcTarget;

	private Scp3114Role ScpRole => base.Role as Scp3114Role;

	public override void Setup(ReferenceHub owner, IFpcRole role, Vector3 localPos, Quaternion localRot)
	{
		base.Setup(owner, role, localPos, localRot);
		this.ScpRole.SubroutineModule.TryGetSubroutine<Scp3114FakeModelManager>(out this._fakeModel);
		this.ScpRole.SubroutineModule.TryGetSubroutine<Scp3114Strangle>(out this._strangle);
		this.ScpRole.SubroutineModule.TryGetSubroutine<Scp3114Dance>(out this._dance);
		this.ScpRole.SubroutineModule.TryGetSubroutine<Scp3114Slap>(out this._slap);
		this._slap.OnTriggered += OnSlapTriggered;
		this.ScpRole.CurIdentity.OnStatusChanged += OnIdentityChanged;
	}

	public override void OnPlayerMove()
	{
		base.OnPlayerMove();
		this._fakeModel.OnPlayerMove();
	}

	public override void ResetObject()
	{
		base.ResetObject();
		this._hadIdentity = false;
		this._slap.OnTriggered -= OnSlapTriggered;
		this.ScpRole.CurIdentity.OnStatusChanged -= OnIdentityChanged;
	}

	protected override void Update()
	{
		base.Update();
		if (!base.Pooled)
		{
			bool hasValue = this._strangle.SyncTarget.HasValue;
			base.Animator.SetBool(Scp3114Model.HashStrangling, hasValue);
			base.Animator.SetFloat(Scp3114Model.HashDanceVariant, this._dance.DanceVariant);
			base.Animator.SetBool(Scp3114Model.HashStealing, this.ScpRole.CurIdentity.Status == Scp3114Identity.DisguiseStatus.Equipping);
			this.AdjustWeight(this._strangleLayer, hasValue);
			this.AdjustWeight(this._danceLayer, this._dance.IsDancing);
		}
	}

	private void AdjustWeight(LayerRefId layerId, bool isActive)
	{
		int layerIndex = base.LayerManager.GetLayerIndex(layerId);
		float layerWeight = base.Animator.GetLayerWeight(layerIndex);
		float num = Time.deltaTime * this._layerWeightAdjustSpeed;
		float value = (isActive ? (layerWeight + num) : (layerWeight - num));
		base.Animator.SetLayerWeight(layerIndex, Mathf.Clamp01(value));
	}

	private void OnIdentityChanged()
	{
		this._skeletonFormItemRoot.SetActive(this.ScpRole.SkeletonIdle);
		switch (this.ScpRole.CurIdentity.Status)
		{
		case Scp3114Identity.DisguiseStatus.Active:
			this._hadIdentity = true;
			break;
		case Scp3114Identity.DisguiseStatus.None:
			if (this._hadIdentity)
			{
				this._hadIdentity = false;
				this._revealParticles.Play(withChildren: true);
				base.Animator.SetTrigger(Scp3114Model.HashReveal);
			}
			break;
		}
	}

	private void OnSlapTriggered()
	{
		base.Animator.SetBool(Scp3114Model.HashSlapMirror, !base.Animator.GetBool(Scp3114Model.HashSlapMirror));
		base.Animator.SetTrigger(Scp3114Model.HashSlapTrigger);
	}
}
