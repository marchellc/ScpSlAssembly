using System;
using PlayerRoles.FirstPersonControl.Thirdperson;
using UnityEngine;

namespace InventorySystem.Items.Thirdperson.LayerProcessors
{
	public abstract class LayerProcessorBase : MonoBehaviour
	{
		private protected ThirdpersonItemBase Source { protected get; private set; }

		public AnimatedCharacterModel TargetModel
		{
			get
			{
				return this.Source.TargetModel;
			}
		}

		protected Animator Animator
		{
			get
			{
				return this.TargetModel.Animator;
			}
		}

		protected ReferenceHub OwnerHub
		{
			get
			{
				return this.TargetModel.OwnerHub;
			}
		}

		public ThirdpersonLayerWeight GetWeightForLayer(ThirdpersonItemBase source, AnimItemLayer3p layer)
		{
			if (!this._alreadyInit)
			{
				this.Init(source);
				this._alreadyInit = true;
			}
			return this.GetWeightForLayer(layer);
		}

		protected virtual void Init(ThirdpersonItemBase source)
		{
			this.Source = source;
		}

		protected abstract ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer);

		private bool _alreadyInit;
	}
}
