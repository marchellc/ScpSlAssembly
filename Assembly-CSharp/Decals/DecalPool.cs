using System;
using System.Collections.Generic;
using UnityEngine;

namespace Decals
{
	public class DecalPool
	{
		public int Instances
		{
			get
			{
				return this._spawned.Count;
			}
		}

		private Decal PrepareInstance(Decal inst)
		{
			if (inst == null)
			{
				inst = global::UnityEngine.Object.Instantiate<Decal>(this._template);
			}
			else
			{
				inst.gameObject.SetActive(true);
			}
			return inst;
		}

		public Decal Get()
		{
			Decal decal;
			if (this._disabled.TryDequeue(out decal))
			{
				decal = this.PrepareInstance(decal);
			}
			else
			{
				decal = global::UnityEngine.Object.Instantiate<Decal>(this._template);
			}
			this._spawned.Enqueue(decal);
			return decal;
		}

		public void DisableLast()
		{
			Decal decal;
			if (!this._spawned.TryDequeue(out decal))
			{
				return;
			}
			if (decal == null)
			{
				return;
			}
			decal.gameObject.SetActive(false);
			this._disabled.Enqueue(decal);
		}

		public void SetLimit(int limit)
		{
			while (this._spawned.Count > limit)
			{
				Decal decal = this._spawned.Dequeue();
				if (!(decal == null))
				{
					decal.Detach();
					global::UnityEngine.Object.Destroy(decal.gameObject);
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

		private const int StartQueueCapacity = 5000;

		private readonly Queue<Decal> _spawned;

		private readonly Queue<Decal> _disabled;

		private readonly Decal _template;

		public readonly DecalPoolType Type;
	}
}
