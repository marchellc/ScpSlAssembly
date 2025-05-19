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

	public ReadOnlySpan<Renderer> Renderers => _renderers;

	public Transform CachedTransform { get; private set; }

	public bool HasOwner => !_ownerless;

	public virtual float Fade
	{
		get
		{
			return Mathf.Clamp01(_lastFade);
		}
		set
		{
			value = Mathf.Clamp01(value);
			if (Fade != value)
			{
				int num = FadeableMaterials.Length;
				for (int i = 0; i < num; i++)
				{
					_fadeableMaterials[i].SetFloat(FadeHash, value);
				}
				_lastFade = value;
				this.OnFadeChanged?.Invoke();
			}
		}
	}

	public Material[] FadeableMaterials
	{
		get
		{
			if (_fadeableMaterials == null)
			{
				PrepareFadeableMaterials();
			}
			return _fadeableMaterials;
		}
	}

	protected virtual Vector3 ModelPositionOffset => _localPos;

	protected virtual Quaternion ModelRotationOffset => _localRot;

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
		int num = _renderers.Length;
		if (_originalMaterials == null)
		{
			SetOriginalMaterials();
		}
		int total = 0;
		_previouslyDuplicatedMats.Clear();
		for (int i = 0; i < num; i++)
		{
			Renderer renderer = _renderers[i];
			renderer.GetSharedMaterials(_sharedMaterialsNonAlloc);
			switch (_sharedMaterialsNonAlloc.Count)
			{
			case 1:
				renderer.sharedMaterial = InstantiateFadeableMaterialSingle(_sharedMaterialsNonAlloc[0], ref total);
				break;
			default:
				renderer.sharedMaterials = InstantiateAllFadeableMaterialList(ref total);
				break;
			case 0:
				break;
			}
		}
		_fadeableMaterials = new Material[total];
		Array.Copy(_copyMaterialsNonAlloc, _fadeableMaterials, total);
	}

	private Material InstantiateFadeableMaterialSingle(Material originalMat, ref int total)
	{
		if (!originalMat.HasFloat(FadeHash))
		{
			return originalMat;
		}
		if (!_previouslyDuplicatedMats.TryGetValue(originalMat, out var value))
		{
			value = new Material(originalMat);
			_previouslyDuplicatedMats[originalMat] = value;
			_copyMaterialsNonAlloc[total] = value;
			total++;
		}
		return value;
	}

	private Material[] InstantiateAllFadeableMaterialList(ref int total)
	{
		Material[] array = new Material[_sharedMaterialsNonAlloc.Count];
		for (int i = 0; i < array.Length; i++)
		{
			array[i] = InstantiateFadeableMaterialSingle(_sharedMaterialsNonAlloc[i], ref total);
		}
		return array;
	}

	protected virtual void Awake()
	{
		CachedTransform = base.transform;
	}

	protected virtual void OnValidate()
	{
		Hitboxes = GetComponentsInChildren<HitboxIdentity>();
		MeshRenderer[] componentsInChildren = GetComponentsInChildren<MeshRenderer>();
		SkinnedMeshRenderer[] componentsInChildren2 = GetComponentsInChildren<SkinnedMeshRenderer>();
		int num = componentsInChildren.Length;
		int num2 = componentsInChildren2.Length;
		_renderers = new Renderer[num + num2];
		Array.Copy(componentsInChildren, _renderers, num);
		Array.Copy(componentsInChildren2, 0, _renderers, num, num2);
	}

	public virtual void OnPlayerMove()
	{
		CachedTransform.SetPositionAndRotation(_ownerTr.TransformPoint(ModelPositionOffset), ModelRotationOffset * _ownerTr.rotation);
		this.OnPlayerMoved?.Invoke();
	}

	public virtual void Setup(ReferenceHub owner, IFpcRole role, Vector3 localPos, Quaternion localRot)
	{
		OwnerHub = owner;
		LastRole = role;
		_localPos = localPos;
		_localRot = localRot;
		_ownerTr = owner.transform;
		HitboxIdentity[] hitboxes = Hitboxes;
		foreach (HitboxIdentity hitboxIdentity in hitboxes)
		{
			HitboxIdentity.Instances.Add(hitboxIdentity);
			hitboxIdentity.SetColliders(!OwnerHub.isLocalPlayer);
		}
	}

	public virtual void SetVisibility(bool newState)
	{
	}

	public virtual void PlayShotEffects(HitboxIdentity hitbox, Vector3 shotDirection)
	{
		_onShotSettings.PlayOnShotSound(hitbox, OwnerHub.isLocalPlayer || OwnerHub.IsLocallySpectated());
	}

	public virtual void ResetObject()
	{
		RestoreOriginalMaterials();
		Fade = 1f;
		HitboxIdentity[] hitboxes = Hitboxes;
		foreach (HitboxIdentity item in hitboxes)
		{
			HitboxIdentity.Instances.Remove(item);
		}
	}

	public void SetOriginalMaterials()
	{
		int num = _renderers.Length;
		if (_originalMaterials == null)
		{
			_originalMaterials = new RendererMaterialPair[num];
		}
		for (int i = 0; i < num; i++)
		{
			Renderer renderer = _renderers[i];
			renderer.GetSharedMaterials(_sharedMaterialsNonAlloc);
			Material singleMaterial;
			Material[] multipleMaterials;
			if (_sharedMaterialsNonAlloc.Count > 1)
			{
				singleMaterial = null;
				multipleMaterials = _sharedMaterialsNonAlloc.ToArray();
			}
			else
			{
				singleMaterial = _sharedMaterialsNonAlloc[0];
				multipleMaterials = null;
			}
			_originalMaterials[i] = new RendererMaterialPair(renderer, singleMaterial, multipleMaterials);
		}
	}

	public void RestoreOriginalMaterials()
	{
		if (_originalMaterials == null)
		{
			return;
		}
		for (int i = 0; i < _originalMaterials.Length; i++)
		{
			RendererMaterialPair rendererMaterialPair = _originalMaterials[i];
			if (rendererMaterialPair.MultipleMaterials == null)
			{
				rendererMaterialPair.Rend.sharedMaterial = rendererMaterialPair.SingleMaterial;
			}
			else
			{
				rendererMaterialPair.Rend.sharedMaterials = rendererMaterialPair.MultipleMaterials;
			}
		}
		_fadeableMaterials = null;
	}

	public void ReplaceOriginalMaterials(Func<Material, Material> replacer)
	{
		if (_originalMaterials == null)
		{
			return;
		}
		for (int i = 0; i < _originalMaterials.Length; i++)
		{
			RendererMaterialPair rendererMaterialPair = _originalMaterials[i];
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
		_fadeableMaterials = null;
	}

	public virtual void SetAsOwnerless()
	{
		_ownerless = true;
	}
}
