using System;
using System.Collections.Generic;
using UnityEngine;

namespace MapGeneration.Clutter
{
	public interface IClutterBlocker
	{
		Bounds BlockingBounds { get; }

		public static readonly List<IClutterBlocker> Instances = new List<IClutterBlocker>();
	}
}
