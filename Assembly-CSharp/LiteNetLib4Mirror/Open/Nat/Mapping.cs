using System;
using System.Net;

namespace LiteNetLib4Mirror.Open.Nat
{
	public class Mapping
	{
		internal MappingLifetime LifetimeType { get; set; }

		public string Description { get; internal set; }

		public IPAddress PrivateIP { get; internal set; }

		public NetworkProtocolType NetworkProtocolType { get; internal set; }

		public int PrivatePort { get; internal set; }

		public IPAddress PublicIP { get; internal set; }

		public int PublicPort { get; internal set; }

		public int Lifetime
		{
			get
			{
				return this._lifetime;
			}
			internal set
			{
				if (value == 0)
				{
					this.LifetimeType = MappingLifetime.Permanent;
					this._lifetime = 0;
					this._expiration = DateTime.UtcNow;
					return;
				}
				if (value == 2147483647)
				{
					this.LifetimeType = MappingLifetime.Session;
					this._lifetime = 600;
					this._expiration = DateTime.UtcNow.AddSeconds((double)this._lifetime);
					return;
				}
				this.LifetimeType = MappingLifetime.Manual;
				this._lifetime = value;
				this._expiration = DateTime.UtcNow.AddSeconds((double)this._lifetime);
			}
		}

		public DateTime Expiration
		{
			get
			{
				return this._expiration;
			}
			internal set
			{
				this._expiration = value;
				this._lifetime = (int)(this._expiration - DateTime.UtcNow).TotalSeconds;
			}
		}

		internal Mapping(NetworkProtocolType networkProtocolType, IPAddress privateIP, int privatePort, int publicPort)
			: this(networkProtocolType, privateIP, privatePort, publicPort, 0, "LiteNetLib4Mirror.Open.Nat")
		{
		}

		public Mapping(NetworkProtocolType networkProtocolType, IPAddress privateIP, int privatePort, int publicPort, int lifetime, string description)
		{
			Guard.IsInRange(privatePort, 0, 65535, "privatePort");
			Guard.IsInRange(publicPort, 0, 65535, "publicPort");
			Guard.IsInRange(lifetime, 0, int.MaxValue, "lifetime");
			Guard.IsTrue(networkProtocolType == NetworkProtocolType.Tcp || networkProtocolType == NetworkProtocolType.Udp, "protocol");
			Guard.IsNotNull(privateIP, "privateIP");
			this.NetworkProtocolType = networkProtocolType;
			this.PrivateIP = privateIP;
			this.PrivatePort = privatePort;
			this.PublicIP = IPAddress.None;
			this.PublicPort = publicPort;
			this.Lifetime = lifetime;
			this.Description = description;
		}

		public Mapping(NetworkProtocolType networkProtocolType, int privatePort, int publicPort)
			: this(networkProtocolType, IPAddress.None, privatePort, publicPort, 0, "Open.NAT")
		{
		}

		public Mapping(NetworkProtocolType networkProtocolType, int privatePort, int publicPort, string description)
			: this(networkProtocolType, IPAddress.None, privatePort, publicPort, 0, description)
		{
		}

		public Mapping(NetworkProtocolType networkProtocolType, int privatePort, int publicPort, int lifetime, string description)
			: this(networkProtocolType, IPAddress.None, privatePort, publicPort, lifetime, description)
		{
		}

		internal Mapping(Mapping mapping)
		{
			this.PrivateIP = mapping.PrivateIP;
			this.PrivatePort = mapping.PrivatePort;
			this.NetworkProtocolType = mapping.NetworkProtocolType;
			this.PublicIP = mapping.PublicIP;
			this.PublicPort = mapping.PublicPort;
			this.LifetimeType = mapping.LifetimeType;
			this.Description = mapping.Description;
			this._lifetime = mapping._lifetime;
			this._expiration = mapping._expiration;
		}

		public bool IsExpired()
		{
			return this.LifetimeType != MappingLifetime.Permanent && this.LifetimeType != MappingLifetime.ForcedSession && this.Expiration < DateTime.UtcNow;
		}

		internal bool ShoundRenew()
		{
			return this.LifetimeType == MappingLifetime.Session && this.IsExpired();
		}

		public override bool Equals(object obj)
		{
			if (obj == null)
			{
				return false;
			}
			if (this == obj)
			{
				return true;
			}
			Mapping mapping = obj as Mapping;
			return mapping != null && this.PublicPort == mapping.PublicPort && this.PrivatePort == mapping.PrivatePort;
		}

		public override int GetHashCode()
		{
			return (((this.PublicPort * 397) ^ ((this.PrivateIP != null) ? this.PrivateIP.GetHashCode() : 0)) * 397) ^ this.PrivatePort;
		}

		public override string ToString()
		{
			return string.Format("{0} {1} --> {2}:{3} ({4})", new object[]
			{
				(this.NetworkProtocolType == NetworkProtocolType.Tcp) ? "Tcp" : "Udp",
				this.PublicPort,
				this.PrivateIP,
				this.PrivatePort,
				this.Description
			});
		}

		private DateTime _expiration;

		private int _lifetime;
	}
}
