using System.Collections.Generic;
using UnityEngine;

namespace Decals;

public class DecalPool
{
	private const int StartQueueCapacity = 5000;

	private readonly Queue<Decal> _spawned;

	private readonly Queue<Decal> _disabled;

	private readonly Decal _template;

	public readonly DecalPoolType Type;

	public int Instances => this._spawned.Count;

	private Decal PrepareInstance(Decal inst)
	{
		if (inst == null)
		{
			inst = Object.Instantiate(this._template);
		}
		else
		{
			inst.gameObject.SetActive(value: true);
		}
		return inst;
	}

	public Decal Get()
	{
		Decal decal = ((!this._disabled.TryDequeue(out decal)) ? Object.Instantiate(this._template) : this.PrepareInstance(decal));
		this._spawned.Enqueue(decal);
		return decal;
	}

	public void DisableLast()
	{
		if (this._spawned.TryDequeue(out var result) && !(result == null))
		{
			result.gameObject.SetActive(value: false);
			this._disabled.Enqueue(result);
		}
	}

	public void SetLimit(int limit)
	{
		while (this._spawned.Count > limit)
		{
			Decal decal = this._spawned.Dequeue();
			if (!(decal == null))
			{
				decal.Detach();
				Object.Destroy(decal.gameObject);
			}
		}
	}

	public DecalPool(Decal template)
	{
		this.Type = template.DecalPoolType;
		this._template = template;
		this._spawned = new Queue<Decal>(5000);
		this._disabled = new Queue<Decal>(5000);
	}
}
