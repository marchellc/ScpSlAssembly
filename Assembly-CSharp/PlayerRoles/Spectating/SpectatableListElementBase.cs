using System;
using GameObjectPools;
using UnityEngine;

namespace PlayerRoles.Spectating
{
	public class SpectatableListElementBase : PoolObject
	{
		protected RectTransform CachedRectTransform
		{
			get
			{
				if (!this._transformCacheSet)
				{
					if (!base.TryGetComponent<RectTransform>(out this._cachedTransform))
					{
						throw new InvalidOperationException("SpectatableListElementBase of name '" + base.name + "' does not have a rect transform!");
					}
					this._transformCacheSet = true;
				}
				return this._cachedTransform;
			}
		}

		public SpectatableModuleBase Target
		{
			get
			{
				return this._target;
			}
			internal set
			{
				SpectatableModuleBase target = this._target;
				if (value != target)
				{
					this._target = value;
					this.OnTargetChanged(target, value);
				}
			}
		}

		public int Index { get; internal set; }

		public float Height
		{
			get
			{
				return this.CachedRectTransform.sizeDelta.y;
			}
		}

		public bool IsCurrent
		{
			get
			{
				return !base.Pooled && this.Target == SpectatorTargetTracker.CurrentTarget;
			}
		}

		protected virtual void OnTargetChanged(SpectatableModuleBase prevTarget, SpectatableModuleBase newTarget)
		{
		}

		public void BeginSpectating()
		{
			SpectatorTargetTracker.CurrentTarget = this.Target;
		}

		private RectTransform _cachedTransform;

		private SpectatableModuleBase _target;

		private bool _transformCacheSet;
	}
}
