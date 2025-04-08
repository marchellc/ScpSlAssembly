using System;
using UnityEngine;

namespace MapGeneration.Distributors
{
	public abstract class DistributorSpawnpointBase : MonoBehaviour
	{
		public RoomName RoomName
		{
			get
			{
				if (!this._roomSet)
				{
					this._roomName = RoomUtils.RoomAtPosition(base.transform.position).Name;
					this._roomSet = true;
				}
				return this._roomName;
			}
		}

		private void Awake()
		{
			base.transform.localScale = Vector3.one;
		}

		private RoomName _roomName;

		private bool _roomSet;
	}
}
