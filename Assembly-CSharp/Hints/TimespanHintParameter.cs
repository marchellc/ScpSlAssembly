using System;
using Mirror;

namespace Hints
{
	public class TimespanHintParameter : DoubleHintParameter
	{
		public new static TimespanHintParameter FromNetwork(NetworkReader reader)
		{
			TimespanHintParameter timespanHintParameter = new TimespanHintParameter();
			timespanHintParameter.Deserialize(reader);
			return timespanHintParameter;
		}

		public static TimespanHintParameter FromOffset(double offset, string format, bool negate)
		{
			return new TimespanHintParameter(NetworkTime.time + offset, format, negate);
		}

		public static TimespanHintParameter FromOffset(TimeSpan offset, string format, bool negate)
		{
			return TimespanHintParameter.FromOffset(offset.TotalSeconds, format, negate);
		}

		private protected bool Negate { protected get; private set; }

		protected TimeSpan OffsetTime
		{
			get
			{
				TimeSpan timeSpan = TimeSpan.FromSeconds(base.Value - NetworkTime.time);
				if (!this.Negate)
				{
					return timeSpan;
				}
				return timeSpan.Negate();
			}
		}

		protected TimespanHintParameter()
		{
		}

		public TimespanHintParameter(double sourceTime, string format, bool negate)
			: base(sourceTime, format)
		{
			this.Negate = negate;
		}

		public TimespanHintParameter(DateTimeOffset sourceTime, string format, bool negate)
			: this((sourceTime - DateTimeOffset.UtcNow).TotalSeconds, format, negate)
		{
		}

		public override void Deserialize(NetworkReader reader)
		{
			base.Deserialize(reader);
			this.Negate = reader.ReadBool();
		}

		public override void Serialize(NetworkWriter writer)
		{
			base.Serialize(writer);
			writer.WriteBool(this.Negate);
		}
	}
}
