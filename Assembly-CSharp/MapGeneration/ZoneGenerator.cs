using System;
using UnityEngine;

namespace MapGeneration
{
	public abstract class ZoneGenerator : MonoBehaviour
	{
		public abstract void Generate(global::System.Random rng);

		public FacilityZone TargetZone;
	}
}
