using System;
using InventorySystem.Items.Thirdperson.LayerProcessors;
using PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers;
using UnityEngine;

namespace InventorySystem.Items.Thirdperson
{
	public class IdleThirdpersonItem : ThirdpersonItemBase, IHandPoseModifier
	{
		public override ThirdpersonLayerWeight GetWeightForLayer(AnimItemLayer3p layer)
		{
			if (this._hasProcessor)
			{
				return this._layerProcessor.GetWeightForLayer(this, layer);
			}
			return new ThirdpersonLayerWeight((float)(this._fallbackLayers.Contains(layer) ? 1 : 0), true);
		}

		public virtual HandPoseData ProcessHandPose(HandPoseData data)
		{
			return this._handPoseData;
		}

		internal override void Initialize(InventorySubcontroller sctrl, ItemIdentifier id)
		{
			base.Initialize(sctrl, id);
			this._hasProcessor = this._layerProcessor != null;
			base.SetAnim(AnimState3p.Override0, this._idleOverride);
		}

		protected bool TryGetLayerProcessor<T>(out T processor) where T : LayerProcessorBase
		{
			T t = this._layerProcessor as T;
			if (t != null)
			{
				processor = t;
				return true;
			}
			processor = default(T);
			return false;
		}

		private bool _hasProcessor;

		[SerializeField]
		private AnimationClip _idleOverride;

		[SerializeField]
		private HandPoseData _handPoseData;

		[SerializeField]
		private LayerProcessorBase _layerProcessor;

		[Tooltip("Not used when layer processor is set.")]
		[SerializeField]
		private AnimItemLayer3p[] _fallbackLayers;
	}
}
