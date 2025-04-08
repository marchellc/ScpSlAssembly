using System;
using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp330
{
	public static class Scp330Candies
	{
		public static Dictionary<CandyKindID, ICandy> CandiesById
		{
			get
			{
				if (!Scp330Candies._dictionarySet)
				{
					foreach (ICandy candy in Scp330Candies.AllCandies)
					{
						Scp330Candies.DictionarizedCandies[candy.Kind] = candy;
					}
					Scp330Candies._dictionarySet = true;
				}
				return Scp330Candies.DictionarizedCandies;
			}
		}

		public static CandyKindID GetRandom(CandyKindID ignoredKind = CandyKindID.None)
		{
			float num = 0f;
			foreach (ICandy candy in Scp330Candies.AllCandies)
			{
				if (candy.Kind != ignoredKind)
				{
					num += candy.SpawnChanceWeight;
				}
			}
			float num2 = global::UnityEngine.Random.Range(0f, num);
			foreach (ICandy candy2 in Scp330Candies.AllCandies)
			{
				if (candy2.Kind != ignoredKind)
				{
					num2 -= candy2.SpawnChanceWeight;
					if (num2 <= 0f)
					{
						return candy2.Kind;
					}
				}
			}
			return CandyKindID.None;
		}

		public static readonly ICandy[] AllCandies = new ICandy[]
		{
			new CandyGreen(),
			new CandyPurple(),
			new CandyRainbow(),
			new CandyRed(),
			new CandyYellow(),
			new CandyBlue(),
			new CandyPink()
		};

		private static bool _dictionarySet = false;

		private static readonly Dictionary<CandyKindID, ICandy> DictionarizedCandies = new Dictionary<CandyKindID, ICandy>();
	}
}
