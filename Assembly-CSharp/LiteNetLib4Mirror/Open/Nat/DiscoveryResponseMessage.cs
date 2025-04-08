using System;
using System.Collections.Generic;
using System.Linq;

namespace LiteNetLib4Mirror.Open.Nat
{
	internal class DiscoveryResponseMessage
	{
		public DiscoveryResponseMessage(string message)
		{
			var enumerable = from h in message.Split(new string[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries).Skip(1)
				let c = h.Split(':', StringSplitOptions.None)
				let key = c[0]
				let value = (c.Length > 1) ? string.Join(":", c.Skip(1).ToArray<string>()) : string.Empty
				select new
				{
					Key = key,
					Value = value.Trim()
				};
			this._headers = enumerable.ToDictionary(x => x.Key.ToUpperInvariant(), x => x.Value);
		}

		public string this[string key]
		{
			get
			{
				return this._headers[key.ToUpperInvariant()];
			}
		}

		private readonly IDictionary<string, string> _headers;
	}
}
