using System;
using Mirror;

namespace Hints
{
	public abstract class IdHintParameter : HintParameter
	{
		private protected int Id { protected get; private set; }

		protected IdHintParameter()
		{
		}

		protected IdHintParameter(byte id)
		{
			this.Id = (int)id;
		}

		public override void Deserialize(NetworkReader reader)
		{
			this.Id = reader.ReadInt();
		}

		public override void Serialize(NetworkWriter writer)
		{
			writer.WriteInt(this.Id);
		}
	}
}
