using System.Collections.Generic;
using NorthwoodLib.Pools;
using UnityEngine;

public class RealisticLoadingBar
{
	private float _lastProgress;

	private readonly Queue<Vector2> _queue;

	public float Progress
	{
		get
		{
			while (this._queue.Count > 0 && this._queue.Peek().x <= Time.realtimeSinceStartup)
			{
				this._lastProgress += this._queue.Dequeue().y;
			}
			if (this._queue.Count != 0)
			{
				return this._lastProgress;
			}
			return 1f;
		}
	}

	public RealisticLoadingBar(float targetTime, int numberOfSteps, float maxStepSizeVar, float maxTickVar)
	{
		this._lastProgress = 0f;
		this._queue = new Queue<Vector2>();
		List<Vector2> list = ListPool<Vector2>.Shared.Rent();
		float num = 0f;
		float num2 = 0f;
		for (int i = 0; i < numberOfSteps; i++)
		{
			float num3 = Random.Range(1f, maxTickVar);
			num += num3;
			float num4 = Random.Range(1f, maxStepSizeVar);
			num2 += num4;
			list.Add(new Vector2(num, num4));
		}
		float num5 = num / targetTime;
		foreach (Vector2 item in list)
		{
			this._queue.Enqueue(new Vector2(Time.realtimeSinceStartup + item.x / num5, item.y / num2));
		}
	}
}
