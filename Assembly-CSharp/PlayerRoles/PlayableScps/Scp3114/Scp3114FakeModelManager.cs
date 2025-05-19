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
				VariantType.Original => Original, 
				VariantType.Disguise => Disguise, 
				VariantType.Reveal => Reveal, 
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
			return _animProgress;
		}
		set
		{
			float num = Mathf.Clamp01(value);
			if (num != _animProgress)
			{
				_animProgress = num;
				UpdateModelMaterials();
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
			if (!(_lastModel != null))
			{
				return _ownModel;
			}
			return _lastModel;
		}
	}

	public void OnPlayerMove()
	{
		if (_lastModel != null)
		{
			_lastModel.OnPlayerMove();
		}
	}

	public override void SpawnObject()
	{
		base.SpawnObject();
		base.CastRole.CurIdentity.OnStatusChanged += OnIdentityChanged;
		_ownModel = base.CastRole.FpcModule.CharacterModelInstance as Scp3114Model;
		_ownModel.OnVisibilityChanged += UpdateVisibilityAll;
		UpdateModelMaterials();
	}

	public override void ResetObject()
	{
		base.ResetObject();
		if (_ownModel != null)
		{
			_ownModel.OnVisibilityChanged -= UpdateVisibilityAll;
		}
		base.CastRole.CurIdentity.OnStatusChanged -= OnIdentityChanged;
		_lastModel = null;
		_animProgress = 0f;
		_trackBones = false;
		_lastActiveStatus = Scp3114Identity.DisguiseStatus.None;
		_lastMaterialType = VariantType.Original;
		_modelInstances.ForEachValue(delegate(AnimatedCharacterModel x)
		{
			x.gameObject.SetActive(value: false);
		});
		_materialsInUse.Clear();
		_modelInstances.Clear();
		_mappedBones.Clear();
	}

	private void UpdateModelMaterials()
	{
		CharacterModel characterModelInstance = base.CastRole.FpcModule.CharacterModelInstance;
		float progress = AnimProgress;
		switch (_lastMaterialType)
		{
		case VariantType.Reveal:
			characterModelInstance.Fade = _revealAnimation.FadeOverProgress.Evaluate(progress);
			break;
		case VariantType.Original:
		case VariantType.Disguise:
			if (TeammatePerspective)
			{
				progress *= _teammateProgressMultiplier;
			}
			characterModelInstance.Fade = _disguiseAnimation.FadeOverProgress.Evaluate(progress);
			break;
		}
		_materialsInUse.ForEach(delegate(Material x)
		{
			x.SetFloat(ProgressHash, progress);
		});
	}

	private void OnIdentityChanged()
	{
		Scp3114Identity.StolenIdentity curIdentity = base.CastRole.CurIdentity;
		switch (curIdentity.Status)
		{
		case Scp3114Identity.DisguiseStatus.Active:
			TrySetModel(curIdentity.StolenRole, VariantType.Original);
			AnimProgress = 1f;
			_trackBones = TeammatePerspective;
			_lastActiveStatus = Scp3114Identity.DisguiseStatus.Active;
			break;
		case Scp3114Identity.DisguiseStatus.Equipping:
			TrySetModel(curIdentity.StolenRole, VariantType.Disguise);
			AnimProgress = 0f;
			_trackBones = true;
			_lastActiveStatus = Scp3114Identity.DisguiseStatus.Equipping;
			break;
		case Scp3114Identity.DisguiseStatus.None:
			if (_lastActiveStatus == Scp3114Identity.DisguiseStatus.Active)
			{
				TrySetModel(curIdentity.StolenRole, VariantType.Reveal);
				_trackBones = true;
				AnimProgress = 0f;
			}
			break;
		}
		_wasTeammatePerpsective = TeammatePerspective;
	}

	private void UpdatePerspective()
	{
		if (TeammatePerspective != _wasTeammatePerpsective)
		{
			float animProgress = AnimProgress;
			OnIdentityChanged();
			if (animProgress == AnimProgress)
			{
				UpdateModelMaterials();
			}
			else
			{
				AnimProgress = animProgress;
			}
			_wasTeammatePerpsective = TeammatePerspective;
		}
	}

	private void UpdateVisibilityAll()
	{
		_modelInstances.ForEachValue(delegate(AnimatedCharacterModel x)
		{
			x.SetVisibility(_ownModel.IsVisible);
		});
	}

	protected override void LateUpdate()
	{
		base.LateUpdate();
		if (_trackBones)
		{
			_trackBones &= TryTrackBones();
		}
		UpdatePerspective();
		switch (base.CastRole.CurIdentity.Status)
		{
		case Scp3114Identity.DisguiseStatus.Equipping:
			AnimProgress += Time.deltaTime * _disguiseAnimation.ProgressSpeed;
			break;
		case Scp3114Identity.DisguiseStatus.None:
			if (_lastActiveStatus == Scp3114Identity.DisguiseStatus.Active && AnimProgress < 1f)
			{
				AnimProgress += Time.deltaTime * _revealAnimation.ProgressSpeed;
				if (AnimProgress == 1f)
				{
					TrySetModel(RoleTypeId.None, VariantType.Original);
				}
			}
			else if (_lastActiveStatus == Scp3114Identity.DisguiseStatus.Equipping && AnimProgress > 0f)
			{
				AnimProgress -= Time.deltaTime * _disguiseAnimation.ProgressSpeed;
				if (AnimProgress == 0f)
				{
					TrySetModel(RoleTypeId.None, VariantType.Original);
				}
			}
			break;
		}
	}

	private bool TryTrackBones()
	{
		if (_lastModel == null || !_lastModel.gameObject.activeSelf)
		{
			return false;
		}
		if (!_mappedBones.TryGetValue(_lastModel, out var value))
		{
			return false;
		}
		bool flag = base.CastRole.CurIdentity.Status == Scp3114Identity.DisguiseStatus.Equipping;
		float t = (flag ? _boneTrackingOverDisguiseProgress.Evaluate(AnimProgress) : 1f);
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
		if (_lastModel.TryGetSubcontroller<SecondaryRigsSubcontroller>(out var subcontroller))
		{
			subcontroller.MatchAll();
		}
		return true;
	}

	private bool TrySetModel(RoleTypeId role, VariantType variant)
	{
		if (!PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(role, out var result))
		{
			if (_lastModel != null)
			{
				_lastModel.gameObject.SetActive(value: false);
			}
			return false;
		}
		GameObject characterModelTemplate = result.FpcModule.CharacterModelTemplate;
		if (!_modelInstances.TryGetValue(characterModelTemplate, out var value))
		{
			if (!TryCreateModel(characterModelTemplate, out value))
			{
				return false;
			}
			_modelInstances.Add(characterModelTemplate, value);
		}
		else if (_lastModel != value && _lastModel != null)
		{
			_lastModel.gameObject.SetActive(value: false);
		}
		_lastModel = value;
		characterModelTemplate.transform.GetPositionAndRotation(out var position, out var rotation);
		value.gameObject.SetActive(value: true);
		value.RestoreOriginalMaterials();
		value.Setup(base.Owner, base.CastRole, position, rotation);
		value.SetVisibility(_ownModel.IsVisible);
		if (_ownModel.IsTracked)
		{
			SharedHandsController.SetRoleGloves(role);
			CameraShakeController.AddEffect(new HeadbobShake(_ownModel));
		}
		SetModelMaterials(value, variant);
		_lastMaterialType = variant;
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
		model.SetVisibility(_ownModel.IsVisible);
		model.SetOriginalMaterials();
		List<MappedBone> orAddNew = _mappedBones.GetOrAddNew(model);
		Scp3114Model.HumanoidBone[] humanoidBones = _ownModel.HumanoidBones;
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
		_materialsInUse.Clear();
		if (matType == VariantType.Original)
		{
			if (!TeammatePerspective)
			{
				return;
			}
			matType = VariantType.Disguise;
		}
		model.ReplaceOriginalMaterials(delegate(Material original)
		{
			Material template = GetVariant(original, matType);
			Material orAdd = _materialInstances.GetOrAdd(template, () => new Material(template));
			_materialsInUse.Add(orAdd);
			return orAdd;
		});
	}

	public static Material GetVariant(Material original, VariantType matType)
	{
		if (_dictionarizedPairs == null)
		{
			if (!PlayerRoleLoader.TryGetRoleTemplate<Scp3114Role>(RoleTypeId.Scp3114, out var result))
			{
				return original;
			}
			if (!result.SubroutineModule.TryGetSubroutine<Scp3114FakeModelManager>(out var subroutine))
			{
				return original;
			}
			_dictionarizedPairs = new Dictionary<Material, MaterialPair>();
			_dictionarizedPairs.FromArray(subroutine._materialPairs, (MaterialPair x) => x.Original);
		}
		if (!_dictionarizedPairs.TryGetValue(original, out var value))
		{
			return original;
		}
		return value.FromType(matType);
	}
}
