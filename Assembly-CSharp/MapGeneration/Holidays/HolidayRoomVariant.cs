using System;

namespace MapGeneration.Holidays
{
	[Serializable]
	public struct HolidayRoomVariant : IHolidayFetchableData<SpawnableRoom>
	{
		public HolidayType Holiday { readonly get; private set; }

		public SpawnableRoom Result { readonly get; private set; }
	}
}
