using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.VFX;

public static class ParticlePool
{
	public static VisualEffect GetFromPool(this VisualEffect template, float minAge = 0.1f)
	{
		return ParticlePool.GetAny<VisualEffect>(template, ParticlePool.VfxPools, (VisualEffect x) => x.aliveParticleCount > 0, minAge);
	}

	public static ParticleSystem GetFromPool(this ParticleSystem template, float minAge = 0.1f)
	{
		return ParticlePool.GetAny<ParticleSystem>(template, ParticlePool.PsPools, (ParticleSystem x) => x.isPlaying, minAge);
	}

	private static T GetAny<T>(T template, Dictionary<T, Queue<ParticlePool.PooledSystem<T>>> pool, Predicate<T> isBusy, float minElapsed) where T : Component
	{
		Queue<ParticlePool.PooledSystem<T>> orAdd = pool.GetOrAdd(template, () => new Queue<ParticlePool.PooledSystem<T>>());
		ParticlePool.PooledSystem<T> pooledSystem;
		while (orAdd.TryPeek(out pooledSystem))
		{
			if (pooledSystem.System == null)
			{
				orAdd.Dequeue();
			}
			else
			{
				if (pooledSystem.AgeSeconds >= (double)minElapsed && !isBusy(pooledSystem.System))
				{
					ParticlePool.PooledSystem<T> pooledSystem2 = orAdd.Dequeue();
					pooledSystem2.LastUseTime.Restart();
					orAdd.Enqueue(pooledSystem2);
					return pooledSystem2.System;
				}
				break;
			}
		}
		ParticlePool.PooledSystem<T> pooledSystem3 = new ParticlePool.PooledSystem<T>(template);
		orAdd.Enqueue(pooledSystem3);
		return pooledSystem3.System;
	}

	private static readonly Dictionary<VisualEffect, Queue<ParticlePool.PooledSystem<VisualEffect>>> VfxPools = new Dictionary<VisualEffect, Queue<ParticlePool.PooledSystem<VisualEffect>>>();

	private static readonly Dictionary<ParticleSystem, Queue<ParticlePool.PooledSystem<ParticleSystem>>> PsPools = new Dictionary<ParticleSystem, Queue<ParticlePool.PooledSystem<ParticleSystem>>>();

	private readonly struct PooledSystem<T> where T : Component
	{
		public double AgeSeconds
		{
			get
			{
				return this.LastUseTime.Elapsed.TotalSeconds;
			}
		}

		public PooledSystem(T template)
		{
			this.System = global::UnityEngine.Object.Instantiate<T>(template);
			this.LastUseTime = Stopwatch.StartNew();
		}

		public readonly T System;

		public readonly Stopwatch LastUseTime;
	}
}
