using System.Collections.Generic;
using Mirror;
using Respawning.Objectives;

namespace Respawning;

public struct ObjectiveCompletionMessage : NetworkMessage
{
	private readonly int _index;

	public FactionObjectiveBase Objective { get; private set; }

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
		if (!FactionInfluenceManager.Objectives.TryGet(this._index, out var element))
		{
			throw new KeyNotFoundException($"Failed to get objective of index: {this._index}.");
		}
		this.Objective = element;
		this.Objective.ClientReadRpc(reader);
	}

	public void Write(NetworkWriter writer)
	{
		writer.WriteInt(this._index);
		if (NetworkServer.active)
		{
			this.Objective.ServerWriteRpc(writer);
		}
	}
}
