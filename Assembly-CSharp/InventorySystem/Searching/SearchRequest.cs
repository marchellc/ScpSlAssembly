using System;
using InventorySystem.Items.Pickups;
using Mirror;

namespace InventorySystem.Searching
{
	public struct SearchRequest : ISearchSession, NetworkMessage, IEquatable<SearchRequest>
	{
		public byte Id { readonly get; private set; }

		public SearchSession Body
		{
			get
			{
				return this._body;
			}
		}

		public ItemPickupBase Target
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

		public double Progress
		{
			get
			{
				return this._body.Progress;
			}
		}

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
			return this.Body.Equals(other.Body) && this.Id == other.Id;
		}

		public override bool Equals(object obj)
		{
			if (obj is SearchRequest)
			{
				SearchRequest searchRequest = (SearchRequest)obj;
				return this.Equals(searchRequest);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (this.Body.GetHashCode() * 397) ^ this.Id.GetHashCode();
		}

		private SearchSession _body;
	}
}
