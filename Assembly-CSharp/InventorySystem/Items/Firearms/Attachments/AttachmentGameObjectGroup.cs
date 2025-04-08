using System;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments
{
	[Serializable]
	public struct AttachmentGameObjectGroup
	{
		public readonly void SetActive(bool state)
		{
			for (int i = 0; i < this.Group.Length; i++)
			{
				this.Group[i].SetActive(state);
			}
		}

		public GameObject[] Group;
	}
}
