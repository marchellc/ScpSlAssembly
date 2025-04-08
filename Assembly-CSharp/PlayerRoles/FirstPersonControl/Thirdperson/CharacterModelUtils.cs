using System;
using System.Collections.Generic;
using PlayerRoles.Ragdolls;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson
{
	public static class CharacterModelUtils
	{
		public static void ConvertRagdoll(BasicRagdoll ragdoll, Func<Renderer, List<Material>, bool> filter, Material template, List<Material> appliedMaterials)
		{
			appliedMaterials.Clear();
			CharacterModelUtils.RenderersNonAlloc.Clear();
			ragdoll.GetComponentsInChildren<Renderer>(true, CharacterModelUtils.RenderersNonAlloc);
			foreach (Renderer renderer in CharacterModelUtils.RenderersNonAlloc)
			{
				if (!(renderer is ParticleSystemRenderer))
				{
					renderer.GetSharedMaterials(CharacterModelUtils.MaterialsNonAlloc);
					if (filter == null || filter(renderer, CharacterModelUtils.MaterialsNonAlloc))
					{
						int count = CharacterModelUtils.MaterialsNonAlloc.Count;
						Material[] array = new Material[count];
						for (int i = 0; i < count; i++)
						{
							array[i] = CharacterModelUtils.ConvertShader(renderer.sharedMaterials[i], template);
						}
						renderer.materials = array;
						appliedMaterials.AddRange(array);
					}
				}
			}
		}

		public static Material ConvertShader(Material original, Material shaderTemplate)
		{
			int[] orAdd = CharacterModelUtils.TextureHashes.GetOrAdd(shaderTemplate, () => shaderTemplate.GetTexturePropertyNameIDs());
			Material material = new Material(shaderTemplate);
			CharacterModelUtils.CopyProperties(orAdd, original, material, (Material og, int hash) => og.HasTexture(hash), delegate(Material og, int hash, Material res)
			{
				res.SetTexture(hash, og.GetTexture(hash));
			});
			CharacterModelUtils.CopyProperties(CharacterModelUtils.FloatsToCopy, original, material, (Material og, int hash) => og.HasFloat(hash), delegate(Material og, int hash, Material res)
			{
				res.SetFloat(hash, og.GetFloat(hash));
			});
			return material;
		}

		private static void CopyProperties(int[] ids, Material original, Material result, Func<Material, int, bool> hasChecker, Action<Material, int, Material> applier)
		{
			foreach (int num in ids)
			{
				if (hasChecker(original, num))
				{
					applier(original, num, result);
				}
			}
		}

		private static readonly Dictionary<Material, int[]> TextureHashes = new Dictionary<Material, int[]>();

		private static readonly List<Renderer> RenderersNonAlloc = new List<Renderer>();

		private static readonly List<Material> MaterialsNonAlloc = new List<Material>();

		private static readonly int[] FloatsToCopy = new int[] { Shader.PropertyToID("_UseMaskMap") };
	}
}
