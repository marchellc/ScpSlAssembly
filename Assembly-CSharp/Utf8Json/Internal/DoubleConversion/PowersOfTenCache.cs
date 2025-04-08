using System;

namespace Utf8Json.Internal.DoubleConversion
{
	internal static class PowersOfTenCache
	{
		public static void GetCachedPowerForBinaryExponentRange(int min_exponent, int max_exponent, out DiyFp power, out int decimal_exponent)
		{
			int num = 64;
			double num2 = Math.Ceiling((double)(min_exponent + num - 1) * 0.30102999566398114);
			int num3 = (348 + (int)num2 - 1) / 8 + 1;
			CachedPower cachedPower = PowersOfTenCache.kCachedPowers[num3];
			decimal_exponent = (int)cachedPower.decimal_exponent;
			power = new DiyFp(cachedPower.significand, (int)cachedPower.binary_exponent);
		}

		public static void GetCachedPowerForDecimalExponent(int requested_exponent, out DiyFp power, out int found_exponent)
		{
			int num = (requested_exponent + 348) / 8;
			CachedPower cachedPower = PowersOfTenCache.kCachedPowers[num];
			power = new DiyFp(cachedPower.significand, (int)cachedPower.binary_exponent);
			found_exponent = (int)cachedPower.decimal_exponent;
		}

		private static readonly CachedPower[] kCachedPowers = new CachedPower[]
		{
			new CachedPower(18054884314459144840UL, -1220, -348),
			new CachedPower(13451937075301367670UL, -1193, -340),
			new CachedPower(10022474136428063862UL, -1166, -332),
			new CachedPower(14934650266808366570UL, -1140, -324),
			new CachedPower(11127181549972568877UL, -1113, -316),
			new CachedPower(16580792590934885855UL, -1087, -308),
			new CachedPower(12353653155963782858UL, -1060, -300),
			new CachedPower(18408377700990114895UL, -1034, -292),
			new CachedPower(13715310171984221708UL, -1007, -284),
			new CachedPower(10218702384817765436UL, -980, -276),
			new CachedPower(15227053142812498563UL, -954, -268),
			new CachedPower(11345038669416679861UL, -927, -260),
			new CachedPower(16905424996341287883UL, -901, -252),
			new CachedPower(12595523146049147757UL, -874, -244),
			new CachedPower(9384396036005875287UL, -847, -236),
			new CachedPower(13983839803942852151UL, -821, -228),
			new CachedPower(10418772551374772303UL, -794, -220),
			new CachedPower(15525180923007089351UL, -768, -212),
			new CachedPower(11567161174868858868UL, -741, -204),
			new CachedPower(17236413322193710309UL, -715, -196),
			new CachedPower(12842128665889583758UL, -688, -188),
			new CachedPower(9568131466127621947UL, -661, -180),
			new CachedPower(14257626930069360058UL, -635, -172),
			new CachedPower(10622759856335341974UL, -608, -164),
			new CachedPower(15829145694278690180UL, -582, -156),
			new CachedPower(11793632577567316726UL, -555, -148),
			new CachedPower(17573882009934360870UL, -529, -140),
			new CachedPower(13093562431584567480UL, -502, -132),
			new CachedPower(9755464219737475723UL, -475, -124),
			new CachedPower(14536774485912137811UL, -449, -116),
			new CachedPower(10830740992659433045UL, -422, -108),
			new CachedPower(16139061738043178685UL, -396, -100),
			new CachedPower(12024538023802026127UL, -369, -92),
			new CachedPower(17917957937422433684UL, -343, -84),
			new CachedPower(13349918974505688015UL, -316, -76),
			new CachedPower(9946464728195732843UL, -289, -68),
			new CachedPower(14821387422376473014UL, -263, -60),
			new CachedPower(11042794154864902060UL, -236, -52),
			new CachedPower(16455045573212060422UL, -210, -44),
			new CachedPower(12259964326927110867UL, -183, -36),
			new CachedPower(18268770466636286478UL, -157, -28),
			new CachedPower(13611294676837538539UL, -130, -20),
			new CachedPower(10141204801825835212UL, -103, -12),
			new CachedPower(15111572745182864684UL, -77, -4),
			new CachedPower(11258999068426240000UL, -50, 4),
			new CachedPower(16777216000000000000UL, -24, 12),
			new CachedPower(12500000000000000000UL, 3, 20),
			new CachedPower(9313225746154785156UL, 30, 28),
			new CachedPower(13877787807814456755UL, 56, 36),
			new CachedPower(10339757656912845936UL, 83, 44),
			new CachedPower(15407439555097886824UL, 109, 52),
			new CachedPower(11479437019748901445UL, 136, 60),
			new CachedPower(17105694144590052135UL, 162, 68),
			new CachedPower(12744735289059618216UL, 189, 76),
			new CachedPower(9495567745759798747UL, 216, 84),
			new CachedPower(14149498560666738074UL, 242, 92),
			new CachedPower(10542197943230523224UL, 269, 100),
			new CachedPower(15709099088952724970UL, 295, 108),
			new CachedPower(11704190886730495818UL, 322, 116),
			new CachedPower(17440603504673385349UL, 348, 124),
			new CachedPower(12994262207056124023UL, 375, 132),
			new CachedPower(9681479787123295682UL, 402, 140),
			new CachedPower(14426529090290212157UL, 428, 148),
			new CachedPower(10748601772107342003UL, 455, 156),
			new CachedPower(16016664761464807395UL, 481, 164),
			new CachedPower(11933345169920330789UL, 508, 172),
			new CachedPower(17782069995880619868UL, 534, 180),
			new CachedPower(13248674568444952270UL, 561, 188),
			new CachedPower(9871031767461413346UL, 588, 196),
			new CachedPower(14708983551653345445UL, 614, 204),
			new CachedPower(10959046745042015199UL, 641, 212),
			new CachedPower(16330252207878254650UL, 667, 220),
			new CachedPower(12166986024289022870UL, 694, 228),
			new CachedPower(18130221999122236476UL, 720, 236),
			new CachedPower(13508068024458167312UL, 747, 244),
			new CachedPower(10064294952495520794UL, 774, 252),
			new CachedPower(14996968138956309548UL, 800, 260),
			new CachedPower(11173611982879273257UL, 827, 268),
			new CachedPower(16649979327439178909UL, 853, 276),
			new CachedPower(12405201291620119593UL, 880, 284),
			new CachedPower(9242595204427927429UL, 907, 292),
			new CachedPower(13772540099066387757UL, 933, 300),
			new CachedPower(10261342003245940623UL, 960, 308),
			new CachedPower(15290591125556738113UL, 986, 316),
			new CachedPower(11392378155556871081UL, 1013, 324),
			new CachedPower(16975966327722178521UL, 1039, 332),
			new CachedPower(12648080533535911531UL, 1066, 340)
		};

		public const int kCachedPowersOffset = 348;

		public const double kD_1_LOG2_10 = 0.30102999566398114;

		public const int kDecimalExponentDistance = 8;

		public const int kMinDecimalExponent = -348;

		public const int kMaxDecimalExponent = 340;
	}
}
