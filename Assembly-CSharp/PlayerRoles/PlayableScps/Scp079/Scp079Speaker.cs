using System;
using System.Collections.Generic;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079
{
	public class Scp079Speaker : Scp079InteractableBase
	{
		protected override void OnRegistered()
		{
			base.OnRegistered();
			this._wasRegistered = true;
			Scp079Speaker.SpeakersInRooms.GetOrAdd(this.Room, () => new List<Scp079Speaker>()).Add(this);
		}

		protected override void OnDestroy()
		{
			base.OnDestroy();
			if (!this._wasRegistered || this.Room == null)
			{
				return;
			}
			List<Scp079Speaker> list;
			if (!Scp079Speaker.SpeakersInRooms.TryGetValue(this.Room, out list))
			{
				return;
			}
			if (!list.Remove(this) || list.Count != 0)
			{
				return;
			}
			Scp079Speaker.SpeakersInRooms.Remove(this.Room);
		}

		public static bool TryGetSpeaker(Scp079Camera cam, out Scp079Speaker best)
		{
			best = null;
			List<Scp079Speaker> list;
			if (!Scp079Speaker.SpeakersInRooms.TryGetValue(cam.Room, out list))
			{
				return false;
			}
			Vector3 position = cam.Position;
			bool flag = false;
			float num = 0f;
			foreach (Scp079Speaker scp079Speaker in list)
			{
				float sqrMagnitude = (scp079Speaker.Position - position).sqrMagnitude;
				if (sqrMagnitude <= num || !flag)
				{
					flag = true;
					num = sqrMagnitude;
					best = scp079Speaker;
				}
			}
			return flag;
		}

		private static readonly Dictionary<RoomIdentifier, List<Scp079Speaker>> SpeakersInRooms = new Dictionary<RoomIdentifier, List<Scp079Speaker>>();

		private bool _wasRegistered;
	}
}
