using System.Diagnostics;
using Mirror;

namespace InventorySystem.Items.Firearms.Modules.Misc;

public class ClientRequestTimer
{
	private const float AdditionalTimeout = 0.25f;

	private Stopwatch _stopwatch;

	private double _timeoutDuration;

	public bool Sent { get; private set; }

	public bool Busy
	{
		get
		{
			if (this.Sent)
			{
				return !this.TimedOut;
			}
			return false;
		}
	}

	public bool TimedOut
	{
		get
		{
			if (!this.Sent)
			{
				return false;
			}
			return this._stopwatch.Elapsed.TotalSeconds > this._timeoutDuration;
		}
	}

	public void Trigger()
	{
		this.Sent = true;
		if (this._stopwatch == null)
		{
			this._stopwatch = Stopwatch.StartNew();
		}
		else
		{
			this._stopwatch.Restart();
		}
		this._timeoutDuration = NetworkTime.rtt + 0.25;
	}

	public void Reset()
	{
		this.Sent = false;
	}
}
