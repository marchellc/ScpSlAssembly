using System;
using Mirror;

namespace PlayerStatsSystem
{
	public static class DamageHandlerReaderWriter
	{
		public static void WriteDamageHandler(this NetworkWriter writer, DamageHandlerBase info)
		{
			writer.WriteByte(DamageHandlers.IdsByTypeHash[info.GetType().FullName.GetStableHashCode()]);
			info.WriteAdditionalData(writer);
		}

		public static DamageHandlerBase ReadDamageHandler(this NetworkReader reader)
		{
			byte b = reader.ReadByte();
			Func<DamageHandlerBase> func;
			if (!DamageHandlers.ConstructorsById.TryGetValue(b, out func))
			{
				throw new InvalidOperationException("DamageType " + b.ToString() + " does not have a defined handler!");
			}
			DamageHandlerBase damageHandlerBase = func();
			damageHandlerBase.ReadAdditionalData(reader);
			return damageHandlerBase;
		}
	}
}
