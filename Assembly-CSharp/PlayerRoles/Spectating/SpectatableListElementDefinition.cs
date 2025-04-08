using System;
using GameObjectPools;
using UnityEngine;

namespace PlayerRoles.Spectating
{
	[Serializable]
	public struct SpectatableListElementDefinition
	{
		public bool TryGetFromPools(Transform parent, out SpectatableListElementBase full, out SpectatableListElementBase compact)
		{
			if (this.TrySpawn(parent, this.FullSize, out full) && this.TrySpawn(parent, this.Compact, out compact))
			{
				return true;
			}
			full = null;
			compact = null;
			return false;
		}

		private bool TrySpawn(Transform parent, SpectatableListElementBase template, out SpectatableListElementBase instance)
		{
			PoolObject poolObject;
			if (PoolManager.Singleton.TryGetPoolObject(template.gameObject, parent, out poolObject, true))
			{
				instance = poolObject as SpectatableListElementBase;
				return true;
			}
			instance = null;
			return false;
		}

		public SpectatableListElementType Type;

		public SpectatableListElementBase FullSize;

		public SpectatableListElementBase Compact;
	}
}
