using System;
using UnityEngine;

namespace CustomRendering
{
	public class FogController : MonoBehaviour
	{
		public static float FogFarPlaneDistance { get; private set; }

		public static FogController Singleton { get; private set; }
	}
}
