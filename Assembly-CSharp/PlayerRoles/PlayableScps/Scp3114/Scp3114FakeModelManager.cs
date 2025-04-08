using System;
using System.Collections.Generic;
using GameObjectPools;
using PlayerRoles.FirstPersonControl.Thirdperson;
using PlayerRoles.Subroutines;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace PlayerRoles.PlayableScps.Scp3114
{
	public class Scp3114FakeModelManager : StandardSubroutine<Scp3114Role>
	{
		private float AnimProgress
		{
			get
			{
				return this._animProgress;
			}
			set
			{
				float num = Mathf.Clamp01(value);
				if (num == this._animProgress)
				{
					return;
				}
				this._animProgress = num;
				this.UpdateModelMaterials();
			}
		}

		private bool TeammatePerspective
		{
			get
			{
				ReferenceHub referenceHub;
				return ReferenceHub.TryGetPovHub(out referenceHub) && referenceHub.IsSCP(true);
			}
		}

		public override void SpawnObject()
		{
			base.SpawnObject();
			base.CastRole.CurIdentity.OnStatusChanged += this.OnIdentityChanged;
			this._ownModel = base.CastRole.FpcModule.CharacterModelInstance as Scp3114Model;
			this._ownModel.OnVisibilityChanged += this.UpdateVisibilityAll;
			this.UpdateModelMaterials();
		}

		public override void ResetObject()
		{
			base.ResetObject();
			if (this._ownModel != null)
			{
				this._ownModel.OnVisibilityChanged += this.UpdateVisibilityAll;
			}
			this._animProgress = 0f;
			this._trackBones = false;
			this._ownModelMapped = false;
			this._lastActiveStatus = Scp3114Identity.DisguiseStatus.None;
			this._lastMaterialType = Scp3114FakeModelManager.VariantType.Original;
			base.CastRole.CurIdentity.OnStatusChanged -= this.OnIdentityChanged;
			this._modelInstances.ForEachValue(delegate(AnimatedCharacterModel mod)
			{
				mod.RestoreOriginalMaterials();
				this.RestoreBones(mod);
				mod.Animator.StartPlayback();
				mod.ReturnToPool(true);
			});
			this._materialsInUse.Clear();
			this._modelInstances.Clear();
			this._ownModelBones.Clear();
			this._mappedBones.Clear();
		}

		private void UpdateModelMaterials()
		{
			CharacterModel characterModelInstance = base.CastRole.FpcModule.CharacterModelInstance;
			float progress = this.AnimProgress;
			Scp3114FakeModelManager.VariantType lastMaterialType = this._lastMaterialType;
			if (lastMaterialType > Scp3114FakeModelManager.VariantType.Disguise)
			{
				if (lastMaterialType == Scp3114FakeModelManager.VariantType.Reveal)
				{
					characterModelInstance.Fade = this._revealAnimation.FadeOverProgress.Evaluate(progress);
				}
			}
			else
			{
				if (this.TeammatePerspective)
				{
					progress *= this._teammateProgressMultiplier;
				}
				characterModelInstance.Fade = this._disguiseAnimation.FadeOverProgress.Evaluate(progress);
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
			case Scp3114Identity.DisguiseStatus.None:
				if (this._lastActiveStatus == Scp3114Identity.DisguiseStatus.Active)
				{
					this.TrySetModel(curIdentity.StolenRole, Scp3114FakeModelManager.VariantType.Reveal);
					this._trackBones = true;
					this.AnimProgress = 0f;
				}
				break;
			case Scp3114Identity.DisguiseStatus.Equipping:
				this.TrySetModel(curIdentity.StolenRole, Scp3114FakeModelManager.VariantType.Disguise);
				this.AnimProgress = 0f;
				this._trackBones = true;
				this._lastActiveStatus = Scp3114Identity.DisguiseStatus.Equipping;
				break;
			case Scp3114Identity.DisguiseStatus.Active:
				if (this.TrySetModel(curIdentity.StolenRole, Scp3114FakeModelManager.VariantType.Original))
				{
					this.RestoreBones(this._lastModel);
				}
				this.AnimProgress = 1f;
				this._trackBones = this.TeammatePerspective;
				this._lastActiveStatus = Scp3114Identity.DisguiseStatus.Active;
				break;
			}
			this._wasTeammatePerpsective = this.TeammatePerspective;
		}

		private void UpdatePerspective()
		{
			if (this.TeammatePerspective == this._wasTeammatePerpsective)
			{
				return;
			}
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

		private void UpdateVisibilityAll()
		{
			this._modelInstances.ForEachValue(delegate(AnimatedCharacterModel x)
			{
				x.SetVisibility(this._ownModel.IsVisible);
			});
		}

		private void Update()
		{
			this.UpdatePerspective();
			Scp3114Identity.DisguiseStatus status = base.CastRole.CurIdentity.Status;
			if (status != Scp3114Identity.DisguiseStatus.None)
			{
				if (status == Scp3114Identity.DisguiseStatus.Equipping)
				{
					this.AnimProgress += Time.deltaTime * this._disguiseAnimation.ProgressSpeed;
					return;
				}
			}
			else if (this._lastActiveStatus == Scp3114Identity.DisguiseStatus.Active && this.AnimProgress < 1f)
			{
				this.AnimProgress += Time.deltaTime * this._revealAnimation.ProgressSpeed;
				if (this.AnimProgress == 1f)
				{
					this.TrySetModel(RoleTypeId.None, Scp3114FakeModelManager.VariantType.Original);
					return;
				}
			}
			else if (this._lastActiveStatus == Scp3114Identity.DisguiseStatus.Equipping && this.AnimProgress > 0f)
			{
				this.AnimProgress -= Time.deltaTime * this._disguiseAnimation.ProgressSpeed;
				if (this.AnimProgress == 0f)
				{
					this.TrySetModel(RoleTypeId.None, Scp3114FakeModelManager.VariantType.Original);
				}
			}
		}

		private void LateUpdate()
		{
			if (!this._trackBones)
			{
				return;
			}
			if (!this.TryTrackBones())
			{
				this._trackBones = false;
			}
		}

		private bool TryTrackBones()
		{
			if (this._lastModel == null || !this._lastModel.gameObject.activeSelf)
			{
				return false;
			}
			List<Scp3114FakeModelManager.MappedBone> list;
			if (!this._mappedBones.TryGetValue(this._lastModel, out list))
			{
				return false;
			}
			foreach (Scp3114FakeModelManager.MappedBone mappedBone in list)
			{
				Transform tracked = mappedBone.Tracked;
				mappedBone.Original.SetPositionAndRotation(tracked.position, tracked.rotation);
			}
			return true;
		}

		private bool TrySetModel(RoleTypeId role, Scp3114FakeModelManager.VariantType variant)
		{
			HumanRole humanRole;
			if (!PlayerRoleLoader.TryGetRoleTemplate<HumanRole>(role, out humanRole))
			{
				if (this._lastModel != null)
				{
					this._lastModel.gameObject.SetActive(false);
				}
				return false;
			}
			GameObject characterModelTemplate = humanRole.FpcModule.CharacterModelTemplate;
			AnimatedCharacterModel animatedCharacterModel;
			if (!this._modelInstances.TryGetValue(characterModelTemplate, out animatedCharacterModel))
			{
				if (!this.TryCreateModel(characterModelTemplate, out animatedCharacterModel))
				{
					return false;
				}
				this._modelInstances.Add(characterModelTemplate, animatedCharacterModel);
			}
			else if (this._lastModel != animatedCharacterModel && this._lastModel != null)
			{
				this._lastModel.gameObject.SetActive(false);
			}
			this._lastModel = animatedCharacterModel;
			animatedCharacterModel.gameObject.SetActive(true);
			animatedCharacterModel.RestoreOriginalMaterials();
			this.SetModelMaterials(animatedCharacterModel, variant);
			this._lastMaterialType = variant;
			return true;
		}

		private bool TryCreateModel(GameObject template, out AnimatedCharacterModel model)
		{
			model = null;
			PoolObject poolObject;
			if (!PoolManager.Singleton.TryGetPoolObject(template, base.transform, out poolObject, true))
			{
				return false;
			}
			AnimatedCharacterModel animatedCharacterModel = poolObject as AnimatedCharacterModel;
			if (animatedCharacterModel == null)
			{
				return false;
			}
			if (!this._ownModelMapped)
			{
				this._ownModel.gameObject.ForEachComponentInChildren(delegate(Transform bone)
				{
					string name = bone.name;
					if (!name.StartsWith("mixamorig:"))
					{
						return;
					}
					this._ownModelBones[name] = bone;
				}, true);
				this._ownModelMapped = true;
			}
			model = animatedCharacterModel;
			Transform transform = template.transform;
			Transform transform2 = model.transform;
			transform2.localPosition = transform.position;
			transform2.localScale = transform.localScale;
			transform2.localRotation = transform.rotation;
			model.SetVisibility(this._ownModel.IsVisible);
			model.SetOriginalMaterials();
			List<Scp3114FakeModelManager.MappedBone> boneMap = this._mappedBones.GetOrAdd(model, () => new List<Scp3114FakeModelManager.MappedBone>());
			transform2.gameObject.ForEachComponentInChildren(delegate(Transform bone)
			{
				string name2 = bone.name;
				Transform transform3;
				if (!this._ownModelBones.TryGetValue(name2, out transform3))
				{
					return;
				}
				boneMap.Add(new Scp3114FakeModelManager.MappedBone
				{
					Original = bone,
					Tracked = transform3,
					PrevPos = bone.localPosition,
					PrevRot = bone.localRotation
				});
			}, true);
			return true;
		}

		private void RestoreBones(AnimatedCharacterModel model)
		{
			List<Scp3114FakeModelManager.MappedBone> list;
			if (!this._mappedBones.TryGetValue(model, out list))
			{
				return;
			}
			foreach (Scp3114FakeModelManager.MappedBone mappedBone in list)
			{
				mappedBone.Original.SetLocalPositionAndRotation(mappedBone.PrevPos, mappedBone.PrevRot);
			}
		}

		private unsafe void SetModelMaterials(AnimatedCharacterModel model, Scp3114FakeModelManager.VariantType matType)
		{
			model.RestoreOriginalMaterials();
			this._materialsInUse.Clear();
			if (matType == Scp3114FakeModelManager.VariantType.Original)
			{
				if (!this.TeammatePerspective)
				{
					return;
				}
				matType = Scp3114FakeModelManager.VariantType.Disguise;
			}
			ReadOnlySpan<Renderer> renderers = model.Renderers;
			for (int i = 0; i < renderers.Length; i++)
			{
				Renderer renderer = *renderers[i];
				Material template = Scp3114FakeModelManager.GetVariant(renderer.sharedMaterial, matType);
				Material orAdd = this._materialInstances.GetOrAdd(template, () => new Material(template));
				renderer.sharedMaterial = orAdd;
				this._materialsInUse.Add(orAdd);
			}
		}

		public static Material GetVariant(Material original, Scp3114FakeModelManager.VariantType matType)
		{
			if (Scp3114FakeModelManager._dictionarizedPairs == null)
			{
				Scp3114Role scp3114Role;
				if (!PlayerRoleLoader.TryGetRoleTemplate<Scp3114Role>(RoleTypeId.Scp3114, out scp3114Role))
				{
					return original;
				}
				Scp3114FakeModelManager scp3114FakeModelManager;
				if (!scp3114Role.SubroutineModule.TryGetSubroutine<Scp3114FakeModelManager>(out scp3114FakeModelManager))
				{
					return original;
				}
				Scp3114FakeModelManager._dictionarizedPairs = new Dictionary<Material, Scp3114FakeModelManager.MaterialPair>();
				Scp3114FakeModelManager._dictionarizedPairs.FromArray(scp3114FakeModelManager._materialPairs, (Scp3114FakeModelManager.MaterialPair x) => x.Original);
			}
			Scp3114FakeModelManager.MaterialPair materialPair;
			if (!Scp3114FakeModelManager._dictionarizedPairs.TryGetValue(original, out materialPair))
			{
				return original;
			}
			return materialPair.FromType(matType);
		}

		[SerializeField]
		private Scp3114FakeModelManager.MaterialAnimation _disguiseAnimation;

		[SerializeField]
		private Scp3114FakeModelManager.MaterialAnimation _revealAnimation;

		[SerializeField]
		private Scp3114FakeModelManager.MaterialPair[] _materialPairs;

		[SerializeField]
		private float _teammateProgressMultiplier;

		private static Dictionary<Material, Scp3114FakeModelManager.MaterialPair> _dictionarizedPairs;

		private Scp3114Identity.DisguiseStatus _lastActiveStatus;

		private Scp3114FakeModelManager.VariantType _lastMaterialType;

		private float _animProgress;

		private bool _ownModelMapped;

		private bool _trackBones;

		private AnimatedCharacterModel _lastModel;

		private Scp3114Model _ownModel;

		private bool _wasTeammatePerpsective;

		private const string RigPrefix = "mixamorig:";

		private static readonly int ProgressHash = Shader.PropertyToID("_Progress");

		private readonly Dictionary<Material, Material> _materialInstances = new Dictionary<Material, Material>();

		private readonly Dictionary<GameObject, AnimatedCharacterModel> _modelInstances = new Dictionary<GameObject, AnimatedCharacterModel>();

		private readonly Dictionary<AnimatedCharacterModel, List<Scp3114FakeModelManager.MappedBone>> _mappedBones = new Dictionary<AnimatedCharacterModel, List<Scp3114FakeModelManager.MappedBone>>();

		private readonly Dictionary<string, Transform> _ownModelBones = new Dictionary<string, Transform>();

		private readonly List<Material> _materialsInUse = new List<Material>();

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

			public Vector3 PrevPos;

			public Quaternion PrevRot;
		}

		[Serializable]
		public class MaterialPair
		{
			public Material FromType(Scp3114FakeModelManager.VariantType type)
			{
				Material material;
				switch (type)
				{
				case Scp3114FakeModelManager.VariantType.Original:
					material = this.Original;
					break;
				case Scp3114FakeModelManager.VariantType.Disguise:
					material = this.Disguise;
					break;
				case Scp3114FakeModelManager.VariantType.Reveal:
					material = this.Reveal;
					break;
				default:
					throw new NotImplementedException("Unknown material type");
				}
				return material;
			}

			public Material Original;

			public Material Disguise;

			public Material Reveal;
		}

		public enum VariantType
		{
			Original,
			Disguise,
			Reveal
		}
	}
}
