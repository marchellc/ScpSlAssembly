using System;
using System.Collections.Generic;
using InventorySystem.Items.Armor;
using Mirror;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers.Wearables;

public class WearableArmor : DisplayableWearableBase
{
	[Serializable]
	public struct ArmorSet
	{
		public ItemType[] ArmorTypes;

		public PackedArmorPart[] PackedArmorParts;

		public List<ElementBonePair> TrackedBones;

		public List<VisibleElement> VisibleObjects;

		public GameObject[] SimpleModelElements;
	}

	[Serializable]
	public class VisibleElement
	{
		public GameObject Target;

		public HumanBodyBones Parent;

		public bool ScaleToFade;

		private Transform _targetTr;

		private Vector3 _originalScale;

		public void Setup(Animator anim)
		{
			this._targetTr = this.Target.transform;
			if (this.Parent != HumanBodyBones.LastBone)
			{
				this._targetTr.SetParent(anim.GetBoneTransform(this.Parent));
			}
			if (this.ScaleToFade)
			{
				this._originalScale = this._targetTr.localScale;
			}
			this.Target.SetActive(value: false);
		}

		public void SetScale(float scale)
		{
			this._targetTr.localScale = this._originalScale * scale;
		}
	}

	[Serializable]
	public class ElementBonePair
	{
		[SerializeField]
		private Transform _target;

		[SerializeField]
		private HumanBodyBones _bone;

		private Transform _boneTr;

		public void Setup(Animator anim)
		{
			this._boneTr = anim.GetBoneTransform(this._bone);
		}

		public void Track()
		{
			this._boneTr.GetPositionAndRotation(out var position, out var rotation);
			this._target.SetPositionAndRotation(position, rotation);
		}
	}

	private static readonly Dictionary<Material, Material> NonAllocMaterialCopies = new Dictionary<Material, Material>();

	private static readonly List<Material> NonAllocMatList = new List<Material>();

	private static readonly int FadeHash = Shader.PropertyToID("_Fade");

	private readonly List<GameObject> _activeSimpleElements = new List<GameObject>();

	private readonly List<VisibleElement> _activeVisibleElements = new List<VisibleElement>();

	private readonly List<ElementBonePair> _trackedBones = new List<ElementBonePair>();

	private readonly List<Material> _allFadeableMaterials = new List<Material>();

	private bool _hasCuller;

	private ItemType? _lastArmor;

	[field: SerializeField]
	public ArmorSet[] DefinedSets { get; private set; }

	[field: SerializeField]
	public List<Renderer> FadeableRenderes { get; private set; }

	private ItemType ServerCurArmor
	{
		get
		{
			if (!base.Model.OwnerHub.inventory.TryGetBodyArmor(out var bodyArmor))
			{
				return ItemType.None;
			}
			return bodyArmor.ItemTypeId;
		}
	}

	public override void Initialize(WearableSubcontroller subcontroller)
	{
		base.Initialize(subcontroller);
		this.InitCuller();
		this.InitElements();
		this.InitFadeables();
		this.SetFade(base.Model.Fade);
		this.SetArmor(ItemType.None);
	}

	public override void SetFade(float fade)
	{
		foreach (VisibleElement activeVisibleElement in this._activeVisibleElements)
		{
			if (activeVisibleElement.ScaleToFade)
			{
				activeVisibleElement.SetScale(fade);
			}
		}
		foreach (Material allFadeableMaterial in this._allFadeableMaterials)
		{
			allFadeableMaterial.SetFloat(WearableArmor.FadeHash, fade);
		}
	}

	public override void WriteSyncvars(NetworkWriter writer)
	{
		base.WriteSyncvars(writer);
		writer.WriteSByte((sbyte)this.ServerCurArmor);
	}

	public override void ApplySyncvars(NetworkReader reader)
	{
		base.ApplySyncvars(reader);
		this.SetArmor((ItemType)reader.ReadSByte());
	}

	public override void OnWornStatusChanged()
	{
		base.OnWornStatusChanged();
		if (!base.IsWorn)
		{
			this.SetArmor(ItemType.None);
		}
	}

	public override void UpdateVisibility()
	{
		base.UpdateVisibility();
		foreach (VisibleElement activeVisibleElement in this._activeVisibleElements)
		{
			activeVisibleElement.Target.SetActive(base.IsVisible);
		}
	}

	private void LateUpdate()
	{
		if (!this._hasCuller)
		{
			this.MatchAllBones();
		}
	}

	private void MatchAllBones()
	{
		this.MatchBones(0);
	}

	private void MatchBones(int startIndex)
	{
		if (base.IsVisible)
		{
			for (int i = startIndex; i < this._trackedBones.Count; i++)
			{
				this._trackedBones[i].Track();
			}
		}
	}

	private void SetArmor(ItemType armorType)
	{
		if (this._lastArmor == armorType)
		{
			return;
		}
		this._lastArmor = armorType;
		foreach (VisibleElement activeVisibleElement in this._activeVisibleElements)
		{
			activeVisibleElement.Target.SetActive(value: false);
		}
		foreach (GameObject activeSimpleElement in this._activeSimpleElements)
		{
			activeSimpleElement.SetActive(value: false);
		}
		this._trackedBones.Clear();
		this._activeSimpleElements.Clear();
		this._activeVisibleElements.Clear();
		float fade = base.Model.Fade;
		ArmorSet[] definedSets = this.DefinedSets;
		for (int i = 0; i < definedSets.Length; i++)
		{
			ArmorSet armorSet = definedSets[i];
			if (!armorSet.ArmorTypes.Contains(armorType))
			{
				continue;
			}
			int count = this._trackedBones.Count;
			this._trackedBones.AddRange(armorSet.TrackedBones);
			this.MatchBones(count);
			foreach (VisibleElement visibleObject in armorSet.VisibleObjects)
			{
				visibleObject.Target.SetActive(base.IsVisible);
				if (visibleObject.ScaleToFade)
				{
					visibleObject.SetScale(fade);
				}
				this._activeVisibleElements.Add(visibleObject);
			}
			GameObject[] simpleModelElements = armorSet.SimpleModelElements;
			foreach (GameObject gameObject in simpleModelElements)
			{
				gameObject.SetActive(value: true);
				this._activeSimpleElements.Add(gameObject);
			}
		}
	}

	private void InitCuller()
	{
		if (base.Model.TryGetSubcontroller<CullingSubcontroller>(out var subcontroller))
		{
			this._hasCuller = true;
			subcontroller.OnAnimatorUpdated += MatchAllBones;
		}
	}

	private void InitElements()
	{
		Animator animator = base.Animator;
		ArmorSet[] definedSets = this.DefinedSets;
		for (int i = 0; i < definedSets.Length; i++)
		{
			ArmorSet target = definedSets[i];
			PackedArmorPart[] packedArmorParts = target.PackedArmorParts;
			for (int j = 0; j < packedArmorParts.Length; j++)
			{
				packedArmorParts[j].Unpack(target, this.FadeableRenderes);
			}
			foreach (VisibleElement visibleObject in target.VisibleObjects)
			{
				visibleObject.Setup(animator);
			}
			foreach (ElementBonePair trackedBone in target.TrackedBones)
			{
				trackedBone.Setup(animator);
			}
			GameObject[] simpleModelElements = target.SimpleModelElements;
			for (int j = 0; j < simpleModelElements.Length; j++)
			{
				simpleModelElements[j].SetActive(!base.Model.HasOwner);
			}
		}
	}

	private void InitFadeables()
	{
		WearableArmor.NonAllocMaterialCopies.Clear();
		foreach (Renderer fadeableRendere in this.FadeableRenderes)
		{
			this.SetupRendererMatInstances(fadeableRendere);
		}
		this._allFadeableMaterials.EnsureCapacity(WearableArmor.NonAllocMaterialCopies.Count);
		foreach (KeyValuePair<Material, Material> nonAllocMaterialCopy in WearableArmor.NonAllocMaterialCopies)
		{
			this._allFadeableMaterials.Add(nonAllocMaterialCopy.Value);
		}
	}

	private void SetupRendererMatInstances(Renderer rend)
	{
		WearableArmor.NonAllocMatList.Clear();
		rend.GetSharedMaterials(WearableArmor.NonAllocMatList);
		if (WearableArmor.NonAllocMatList.Count == 1)
		{
			rend.sharedMaterial = this.GetOrAddMatInstance(WearableArmor.NonAllocMatList[0]);
			return;
		}
		for (int i = 0; i < WearableArmor.NonAllocMatList.Count; i++)
		{
			WearableArmor.NonAllocMatList[i] = this.GetOrAddMatInstance(WearableArmor.NonAllocMatList[i]);
		}
		rend.SetMaterials(WearableArmor.NonAllocMatList);
	}

	private Material GetOrAddMatInstance(Material original)
	{
		if (WearableArmor.NonAllocMaterialCopies.TryGetValue(original, out var value))
		{
			return value;
		}
		Material material = new Material(original);
		WearableArmor.NonAllocMaterialCopies[original] = material;
		return material;
	}
}
