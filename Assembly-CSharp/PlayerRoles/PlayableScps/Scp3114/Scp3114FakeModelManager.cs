using System;
using System.Collections.Generic;
using CameraShaking;
using GameObjectPools;
using InventorySystem.Items;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp3114;

public class Scp3114FakeModelManager : StandardSubroutine<Scp3114Role>, ISubcontrollerRpcRedirector
{
	[Serializable]
	private class MaterialAnimation
	{
		public float ProgressSpeed;

		public AnimationCurve FadeOverProgress;
	}

	private struct MappedBone
	{
		public Transform Original;

		public Transform Tracked;
	}

	[Serializable]
	public class MaterialPair
	{
		public Material Original;

		public Material Disguise;

		public Material Reveal;

		public Material FromType(VariantType type)
		{
			return type switch
			{
				VariantType.Original => this.Original, 
				VariantType.Disguise => this.Disguise, 
				VariantType.Reveal => this.Reveal, 
				_ => throw new NotImplementedException("Unknown material type"), 
			};
		}
	}

	public enum VariantType
	{
		Original,
		Disguise,
		Reveal
	}

	[SerializeField]
	private MaterialAnimation _disguiseAnimation;

	[SerializeField]
	private MaterialAnimation _revealAnimation;

	[SerializeField]
	private MaterialPair[] _materialPairs;

	[SerializeField]
	private AnimationCurve _boneTrackingOverDisguiseProgress;

	[SerializeField]
	private float _teammateProgressMultiplier;

	private static Dictionary<Material, MaterialPair> _dictionarizedPairs;

	private Scp3114Identity.DisguiseStatus _lastActiveStatus;

	private VariantType _lastMaterialType;

	private float _animProgress;

	private bool _trackBones;

	private AnimatedCharacterModel _lastModel;

	private Scp3114Model _ownModel;

	private bool _wasTeammatePerpsective;

	private static readonly int ProgressHash = Shader.PropertyToID("_Progress");

	private readonly Dictionary<Material, Material> _materialInstances = new Dictionary<Material, Material>();

	private readonly Dictionary<GameObject, AnimatedCharacterModel> _modelInstances = new Dictionary<GameObject, AnimatedCharacterModel>();

	private readonly Dictionary<AnimatedCharacterModel, List<MappedBone>> _mappedBones = new Dictionary<AnimatedCharacterModel, List<MappedBone>>();

	private readonly List<Material> _materialsInUse = new List<Material>();

	private float AnimProgress
	{
		get
		{
			return this._animProgress;
		}
		set
		{
			float num = Mathf.Clamp01(value);
			if (num != this._animProgress)
			{
				this._animProgress = num;
				this.UpdateModelMaterials();
			}
		}
	}

	private bool TeammatePerspective
	{
		get
		{
			if (ReferenceHub.TryGetPovHub(out var hub))
			{
				return hub.IsSCP();
			}
			return false;
		}
	}

	public AnimatedCharacterModel RpcTarget
	{
		get
		{
			if (!(this._lastModel != null))
			{
				return this._ownModel;
			}
			return this._lastModel;
		}
	}

	public void OnPlayerMove()
	{
		if (this._lastModel != null)
		{
			this._lastModel.OnPlayerMove();
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		base.CastRole.CurIdentity.OnStatusChanged += OnIdentityChanged;
		this._ownModel = base.CastRole.FpcModule.CharacterModelInstance as Scp3114Model;
		this._ownModel.OnVisibilityChanged += UpdateVisibilityAll;
		this.UpdateModelMaterials();
	}

	public override void ResetObject()
	{
		base.ResetObject();
		if (this._ownModel != null)
		{
			this._ownModel.OnVisibilityChanged -= UpdateVisibilityAll;
		}
		base.CastRole.CurIdentity.OnStatusChanged -= OnIdentityChanged;
		this._lastModel = null;
		this._animProgress = 0f;
		this._trackBones = false;
		this._lastActiveStatus = Scp3114Identity.DisguiseStatus.None;
		this._lastMaterialType = VariantType.Original;
		this._modelInstances.ForEachValue(delegate(AnimatedCharacterModel x)
		{
			x.gameObject.SetActive(value: false);
		});
		this._materialsInUse.Clear();
		this._modelInstances.Clear();
		this._mappedBones.Clear();
	}

	private void UpdateModelMaterials()
	{
		CharacterModel characterModelInstance = base.CastRole.FpcModule.CharacterModelInstance;
		float progress = this.AnimProgress;
		switch (this._lastMaterialType)
		{
		case VariantType.Reveal:
			characterModelInstance.Fade = this._revealAnimation.FadeOverProgress.Evaluate(progress);
			break;
		case VariantType.Original:
		case VariantType.Disguise:
			if (this.TeammatePerspective)
			{
				progress *= this._teammateProgressMultiplier;
			}
			characterModelInstance.Fade = this._disguiseAnimation.FadeOverProgress.Evaluate(progress);
			break;
		}
		this._materialsInUse.ForEach(delegate(Material x)
		{
			x.SetFloat(Scp3114FakeModelManager.ProgressHash, progress);
		});
	}

	private void OnIdentityChanged()
	{
		Scp3114Identity.StolenIdentity curIdentity = base.CastRole.CurIdentity;
		switch (curIdentity.Status)
		{
		case Scp3114Identity.DisguiseStatus.Active:
			this.TrySetModel(curIdentity.StolenRole, VariantType.Original);
			this.AnimProgress = 1f;
			this._trackBones = this.TeammatePerspective;
			this._lastActiveStatus = Scp3114Identity.DisguiseStatus.Active;
			break;
		case Scp3114Identity.DisguiseStatus.Equipping:
			this.TrySetModel(curIdentity.StolenRole, VariantType.Disguise);
			this.AnimProgress = 0f;
			this._trackBones = true;
			this._lastActiveStatus = Scp3114Identity.DisguiseStatus.Equipping;
			break;
		case Scp3114Identity.DisguiseStatus.None:
			if (this._lastActiveStatus == Scp3114Identity.DisguiseStatus.Active)
			{
				this.TrySetModel(curIdentity.StolenRole, VariantType.Reveal);
				this._trackBones = true;
				this.AnimProgress = 0f;
			}
			break;
		}
		this._wasTeammatePerpsective = this.TeammatePerspective;
	}

	private void UpdatePerspective()
	{
		if (this.TeammatePerspective != this._wasTeammatePerpsective)
		{
			float animProgress = this.AnimProgress;
			this.OnIdentityChanged();
			if (animProgress == this.AnimProgress)
			{
				this.UpdateModelMaterials();
			}
			else
			{
				this.AnimProgress = animProgress;
			}
			this._wasTeammatePerpsective = this.TeammatePerspective;
		}
	}

	private void UpdateVisibilityAll()
	{
		this._modelInstances.ForEachValue(delegate(AnimatedCharacterModel x)
		{
			x.SetVisibility(this._ownModel.IsVisible);
		});
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		if (this._trackBones)
		{
			this._trackBones &= this.TryTrackBones();
		}
		this.UpdatePerspective();
		switch (base.CastRole.CurIdentity.Status)
		{
		case Scp3114Identity.DisguiseStatus.Equipping:
			this.AnimProgress += Time.deltaTime * this._disguiseAnimation.ProgressSpeed;
			break;
		case Scp3114Identity.DisguiseStatus.None:
			if (this._lastActiveStatus == Scp3114Identity.DisguiseStatus.Active && this.AnimProgress < 1f)
			{
				this.AnimProgress += Time.deltaTime * this._revealAnimation.ProgressSpeed;
				if (this.AnimProgress == 1f)
				{
					this.TrySetModel(RoleTypeId.None, VariantType.Original);
				}
			}
			else if (this._lastActiveStatus == Scp3114Identity.DisguiseStatus.Equipping && this.AnimProgress > 0f)
			{
				this.AnimProgress -= Time.deltaTime * this._disguiseAnimation.ProgressSpeed;
				if (this.AnimProgress == 0f)
				{
					this.TrySetModel(RoleTypeId.None, VariantType.Original);
				}
			}
			break;
		}
	}

	private bool TryTrackBones()
	{
		if (this._lastModel == null || !this._lastModel.gameObject.activeSelf)
		{
			return false;
		}
		if (!this._mappedBones.TryGetValue(this._lastModel, out var value))
		{
			return false;
		}
		bool flag = base.CastRole.CurIdentity.Status == Scp3114Identity.DisguiseStatus.Equipping;
		float t = (flag ? this._boneTrackingOverDisguiseProgress.Evaluate(this.AnimProgress) : 1f);
		foreach (MappedBone item in value)
		{
			item.Original.GetPositionAndRotation(out var position, out var rotation);
			if (flag)
			{
				item.Tracked.GetPositionAndRotation(out var position2, out var rotation2);
				item.Tracked.SetPositionAndRotation(Vector3.Lerp(position2, position, t), Quaternion.Lerp(rotation2, rotation, t));
			}
			else
			{
				item.Tracked.SetPositionAndRotation(position, rotation);
			}
		}
		if (this._lastModel.TryGetSubcontroller<SecondaryRigsSubcontroller>(out var subcontroller))
		{
			subcontroller.MatchAll();
		}
		return true;
	}

	private bool TrySetModel(RoleTypeId role, VariantType variant)
	{
		if (!PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(role, out var result))
		{
			if (this._lastModel != null)
			{
				this._lastModel.gameObject.SetActive(value: false);
			}
			return false;
		}
		GameObject characterModelTemplate = result.FpcModule.CharacterModelTemplate;
		if (!this._modelInstances.TryGetValue(characterModelTemplate, out var value))
		{
			if (!this.TryCreateModel(characterModelTemplate, out value))
			{
				return false;
			}
			this._modelInstances.Add(characterModelTemplate, value);
		}
		else if (this._lastModel != value && this._lastModel != null)
		{
			this._lastModel.gameObject.SetActive(value: false);
		}
		this._lastModel = value;
		characterModelTemplate.transform.GetPositionAndRotation(out var position, out var rotation);
		value.gameObject.SetActive(value: true);
		value.RestoreOriginalMaterials();
		value.Setup(base.Owner, base.CastRole, position, rotation);
		value.SetVisibility(this._ownModel.IsVisible);
		if (this._ownModel.IsTracked)
		{
			SharedHandsController.SetRoleGloves(role);
			CameraShakeController.AddEffect(new HeadbobShake(this._ownModel));
		}
		this.SetModelMaterials(value, variant);
		this._lastMaterialType = variant;
		return true;
	}

	private bool TryCreateModel(GameObject template, out AnimatedCharacterModel model)
	{
		model = null;
		if (!PoolManager.Singleton.TryGetPoolObject(template, base.transform, out var poolObject))
		{
			return false;
		}
		if (!(poolObject is AnimatedCharacterModel animatedCharacterModel))
		{
			return false;
		}
		model = animatedCharacterModel;
		Transform transform = template.transform;
		Transform obj = model.transform;
		transform.GetPositionAndRotation(out var position, out var rotation);
		obj.SetLocalPositionAndRotation(position, rotation);
		obj.localScale = transform.localScale;
		model.SetVisibility(this._ownModel.IsVisible);
		model.SetOriginalMaterials();
		List<MappedBone> orAddNew = this._mappedBones.GetOrAddNew(model);
		Scp3114Model.HumanoidBone[] humanoidBones = this._ownModel.HumanoidBones;
		for (int i = 0; i < humanoidBones.Length; i++)
		{
			Scp3114Model.HumanoidBone humanoidBone = humanoidBones[i];
			Transform boneTransform = model.Animator.GetBoneTransform(humanoidBone.BoneType);
			if (!(boneTransform == null))
			{
				orAddNew.Add(new MappedBone
				{
					Original = humanoidBone.Transform,
					Tracked = boneTransform
				});
			}
		}
		return true;
	}

	private void SetModelMaterials(AnimatedCharacterModel model, VariantType matType)
	{
		model.RestoreOriginalMaterials();
		this._materialsInUse.Clear();
		if (matType == VariantType.Original)
		{
			if (!this.TeammatePerspective)
			{
				return;
			}
			matType = VariantType.Disguise;
		}
		model.ReplaceOriginalMaterials(delegate(Material original)
		{
			Material template = Scp3114FakeModelManager.GetVariant(original, matType);
			Material orAdd = this._materialInstances.GetOrAdd(template, () => new Material(template));
			this._materialsInUse.Add(orAdd);
			return orAdd;
		});
	}

	public static Material GetVariant(Material original, VariantType matType)
	{
		if (Scp3114FakeModelManager._dictionarizedPairs == null)
		{
			if (!PlayerRoleLoader.TryGetRoleTemplate<Scp3114Role>(RoleTypeId.Scp3114, out var result))
			{
				return original;
			}
			if (!result.SubroutineModule.TryGetSubroutine<Scp3114FakeModelManager>(out var subroutine))
			{
				return original;
			}
			Scp3114FakeModelManager._dictionarizedPairs = new Dictionary<Material, MaterialPair>();
			Scp3114FakeModelManager._dictionarizedPairs.FromArray(subroutine._materialPairs, (MaterialPair x) => x.Original);
		}
		if (!Scp3114FakeModelManager._dictionarizedPairs.TryGetValue(original, out var value))
		{
			return original;
		}
		return value.FromType(matType);
	}
}
