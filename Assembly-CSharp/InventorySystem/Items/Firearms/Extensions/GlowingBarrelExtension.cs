using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions;

public class GlowingBarrelExtension : OverheatExtensionBase
{
	[Serializable]
	private struct AffectedMaterial
	{
		public Renderer[] Renderers;
	}

	private static readonly Dictionary<Material, Queue<Material>> MaterialPool = new Dictionary<Material, Queue<Material>>();

	private static readonly int TemperatureHash = Shader.PropertyToID("_Temperature");

	[SerializeField]
	private AffectedMaterial[] _materialsToCopy;

	private Material[] _materialTemplates;

	private Material[] _materialInstances;

	private int _materialCount;

	protected override void OnTemperatureChanged(float temp)
	{
		for (int i = 0; i < this._materialCount; i++)
		{
			this._materialInstances[i].SetFloat(GlowingBarrelExtension.TemperatureHash, temp);
		}
	}

	public override void OnDestroyExtension()
	{
		base.OnDestroyExtension();
		for (int i = 0; i < this._materialCount; i++)
		{
			Material key = this._materialTemplates[i];
			Material item = this._materialInstances[i];
			GlowingBarrelExtension.MaterialPool.GetOrAddNew(key).Enqueue(item);
		}
	}

	private void Start()
	{
		this._materialCount = this._materialsToCopy.Length;
		this._materialTemplates = new Material[this._materialCount];
		this._materialInstances = new Material[this._materialCount];
		for (int i = 0; i < this._materialCount; i++)
		{
			AffectedMaterial affectedMaterial = this._materialsToCopy[i];
			Material sharedMaterial = affectedMaterial.Renderers[0].sharedMaterial;
			Queue<Material> value;
			Material result;
			Material material = ((!GlowingBarrelExtension.MaterialPool.TryGetValue(sharedMaterial, out value) || !value.TryDequeue(out result)) ? new Material(sharedMaterial) : result);
			this._materialTemplates[i] = sharedMaterial;
			this._materialInstances[i] = material;
			for (int j = 0; j < affectedMaterial.Renderers.Length; j++)
			{
				affectedMaterial.Renderers[j].sharedMaterial = material;
			}
		}
	}
}
