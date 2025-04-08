using System;
using System.Collections.Generic;
using InventorySystem.Items.Firearms.Extensions;
using Mirror;
using RelativePositioning;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	public abstract class TracerBase : MonoBehaviour
	{
		public Vector3 OriginPosition
		{
			get
			{
				if (this._origin != null)
				{
					return this._origin.Value;
				}
				BarrelTipExtension barrelTipExtension;
				if (BarrelTipExtension.TryFindWorldmodelBarrelTip(this.Serial, out barrelTipExtension))
				{
					this._origin = new Vector3?(barrelTipExtension.WorldspacePosition);
				}
				else
				{
					this._origin = new Vector3?(this._fallbackOrigin);
				}
				return this._origin.Value;
			}
		}

		public RelativePosition RelativeHitPosition { get; private set; }

		public ushort Serial { get; private set; }

		public Firearm Template { get; private set; }

		protected abstract bool IsBusy { get; }

		protected virtual void OnCreated()
		{
		}

		protected virtual void OnDequeued()
		{
		}

		protected abstract void OnFired(NetworkReader extraData);

		public virtual void ServerWriteExtraData(Firearm firearm, NetworkWriter writer)
		{
		}

		public void Fire(RelativePosition hitPosition, ushort serial, Vector3 fallbackOriginPosition, NetworkReader extraData, Firearm template)
		{
			if (!this._isInstance)
			{
				throw new InvalidOperationException("Attempting to fire a template or non-pooled tracer. Please get an instance using the GetFromPool method.");
			}
			this.RelativeHitPosition = hitPosition;
			this.Serial = serial;
			this.Template = template;
			this._origin = null;
			this._fallbackOrigin = fallbackOriginPosition;
			this.OnFired(extraData);
		}

		public TracerBase GetFromPool()
		{
			if (this._isInstance)
			{
				throw new InvalidOperationException("GetFromPool can only be called on the prefab tracer object.");
			}
			if (this._instancesPool == null)
			{
				this._instancesPool = new Queue<TracerBase>();
			}
			TracerBase tracerBase;
			while (this._instancesPool.TryPeek(out tracerBase))
			{
				if (tracerBase == null)
				{
					this._instancesPool.Dequeue();
				}
				else
				{
					if (!tracerBase.IsBusy)
					{
						TracerBase tracerBase2 = this._instancesPool.Dequeue();
						tracerBase2.OnDequeued();
						this._instancesPool.Enqueue(tracerBase2);
						return tracerBase2;
					}
					break;
				}
			}
			TracerBase tracerBase3 = global::UnityEngine.Object.Instantiate<TracerBase>(this);
			this._instancesPool.Enqueue(tracerBase3);
			tracerBase3._isInstance = true;
			tracerBase3.OnCreated();
			return tracerBase3;
		}

		private Queue<TracerBase> _instancesPool;

		private bool _isInstance;

		private Vector3? _origin;

		private Vector3 _fallbackOrigin;
	}
}
