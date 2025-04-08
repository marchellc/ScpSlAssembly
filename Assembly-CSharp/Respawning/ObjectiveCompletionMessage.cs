using System;
using System.Collections.Generic;
using Mirror;
using Respawning.Objectives;

namespace Respawning
{
	public struct ObjectiveCompletionMessage : NetworkMessage
	{
		public FactionObjectiveBase Objective { readonly get; private set; }

		public ObjectiveCompletionMessage(FactionObjectiveBase objective)
		{
			this._index = FactionInfluenceManager.Objectives.IndexOf(objective);
			this.Objective = objective;
		}

		public ObjectiveCompletionMessage(int index)
		{
			this._index = index;
			this.Objective = FactionInfluenceManager.Objectives[this._index];
		}

		public ObjectiveCompletionMessage(NetworkReader reader)
		{
			this._index = reader.ReadInt();
			FactionObjectiveBase factionObjectiveBase;
			if (!FactionInfluenceManager.Objectives.TryGet(this._index, out factionObjectiveBase))
			{
				throw new KeyNotFoundException(string.Format("Failed to get objective of index: {0}.", this._index));
			}
			this.Objective = factionObjectiveBase;
			this.Objective.ClientReadRpc(reader);
		}

		public void Write(NetworkWriter writer)
		{
			writer.WriteInt(this._index);
			if (!NetworkServer.active)
			{
				return;
			}
			this.Objective.ServerWriteRpc(writer);
		}

		private readonly int _index;
	}
}
