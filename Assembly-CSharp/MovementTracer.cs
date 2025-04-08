using System;
using RelativePositioning;
using UnityEngine;

public class MovementTracer
{
	public MovementTracer(byte size, byte cooldown, float teleportDistance)
	{
		this._size = size;
		this.Positions = new RelativePosition[(int)size];
		this._cooldown = cooldown;
		this._tpDis = teleportDistance;
		this._cooldownTimer = 0;
	}

	public void Record(Vector3 plyPosition)
	{
		if (this._cooldownTimer > 0)
		{
			this._cooldownTimer -= 1;
			return;
		}
		byte b = this.Clock + 1;
		this.Clock = b;
		if (b >= this._size)
		{
			this.Clock = 0;
		}
		this.Positions[(int)this.Clock] = new RelativePosition(plyPosition);
		this._cooldownTimer = this._cooldown;
	}

	public Bounds GenerateBounds(float time, bool ignoreTeleports)
	{
		int num = Mathf.FloorToInt(time / Time.fixedDeltaTime / (float)(this._cooldown + 1));
		if (num <= 0)
		{
			Debug.LogError(string.Format("MovementTracer was requested to generate Bounds for the last {0} seconds, but it's too short. Please access PlayerMovementSync.RealModelPosition directly.", time));
			num = 1;
		}
		else if (num > (int)this._size)
		{
			Debug.LogError(string.Format("MovementTracer was requested to generate Bounds for the last {0} seconds, but it can't keep track of positions after {1}", time, ((float)this._cooldown + 1f) * (float)this._size * Time.fixedDeltaTime));
			num = (int)this._size;
		}
		Bounds bounds = new Bounds(this.Positions[(int)this.Clock].Position, Vector3.zero);
		for (int i = 1; i < num; i++)
		{
			int num2 = (int)this.Clock - i;
			if (num2 < 0)
			{
				num2 += (int)(this._size - 1);
			}
			Vector3 position = this.Positions[num2].Position;
			if (!ignoreTeleports && Vector3.Distance(bounds.ClosestPoint(position), position) > this._tpDis)
			{
				break;
			}
			bounds.Encapsulate(position);
		}
		return bounds;
	}

	public byte Clock;

	public readonly RelativePosition[] Positions;

	private readonly byte _size;

	private readonly byte _cooldown;

	private readonly float _tpDis;

	private byte _cooldownTimer;
}
