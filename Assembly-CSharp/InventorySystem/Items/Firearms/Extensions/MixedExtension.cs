using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public abstract class MixedExtension : MonoBehaviour, IWorldmodelExtension, IViewmodelExtension
	{
		private protected ItemIdentifier Identifier { protected get; private set; }

		private protected FirearmWorldmodel Worldmodel { protected get; private set; }

		private protected bool WorldmodelMode { protected get; private set; }

		private protected bool ViewmodelMode { protected get; private set; }

		private protected AnimatedFirearmViewmodel Viewmodel { protected get; private set; }

		public virtual void InitViewmodel(AnimatedFirearmViewmodel viewmodel)
		{
			this.Identifier = viewmodel.ItemId;
			this.Viewmodel = viewmodel;
			this.ViewmodelMode = true;
			this.SetupAny();
		}

		public virtual void SetupWorldmodel(FirearmWorldmodel worldmodel)
		{
			this.Identifier = worldmodel.Identifier;
			this.Worldmodel = worldmodel;
			this.WorldmodelMode = true;
			this.SetupAny();
		}

		public virtual void SetupAny()
		{
		}

		protected void SetLayer(int layer)
		{
			this.SetLayer(layer, base.transform);
		}

		private void SetLayer(int layer, Transform t)
		{
			t.gameObject.layer = layer;
			int childCount = t.childCount;
			for (int i = 0; i < childCount; i++)
			{
				this.SetLayer(layer, t.GetChild(i));
			}
		}

		public const int ViewmodelLayer = 10;

		public const int Worldmodellayer = 9;
	}
}
