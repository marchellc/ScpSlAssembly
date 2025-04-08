using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class GlowingBarrelExtension : OverheatExtensionBase
	{
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
				Material material = this._materialTemplates[i];
				Material material2 = this._materialInstances[i];
				GlowingBarrelExtension.MaterialPool.GetOrAdd(material, () => new Queue<Material>()).Enqueue(material2);
			}
		}

		private void Start()
		{
			this._materialCount = this._materialsToCopy.Length;
			this._materialTemplates = new Material[this._materialCount];
			this._materialInstances = new Material[this._materialCount];
			for (int i = 0; i < this._materialCount; i++)
			{
				GlowingBarrelExtension.AffectedMaterial affectedMaterial = this._materialsToCopy[i];
				Material sharedMaterial = affectedMaterial.Renderers[0].sharedMaterial;
				Queue<Material> queue;
				Material material;
				Material material2;
				if (GlowingBarrelExtension.MaterialPool.TryGetValue(sharedMaterial, out queue) && queue.TryDequeue(out material))
				{
					material2 = material;
				}
				else
				{
					material2 = new Material(sharedMaterial);
				}
				this._materialTemplates[i] = sharedMaterial;
				this._materialInstances[i] = material2;
				for (int j = 0; j < affectedMaterial.Renderers.Length; j++)
				{
					affectedMaterial.Renderers[j].sharedMaterial = material2;
				}
			}
		}

		private static readonly Dictionary<Material, Queue<Material>> MaterialPool = new Dictionary<Material, Queue<Material>>();

		private static readonly int TemperatureHash = Shader.PropertyToID("_Temperature");

		[SerializeField]
		private GlowingBarrelExtension.AffectedMaterial[] _materialsToCopy;

		private Material[] _materialTemplates;

		private Material[] _materialInstances;

		private int _materialCount;

		[Serializable]
		private struct AffectedMaterial
		{
			public Renderer[] Renderers;
		}
	}
}
