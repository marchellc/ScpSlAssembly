using System.Collections.Generic;
using UnityEngine;

namespace InventorySystem.Items.Usables.Scp330;

public static class Scp330Candies
{
	public static readonly ICandy[] AllCandies = new ICandy[7]
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

	public static Dictionary<CandyKindID, ICandy> CandiesById
	{
		get
		{
			if (!Scp330Candies._dictionarySet)
			{
				ICandy[] allCandies = Scp330Candies.AllCandies;
				foreach (ICandy candy in allCandies)
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
		ICandy[] allCandies = Scp330Candies.AllCandies;
		foreach (ICandy candy in allCandies)
		{
			if (candy.Kind != ignoredKind)
			{
				num += candy.SpawnChanceWeight;
			}
		}
		float num2 = Random.Range(0f, num);
		allCandies = Scp330Candies.AllCandies;
		foreach (ICandy candy2 in allCandies)
		{
			if (candy2.Kind != ignoredKind)
			{
				num2 -= candy2.SpawnChanceWeight;
				if (!(num2 > 0f))
				{
					return candy2.Kind;
				}
			}
		}
		return CandyKindID.None;
	}
}
