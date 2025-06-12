using RelativePositioning;
using UnityEngine;

public class MovementTracer
{
	public byte Clock;

	public readonly RelativePosition[] Positions;

	private readonly byte _size;

	private readonly byte _cooldown;

	private readonly float _tpDis;

	private byte _cooldownTimer;

	public MovementTracer(byte size, byte cooldown, float teleportDistance)
	{
		this._size = size;
		this.Positions = new RelativePosition[size];
		this._cooldown = cooldown;
		this._tpDis = teleportDistance;
		this._cooldownTimer = 0;
	}

	public void Record(Vector3 plyPosition)
	{
		if (this._cooldownTimer > 0)
		{
			this._cooldownTimer--;
			return;
		}
		if (++this.Clock >= this._size)
		{
			this.Clock = 0;
		}
		this.Positions[this.Clock] = new RelativePosition(plyPosition);
		this._cooldownTimer = this._cooldown;
	}

	public Bounds GenerateBounds(float time, bool ignoreTeleports)
	{
		int num = Mathf.FloorToInt(time / Time.fixedDeltaTime / (float)(this._cooldown + 1));
		if (num <= 0)
		{
			Debug.LogError($"MovementTracer was requested to generate Bounds for the last {time} seconds, but it's too short. Please access PlayerMovementSync.RealModelPosition directly.");
			num = 1;
		}
		else if (num > this._size)
		{
			Debug.LogError($"MovementTracer was requested to generate Bounds for the last {time} seconds, but it can't keep track of positions after {((float)(int)this._cooldown + 1f) * (float)(int)this._size * Time.fixedDeltaTime}");
			num = this._size;
		}
		Bounds result = new Bounds(this.Positions[this.Clock].Position, Vector3.zero);
		for (int i = 1; i < num; i++)
		{
			int num2 = this.Clock - i;
			if (num2 < 0)
			{
				num2 += this._size - 1;
			}
			Vector3 position = this.Positions[num2].Position;
			if (!ignoreTeleports && Vector3.Distance(result.ClosestPoint(position), position) > this._tpDis)
			{
				break;
			}
			result.Encapsulate(position);
		}
		return result;
	}
}
