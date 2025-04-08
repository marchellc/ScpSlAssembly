using System;
using System.Collections.Generic;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons
{
	public abstract class PooledOverconRenderer : OverconRendererBase
	{
		protected T GetFromPool<T>() where T : OverconBase
		{
			OverconBase overconBase;
			if (!this._queue.TryDequeue(out overconBase))
			{
				overconBase = global::UnityEngine.Object.Instantiate<OverconBase>(this._template);
			}
			overconBase.gameObject.SetActive(true);
			this._spawned.Add(overconBase);
			return overconBase as T;
		}

		protected void ReturnToPool(OverconBase instance)
		{
			this._spawned.Remove(instance);
			this._queue.Enqueue(instance);
			instance.gameObject.SetActive(false);
		}

		protected void ReturnAll()
		{
			foreach (OverconBase overconBase in this._spawned)
			{
				if (!(overconBase == null))
				{
					overconBase.gameObject.SetActive(false);
					this._queue.Enqueue(overconBase);
				}
			}
			this._spawned.Clear();
		}

		[SerializeField]
		private OverconBase _template;

		private readonly Queue<OverconBase> _queue = new Queue<OverconBase>();

		private readonly HashSet<OverconBase> _spawned = new HashSet<OverconBase>();
	}
}
