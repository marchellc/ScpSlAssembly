using System;
using MapGeneration;
using UnityEngine;

namespace InventorySystem.Items.Keycards;

[CreateAssetMenu(fileName = "Word Collections", menuName = "ScriptableObject/Items/Keycard Words Collection")]
public class KeycardWordsCollection : ScriptableObject
{
	private int[] _ids;

	private int _lastId;

	private int _lastSeed;

	[field: SerializeField]
	public string[] Words { get; private set; }

	public string NextRandomWord()
	{
		if (this._ids == null || this._lastSeed != SeedSynchronizer.Seed)
		{
			this.ReRandomize();
		}
		int num = this._ids[this._lastId++ % this._ids.Length];
		return this.Words[num % this.Words.Length];
	}

	public void ReRandomize()
	{
		int num = this.Words.Length;
		if (this._ids == null || this._ids.Length != num)
		{
			this._ids = new int[num];
		}
		for (int i = 0; i < num; i++)
		{
			this._ids[i] = i;
		}
		System.Random random = new System.Random();
		int num2 = num;
		while (num2 > 1)
		{
			num2--;
			int num3 = random.Next(num2 + 1);
			ref int reference = ref this._ids[num2];
			ref int reference2 = ref this._ids[num3];
			int num4 = this._ids[num3];
			int num5 = this._ids[num2];
			reference = num4;
			reference2 = num5;
		}
		this._lastSeed = SeedSynchronizer.Seed;
	}
}
