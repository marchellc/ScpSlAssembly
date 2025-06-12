using System;
using System.Collections.Generic;
using GameObjectPools;
using PlayerRoles.Spectating;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson;

public class CharacterModel : PoolObject, IPoolResettable
{
	private record RendererMaterialPair(Renderer Rend, Material SingleMaterial, Material[] MultipleMaterials);

	private float _lastFade = 1f;

	private Material[] _fadeableMaterials;

	private bool _ownerless;

	private RendererMaterialPair[] _originalMaterials;

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

	public bool IsVisible { get; private set; }

	public ReferenceHub OwnerHub { get; private set; }

	public IFpcRole LastRole { get; private set; }

	public ReadOnlySpan<Renderer> Renderers => this._renderers;

	public Transform CachedTransform { get; private set; }

	public bool HasOwner => !this._ownerless;

	public virtual float Fade
	{
		get
		{
			return Mathf.Clamp01(this._lastFade);
		}
		set
		{
			value = Mathf.Clamp01(value);
			if (this.Fade != value)
			{
				int num = this.FadeableMaterials.Length;
				for (int i = 0; i < num; i++)
				{
					this._fadeableMaterials[i].SetFloat(CharacterModel.FadeHash, value);
				}
				this._lastFade = value;
				this.OnFadeChanged?.Invoke();
			}
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

	protected virtual Vector3 ModelPositionOffset => this._localPos;

	protected virtual Quaternion ModelRotationOffset => this._localRot;

	public event Action OnVisibilityChanged;

	public event Action OnFadeChanged;

	public event Action OnPlayerMoved;

	[RuntimeInitializeOnLoadMethod]
	private static void InitPools()
	{
		PlayerRoleLoader.OnLoaded = (Action)Delegate.Combine(PlayerRoleLoader.OnLoaded, (Action)delegate
		{
			foreach (KeyValuePair<RoleTypeId, PlayerRoleBase> allRole in PlayerRoleLoader.AllRoles)
			{
				if (allRole.Value is IFpcRole fpcRole)
				{
					GameObject characterModelTemplate = fpcRole.FpcModule.CharacterModelTemplate;
					if (characterModelTemplate == null || !characterModelTemplate.TryGetComponent<CharacterModel>(out var component))
					{
						Debug.LogError("No character model provided for " + allRole.Key);
					}
					else
					{
						PoolManager.Singleton.TryAddPool(component);
					}
				}
			}
		});
	}

	private void PrepareFadeableMaterials()
	{
		int num = this._renderers.Length;
		if (this._originalMaterials == null)
		{
			this.SetOriginalMaterials();
		}
		int total = 0;
		CharacterModel._previouslyDuplicatedMats.Clear();
		for (int i = 0; i < num; i++)
		{
			Renderer renderer = this._renderers[i];
			renderer.GetSharedMaterials(CharacterModel._sharedMaterialsNonAlloc);
			switch (CharacterModel._sharedMaterialsNonAlloc.Count)
			{
			case 1:
				renderer.sharedMaterial = this.InstantiateFadeableMaterialSingle(CharacterModel._sharedMaterialsNonAlloc[0], ref total);
				break;
			default:
				renderer.sharedMaterials = this.InstantiateAllFadeableMaterialList(ref total);
				break;
			case 0:
				break;
			}
		}
		this._fadeableMaterials = new Material[total];
		Array.Copy(CharacterModel._copyMaterialsNonAlloc, this._fadeableMaterials, total);
	}

	private Material InstantiateFadeableMaterialSingle(Material originalMat, ref int total)
	{
		if (!originalMat.HasFloat(CharacterModel.FadeHash))
		{
			return originalMat;
		}
		if (!CharacterModel._previouslyDuplicatedMats.TryGetValue(originalMat, out var value))
		{
			value = new Material(originalMat);
			CharacterModel._previouslyDuplicatedMats[originalMat] = value;
			CharacterModel._copyMaterialsNonAlloc[total] = value;
			total++;
		}
		return value;
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
		this.OnPlayerMoved?.Invoke();
	}

	public virtual void Setup(ReferenceHub owner, IFpcRole role, Vector3 localPos, Quaternion localRot)
	{
		this.OwnerHub = owner;
		this.LastRole = role;
		this._localPos = localPos;
		this._localRot = localRot;
		this._ownerTr = owner.transform;
		HitboxIdentity[] hitboxes = this.Hitboxes;
		foreach (HitboxIdentity hitboxIdentity in hitboxes)
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
		this.RestoreOriginalMaterials();
		this.Fade = 1f;
		HitboxIdentity[] hitboxes = this.Hitboxes;
		foreach (HitboxIdentity item in hitboxes)
		{
			HitboxIdentity.Instances.Remove(item);
		}
	}

	public void SetOriginalMaterials()
	{
		int num = this._renderers.Length;
		if (this._originalMaterials == null)
		{
			this._originalMaterials = new RendererMaterialPair[num];
		}
		for (int i = 0; i < num; i++)
		{
			Renderer renderer = this._renderers[i];
			renderer.GetSharedMaterials(CharacterModel._sharedMaterialsNonAlloc);
			Material singleMaterial;
			Material[] multipleMaterials;
			if (CharacterModel._sharedMaterialsNonAlloc.Count > 1)
			{
				singleMaterial = null;
				multipleMaterials = CharacterModel._sharedMaterialsNonAlloc.ToArray();
			}
			else
			{
				singleMaterial = CharacterModel._sharedMaterialsNonAlloc[0];
				multipleMaterials = null;
			}
			this._originalMaterials[i] = new RendererMaterialPair(renderer, singleMaterial, multipleMaterials);
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
			RendererMaterialPair rendererMaterialPair = this._originalMaterials[i];
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

	public void ReplaceOriginalMaterials(Func<Material, Material> replacer)
	{
		if (this._originalMaterials == null)
		{
			return;
		}
		for (int i = 0; i < this._originalMaterials.Length; i++)
		{
			RendererMaterialPair rendererMaterialPair = this._originalMaterials[i];
			if (rendererMaterialPair.MultipleMaterials == null)
			{
				Material material = replacer(rendererMaterialPair.SingleMaterial);
				rendererMaterialPair.Rend.sharedMaterial = ((material == null) ? rendererMaterialPair.SingleMaterial : material);
				continue;
			}
			int num = rendererMaterialPair.MultipleMaterials.Length;
			Material[] array = new Material[num];
			for (int j = 0; j < num; j++)
			{
				Material material2 = rendererMaterialPair.MultipleMaterials[j];
				Material material3 = replacer(material2);
				array[j] = ((material3 == null) ? material2 : material3);
			}
			rendererMaterialPair.Rend.sharedMaterials = array;
		}
		this._fadeableMaterials = null;
	}

	public virtual void SetAsOwnerless()
	{
		this._ownerless = true;
	}
}
