using System;
using System.Diagnostics;
using Mirror;

namespace InventorySystem.Items.Firearms.Modules.Misc
{
	public class ClientRequestTimer
	{
		public bool Sent { get; private set; }

		public bool Busy
		{
			get
			{
				return this.Sent && !this.TimedOut;
			}
		}

		public bool TimedOut
		{
			get
			{
				return this.Sent && this._stopwatch.Elapsed.TotalSeconds > this._timeoutDuration;
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

		private const float AdditionalTimeout = 0.25f;

		private Stopwatch _stopwatch;

		private double _timeoutDuration;
	}
}
