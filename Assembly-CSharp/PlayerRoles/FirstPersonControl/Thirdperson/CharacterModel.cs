using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using GameObjectPools;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson
{
	public class CharacterModel : PoolObject, IPoolResettable
	{
		public event Action OnVisibilityChanged;

		public event Action OnFadeChanged;

		public event Action OnPlayerMoved;

		public bool IsVisible { get; private set; }

		public ReferenceHub OwnerHub { get; private set; }

		public IFpcRole LastRole { get; private set; }

		public ReadOnlySpan<Renderer> Renderers
		{
			get
			{
				return this._renderers;
			}
		}

		public Transform CachedTransform { get; private set; }

		public bool ThreadmillEnabled { get; private set; }

		public virtual float Fade
		{
			get
			{
				return Mathf.Clamp01(this._lastFade);
			}
			set
			{
				value = Mathf.Clamp01(value);
				if (this.Fade == value)
				{
					return;
				}
				int num = this.FadeableMaterials.Length;
				for (int i = 0; i < num; i++)
				{
					this._fadeableMaterials[i].SetFloat(CharacterModel.FadeHash, value);
				}
				this._lastFade = value;
				Action onFadeChanged = this.OnFadeChanged;
				if (onFadeChanged == null)
				{
					return;
				}
				onFadeChanged();
			}
		}

		public Material[] FadeableMaterials
		{
			get
			{
				if (this._fadeableMaterials == null)
				{
					this.PrepareFadeableMaterials();
				}
				return this._fadeableMaterials;
			}
		}

		protected virtual Vector3 ModelPositionOffset
		{
			get
			{
				return this._localPos;
			}
		}

		protected virtual Quaternion ModelRotationOffset
		{
			get
			{
				return this._localRot;
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void InitPools()
		{
			PlayerRoleLoader.OnLoaded = (Action)Delegate.Combine(PlayerRoleLoader.OnLoaded, new Action(delegate
			{
				foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> keyValuePair in PlayerRoleLoader.AllRoles)
				{
					IFpcRole fpcRole = keyValuePair.Value as IFpcRole;
					if (fpcRole != null)
					{
						GameObject characterModelTemplate = fpcRole.FpcModule.CharacterModelTemplate;
						CharacterModel characterModel;
						if (characterModelTemplate == null || !characterModelTemplate.TryGetComponent<CharacterModel>(out characterModel))
						{
							Debug.LogError("No character model provided for " + keyValuePair.Key.ToString());
						}
						else
						{
							PoolManager.Singleton.TryAddPool(characterModel);
						}
					}
				}
			}));
		}

		private void PrepareFadeableMaterials()
		{
			int num = this._renderers.Length;
			if (this._originalMaterials == null)
			{
				this.SetOriginalMaterials();
			}
			int num2 = 0;
			CharacterModel._previouslyDuplicatedMats.Clear();
			for (int i = 0; i < num; i++)
			{
				Renderer renderer = this._renderers[i];
				renderer.GetSharedMaterials(CharacterModel._sharedMaterialsNonAlloc);
				int count = CharacterModel._sharedMaterialsNonAlloc.Count;
				if (count != 0)
				{
					if (count == 1)
					{
						renderer.sharedMaterial = this.InstantiateFadeableMaterialSingle(CharacterModel._sharedMaterialsNonAlloc[0], ref num2);
					}
					else
					{
						renderer.sharedMaterials = this.InstantiateAllFadeableMaterialList(ref num2);
					}
				}
			}
			this._fadeableMaterials = new Material[num2];
			Array.Copy(CharacterModel._copyMaterialsNonAlloc, this._fadeableMaterials, num2);
		}

		private Material InstantiateFadeableMaterialSingle(Material originalMat, ref int total)
		{
			if (!originalMat.HasFloat(CharacterModel.FadeHash))
			{
				return originalMat;
			}
			Material material;
			if (!CharacterModel._previouslyDuplicatedMats.TryGetValue(originalMat, out material))
			{
				material = new Material(originalMat);
				CharacterModel._previouslyDuplicatedMats[originalMat] = material;
				CharacterModel._copyMaterialsNonAlloc[total] = material;
				total++;
			}
			return material;
		}

		private Material[] InstantiateAllFadeableMaterialList(ref int total)
		{
			Material[] array = new Material[CharacterModel._sharedMaterialsNonAlloc.Count];
			for (int i = 0; i < array.Length; i++)
			{
				array[i] = this.InstantiateFadeableMaterialSingle(CharacterModel._sharedMaterialsNonAlloc[i], ref total);
			}
			return array;
		}

		protected virtual void Awake()
		{
			this.CachedTransform = base.transform;
		}

		protected virtual void OnValidate()
		{
			this.Hitboxes = base.GetComponentsInChildren<HitboxIdentity>();
			MeshRenderer[] componentsInChildren = base.GetComponentsInChildren<MeshRenderer>();
			SkinnedMeshRenderer[] componentsInChildren2 = base.GetComponentsInChildren<SkinnedMeshRenderer>();
			int num = componentsInChildren.Length;
			int num2 = componentsInChildren2.Length;
			this._renderers = new Renderer[num + num2];
			Array.Copy(componentsInChildren, this._renderers, num);
			Array.Copy(componentsInChildren2, 0, this._renderers, num, num2);
		}

		public virtual void OnPlayerMove()
		{
			this.CachedTransform.SetPositionAndRotation(this._ownerTr.TransformPoint(this.ModelPositionOffset), this.ModelRotationOffset * this._ownerTr.rotation);
			Action onPlayerMoved = this.OnPlayerMoved;
			if (onPlayerMoved == null)
			{
				return;
			}
			onPlayerMoved();
		}

		public virtual void Setup(ReferenceHub owner, IFpcRole role, Vector3 localPos, Quaternion localRot)
		{
			this.OwnerHub = owner;
			this.LastRole = role;
			this._localPos = localPos;
			this._localRot = localRot;
			this._ownerTr = owner.transform;
			foreach (HitboxIdentity hitboxIdentity in this.Hitboxes)
			{
				HitboxIdentity.Instances.Add(hitboxIdentity);
				hitboxIdentity.SetColliders(!this.OwnerHub.isLocalPlayer);
			}
		}

		public virtual void SetVisibility(bool newState)
		{
		}

		public virtual void PlayShotEffects(HitboxIdentity hitbox, Vector3 shotDirection)
		{
			this._onShotSettings.PlayOnShotSound(hitbox, this.OwnerHub.isLocalPlayer || this.OwnerHub.IsLocallySpectated());
		}

		public virtual void ResetObject()
		{
			this.Fade = 1f;
			foreach (HitboxIdentity hitboxIdentity in this.Hitboxes)
			{
				HitboxIdentity.Instances.Remove(hitboxIdentity);
			}
		}

		public void SetOriginalMaterials()
		{
			int num = this._renderers.Length;
			if (this._originalMaterials == null)
			{
				this._originalMaterials = new CharacterModel.RendererMaterialPair[num];
			}
			for (int i = 0; i < num; i++)
			{
				Renderer renderer = this._renderers[i];
				renderer.GetSharedMaterials(CharacterModel._sharedMaterialsNonAlloc);
				Material material;
				Material[] array;
				if (CharacterModel._sharedMaterialsNonAlloc.Count > 1)
				{
					material = null;
					array = CharacterModel._sharedMaterialsNonAlloc.ToArray();
				}
				else
				{
					material = CharacterModel._sharedMaterialsNonAlloc[0];
					array = null;
				}
				this._originalMaterials[i] = new CharacterModel.RendererMaterialPair(renderer, material, array);
			}
		}

		public void RestoreOriginalMaterials()
		{
			if (this._originalMaterials == null)
			{
				return;
			}
			for (int i = 0; i < this._originalMaterials.Length; i++)
			{
				CharacterModel.RendererMaterialPair rendererMaterialPair = this._originalMaterials[i];
				if (rendererMaterialPair.MultipleMaterials == null)
				{
					rendererMaterialPair.Rend.sharedMaterial = rendererMaterialPair.SingleMaterial;
				}
				else
				{
					rendererMaterialPair.Rend.sharedMaterials = rendererMaterialPair.MultipleMaterials;
				}
			}
			this._fadeableMaterials = null;
		}

		public virtual void OnTreadmillInitialized()
		{
			this.ThreadmillEnabled = true;
		}

		private float _lastFade = 1f;

		private Material[] _fadeableMaterials;

		private CharacterModel.RendererMaterialPair[] _originalMaterials;

		private Transform _ownerTr;

		private Vector3 _localPos;

		private Quaternion _localRot;

		private static readonly int FadeHash = Shader.PropertyToID("_Fade");

		private static readonly Material[] _copyMaterialsNonAlloc = new Material[128];

		private static readonly List<Material> _sharedMaterialsNonAlloc = new List<Material>();

		private static readonly Dictionary<Material, Material> _previouslyDuplicatedMats = new Dictionary<Material, Material>();

		[SerializeField]
		private Renderer[] _renderers;

		[SerializeField]
		private ModelShotSettings _onShotSettings;

		public HitboxIdentity[] Hitboxes;

		private class RendererMaterialPair : IEquatable<CharacterModel.RendererMaterialPair>
		{
			public RendererMaterialPair(Renderer Rend, Material SingleMaterial, Material[] MultipleMaterials)
			{
				this.Rend = Rend;
				this.SingleMaterial = SingleMaterial;
				this.MultipleMaterials = MultipleMaterials;
				base..ctor();
			}

			[Nullable(1)]
			protected virtual Type EqualityContract
			{
				[NullableContext(1)]
				[CompilerGenerated]
				get
				{
					return typeof(CharacterModel.RendererMaterialPair);
				}
			}

			public Renderer Rend { get; set; }

			public Material SingleMaterial { get; set; }

			public Material[] MultipleMaterials { get; set; }

			public override string ToString()
			{
				StringBuilder stringBuilder = new StringBuilder();
				stringBuilder.Append("RendererMaterialPair");
				stringBuilder.Append(" { ");
				if (this.PrintMembers(stringBuilder))
				{
					stringBuilder.Append(" ");
				}
				stringBuilder.Append("}");
				return stringBuilder.ToString();
			}

			[NullableContext(1)]
			protected virtual bool PrintMembers(StringBuilder builder)
			{
				builder.Append("Rend");
				builder.Append(" = ");
				builder.Append(this.Rend);
				builder.Append(", ");
				builder.Append("SingleMaterial");
				builder.Append(" = ");
				builder.Append(this.SingleMaterial);
				builder.Append(", ");
				builder.Append("MultipleMaterials");
				builder.Append(" = ");
				builder.Append(this.MultipleMaterials);
				return true;
			}

			[NullableContext(2)]
			public static bool operator !=(CharacterModel.RendererMaterialPair r1, CharacterModel.RendererMaterialPair r2)
			{
				return !(r1 == r2);
			}

			[NullableContext(2)]
			public static bool operator ==(CharacterModel.RendererMaterialPair r1, CharacterModel.RendererMaterialPair r2)
			{
				return r1 == r2 || (r1 != null && r1.Equals(r2));
			}

			public override int GetHashCode()
			{
				return ((EqualityComparer<Type>.Default.GetHashCode(this.EqualityContract) * -1521134295 + EqualityComparer<Renderer>.Default.GetHashCode(this.<Rend>k__BackingField)) * -1521134295 + EqualityComparer<Material>.Default.GetHashCode(this.<SingleMaterial>k__BackingField)) * -1521134295 + EqualityComparer<Material[]>.Default.GetHashCode(this.<MultipleMaterials>k__BackingField);
			}

			[NullableContext(2)]
			public override bool Equals(object obj)
			{
				return this.Equals(obj as CharacterModel.RendererMaterialPair);
			}

			[NullableContext(2)]
			public virtual bool Equals(CharacterModel.RendererMaterialPair other)
			{
				return other != null && this.EqualityContract == other.EqualityContract && EqualityComparer<Renderer>.Default.Equals(this.<Rend>k__BackingField, other.<Rend>k__BackingField) && EqualityComparer<Material>.Default.Equals(this.<SingleMaterial>k__BackingField, other.<SingleMaterial>k__BackingField) && EqualityComparer<Material[]>.Default.Equals(this.<MultipleMaterials>k__BackingField, other.<MultipleMaterials>k__BackingField);
			}

			[NullableContext(1)]
			public virtual CharacterModel.RendererMaterialPair <Clone>$()
			{
				return new CharacterModel.RendererMaterialPair(this);
			}

			protected RendererMaterialPair([Nullable(1)] CharacterModel.RendererMaterialPair original)
			{
				this.Rend = original.<Rend>k__BackingField;
				this.SingleMaterial = original.<SingleMaterial>k__BackingField;
				this.MultipleMaterials = original.<MultipleMaterials>k__BackingField;
			}

			public void Deconstruct(out Renderer Rend, out Material SingleMaterial, out Material[] MultipleMaterials)
			{
				Rend = this.Rend;
				SingleMaterial = this.SingleMaterial;
				MultipleMaterials = this.MultipleMaterials;
			}
		}
	}
}
