using System;
using InventorySystem.Items.Pickups;
using Mirror;
using Utils;

namespace InventorySystem.Searching
{
	public struct SearchSession : ISearchSession, NetworkMessage, IEquatable<SearchSession>
	{
		public ItemPickupBase Target { readonly get; set; }

		public double InitialTime { readonly get; set; }

		public double FinishTime { readonly get; set; }

		public double Duration
		{
			get
			{
				return this.FinishTime - this.InitialTime;
			}
		}

		public double Progress
		{
			get
			{
				return MoreMath.InverseLerp(this.InitialTime, this.FinishTime, NetworkTime.time);
			}
		}

		public SearchSession(double initialTime, double finishTime, ItemPickupBase target)
		{
			this.Target = target;
			this.InitialTime = initialTime;
			this.FinishTime = finishTime;
		}

		public void Deserialize(NetworkReader reader)
		{
			NetworkIdentity networkIdentity = reader.ReadNetworkIdentity();
			this.Target = ((networkIdentity == null) ? null : networkIdentity.GetComponent<ItemPickupBase>());
			this.InitialTime = reader.ReadDouble();
			this.FinishTime = reader.ReadDouble();
		}

		public void Serialize(NetworkWriter writer)
		{
			writer.WriteNetworkIdentity((this.Target == null) ? null : this.Target.netIdentity);
			writer.WriteDouble(this.InitialTime);
			writer.WriteDouble(this.FinishTime);
		}

		public bool Equals(SearchSession other)
		{
			return object.Equals(this.Target, other.Target) && this.InitialTime.Equals(other.InitialTime) && this.FinishTime.Equals(other.FinishTime);
		}

		public override bool Equals(object obj)
		{
			if (obj is SearchSession)
			{
				SearchSession searchSession = (SearchSession)obj;
				return this.Equals(searchSession);
			}
			return false;
		}

		public override int GetHashCode()
		{
			return (((((this.Target != null) ? this.Target.GetHashCode() : 0) * 397) ^ this.InitialTime.GetHashCode()) * 397) ^ this.FinishTime.GetHashCode();
		}
	}
}
