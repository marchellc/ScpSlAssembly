using System.Collections.Generic;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079.Overcons;

public abstract class PooledOverconRenderer : OverconRendererBase
{
	[SerializeField]
	private OverconBase _template;

	private readonly Queue<OverconBase> _queue = new Queue<OverconBase>();

	private readonly HashSet<OverconBase> _spawned = new HashSet<OverconBase>();

	protected T GetFromPool<T>() where T : OverconBase
	{
		if (!this._queue.TryDequeue(out var result))
		{
			result = Object.Instantiate(this._template);
		}
		result.gameObject.SetActive(value: true);
		this._spawned.Add(result);
		return result as T;
	}

	protected void ReturnToPool(OverconBase instance)
	{
		this._spawned.Remove(instance);
		this._queue.Enqueue(instance);
		instance.gameObject.SetActive(value: false);
	}

	protected void ReturnAll()
	{
		foreach (OverconBase item in this._spawned)
		{
			if (!(item == null))
			{
				item.gameObject.SetActive(value: false);
				this._queue.Enqueue(item);
			}
		}
		this._spawned.Clear();
	}
}
