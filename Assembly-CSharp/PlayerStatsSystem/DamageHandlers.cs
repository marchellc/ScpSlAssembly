using System;
using System.Collections.Generic;
using Footprinting;
using Mirror;
using PlayerRoles.PlayableScps.Scp1507;
using PlayerRoles.PlayableScps.Scp3114;
using PlayerRoles.PlayableScps.Scp939;
using UnityEngine;

namespace PlayerStatsSystem
{
	public static class DamageHandlers
	{
		[RuntimeInitializeOnLoadMethod]
		private static void PrepDictionaries()
		{
			byte b = 0;
			foreach (Func<DamageHandlerBase> func in DamageHandlers.DefinedConstructors)
			{
				DamageHandlers.IdsByTypeHash.Add(func().GetType().FullName.GetStableHashCode(), b);
				DamageHandlers.ConstructorsById.Add(b, func);
				b += 1;
			}
		}

		public static readonly Func<DamageHandlerBase>[] DefinedConstructors = new Func<DamageHandlerBase>[]
		{
			() => new RecontainmentDamageHandler(default(Footprint)),
			() => new FirearmDamageHandler(),
			() => new WarheadDamageHandler(),
			() => new UniversalDamageHandler(),
			() => new ScpDamageHandler(),
			() => new Scp096DamageHandler(),
			() => new Scp049DamageHandler(),
			() => new MicroHidDamageHandler(null, 0f),
			() => new CustomReasonDamageHandler(string.Empty),
			() => new ExplosionDamageHandler(default(Footprint), Vector3.zero, 0f, 0, ExplosionType.Grenade),
			() => new Scp018DamageHandler(null, 0f, false),
			() => new DisruptorDamageHandler(null, default(Vector3), 0f),
			() => new JailbirdDamageHandler(),
			() => new Scp939DamageHandler(null, 0f, Scp939DamageType.None),
			() => new Scp3114DamageHandler(),
			() => new Scp1507DamageHandler(default(Footprint), 0f),
			() => new Scp956DamageHandler(Vector3.zero),
			() => new SnowballDamageHandler()
		};

		public static readonly Dictionary<byte, Func<DamageHandlerBase>> ConstructorsById = new Dictionary<byte, Func<DamageHandlerBase>>();

		public static readonly Dictionary<int, byte> IdsByTypeHash = new Dictionary<int, byte>();
	}
}
