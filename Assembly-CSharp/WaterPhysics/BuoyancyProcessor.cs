using System;
using UnityEngine;

namespace WaterPhysics
{
	public static class BuoyancyProcessor
	{
		public static void ProcessNew(Rigidbody rb, BuoyancyMode mode)
		{
			switch (mode)
			{
			case BuoyancyMode.SuperHeavy:
			case BuoyancyMode.Floater:
				rb.drag = 1.7f;
				return;
			case BuoyancyMode.NonBuoyant:
			case BuoyancyMode.ShortTimeFloaters:
			case BuoyancyMode.LongTimeFloaters:
				rb.drag = 2f;
				return;
			case BuoyancyMode.SuperLight:
				rb.drag = 0.5f;
				return;
			default:
				return;
			}
		}

		public static void ProcessExit(Rigidbody rb)
		{
			rb.drag = 0f;
		}

		public static void ProcessFixedUpdate(Rigidbody rb, BuoyancyMode mode, float stayTime, float submergeRatio, Vector3 flow)
		{
			if (stayTime > 100f)
			{
				return;
			}
			float num = 1f - Mathf.InverseLerp(80f, 100f, stayTime);
			if (rb.mass < 0.2f)
			{
				flow *= rb.mass / 0.2f;
			}
			rb.AddForce(flow * num, ForceMode.Force);
			float num2;
			switch (mode)
			{
			case BuoyancyMode.SuperHeavy:
				return;
			case BuoyancyMode.NonBuoyant:
				num2 = 0.9f;
				break;
			case BuoyancyMode.ShortTimeFloaters:
				num2 = Mathf.Lerp(1.5f, 0.95f, stayTime / 10f);
				break;
			case BuoyancyMode.LongTimeFloaters:
				num2 = Mathf.Lerp(1.2f, 0.98f, stayTime / 45f);
				break;
			case BuoyancyMode.Floater:
				num2 = 2f;
				break;
			case BuoyancyMode.SuperLight:
				num2 = 10f;
				break;
			default:
				return;
			}
			Vector3 vector = num2 * submergeRatio * Time.fixedDeltaTime * -Physics.gravity;
			rb.AddForce(vector * num, ForceMode.VelocityChange);
		}

		private const float FullCullTime = 100f;

		private const float StartCullTime = 80f;
	}
}
