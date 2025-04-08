using System;
using System.Diagnostics;
using ProgressiveCulling;
using UnityEngine;

namespace PlayerRoles.FirstPersonControl.Thirdperson.Subcontrollers
{
	public class CullingSubcontroller : CullableSimpleDynamic, IAnimatedModelSubcontroller
	{
		public event Action OnAnimatorUpdated;

		public event Action OnBeforeAnimatorUpdated;

		public bool AnimCulled { get; private set; }

		public void Init(AnimatedCharacterModel model, int index)
		{
			if (model.ThreadmillEnabled)
			{
				return;
			}
			this._model = model;
			model.OnPlayerMoved += this.OnPlyMoved;
			model.Animator.enabled = false;
		}

		private void EvaluateCulling(out bool allowCulling)
		{
			float footstepLoudnessDistance = this._model.FootstepLoudnessDistance;
			float num = footstepLoudnessDistance * footstepLoudnessDistance;
			Vector3 position = this._model.CachedTransform.position;
			Vector3 lastCamPosition = CullingCamera.LastCamPosition;
			allowCulling = (position - lastCamPosition).sqrMagnitude > num;
		}

		private void OnPlyMoved()
		{
			bool flag;
			this.EvaluateCulling(out flag);
			bool flag2 = base.IsCulled && flag;
			double num = (double)this._model.LastMovedDeltaT;
			if (flag2 != this.AnimCulled)
			{
				this.AnimCulled = flag2;
				if (flag2)
				{
					this._culledElapsed.Restart();
				}
				else
				{
					num += this._culledElapsed.Elapsed.TotalSeconds;
					this._culledElapsed.Reset();
				}
			}
			if (!this.AnimCulled)
			{
				Action onBeforeAnimatorUpdated = this.OnBeforeAnimatorUpdated;
				if (onBeforeAnimatorUpdated != null)
				{
					onBeforeAnimatorUpdated();
				}
				num = Math.Min(num, 5.0);
				this._model.Animator.Update((float)num);
				Action onAnimatorUpdated = this.OnAnimatorUpdated;
				if (onAnimatorUpdated == null)
				{
					return;
				}
				onAnimatorUpdated();
			}
		}

		private AnimatedCharacterModel _model;

		private readonly Stopwatch _culledElapsed = new Stopwatch();

		private const float MaxDeltaTime = 5f;
	}
}
