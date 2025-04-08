using System;
using UnityEngine;

namespace MapGeneration.Holidays
{
	public class HolidayMaterialReplacer : MonoBehaviour
	{
		private void Start()
		{
			Material material;
			if (!this._materials.TryGetResult(out material))
			{
				return;
			}
			this._renderer.material = material;
		}

		[SerializeField]
		private Renderer _renderer;

		[SerializeField]
		private HolidayMaterialReplacer.MaterialReplacement[] _materials;

		[Serializable]
		private struct MaterialReplacement : IHolidayFetchableData<Material>
		{
			public HolidayType Holiday { readonly get; private set; }

			public Material Result { readonly get; private set; }
		}
	}
}
