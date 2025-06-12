using System;
using Mirror;

namespace InventorySystem.Searching;

public struct SearchRequest : ISearchSession, NetworkMessage, IEquatable<SearchRequest>
{
	private SearchSession _body;

	public byte Id { get; private set; }

	public SearchSession Body => this._body;

	public ISearchable Target
	{
		get
		{
			return this._body.Target;
		}
		set
		{
			this._body.Target = value;
		}
	}

	public double InitialTime
	{
		get
		{
			return this._body.InitialTime;
		}
		set
		{
			this._body.InitialTime = value;
		}
	}

	public double FinishTime
	{
		get
		{
			return this._body.FinishTime;
		}
		set
		{
			this._body.FinishTime = value;
		}
	}

	public double Progress => this._body.Progress;

	public void Deserialize(NetworkReader reader)
	{
		this.Id = reader.ReadByte();
		this._body.Deserialize(reader);
	}

	public void Serialize(NetworkWriter writer)
	{
		writer.WriteByte(this.Id);
		this._body.Serialize(writer);
	}

	public bool Equals(SearchRequest other)
	{
		if (this.Body.Equals(other.Body))
		{
			return this.Id == other.Id;
		}
		return false;
	}

	public override bool Equals(object obj)
	{
		if (obj is SearchRequest other)
		{
			return this.Equals(other);
		}
		return false;
	}

	public override int GetHashCode()
	{
		return (this.Body.GetHashCode() * 397) ^ this.Id.GetHashCode();
	}
}
