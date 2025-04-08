using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments.Components
{
	[CreateAssetMenu(fileName = "New Sight Pack", menuName = "ScriptableObject/Firearms/Reflex Sight Pack")]
	public class ReflexSightReticlePack : ScriptableObject
	{
		public Texture this[int index]
		{
			get
			{
				return this.Reticles[index];
			}
		}

		public int Length
		{
			get
			{
				return this.Reticles.Length;
			}
		}

		public Texture[] Reticles;
	}
}
