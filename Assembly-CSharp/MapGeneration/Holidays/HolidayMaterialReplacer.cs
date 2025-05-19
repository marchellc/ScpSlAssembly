using System;
using UnityEngine;

namespace MapGeneration.Holidays;

public class HolidayMaterialReplacer : MonoBehaviour
{
	[Serializable]
	private struct MaterialReplacement : IHolidayFetchableData<Material>
	{
		[field: SerializeField]
		public HolidayType Holiday { get; private set; }

		[field: SerializeField]
		public Material Result { get; private set; }
	}

	[SerializeField]
	private Renderer _renderer;

	[SerializeField]
	private MaterialReplacement[] _materials;

	private void Start()
	{
		if (_materials.TryGetResult<MaterialReplacement, Material>(out var result))
		{
			_renderer.material = result;
		}
	}
}
