using System.Collections.Generic;
using MapGeneration;
using PlayerRoles.PlayableScps.Scp079.Cameras;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp079;

public class Scp079Speaker : Scp079InteractableBase
{
	private static readonly Dictionary<RoomIdentifier, List<Scp079Speaker>> SpeakersInRooms = new Dictionary<RoomIdentifier, List<Scp079Speaker>>();

	private bool _wasRegistered;

	protected override void OnRegistered()
	{
		base.OnRegistered();
		this._wasRegistered = true;
		Scp079Speaker.SpeakersInRooms.GetOrAddNew(this.Room).Add(this);
	}

	protected override void OnDestroy()
	{
		base.OnDestroy();
		if (this._wasRegistered && !(this.Room == null) && Scp079Speaker.SpeakersInRooms.TryGetValue(this.Room, out var value) && value.Remove(this) && value.Count == 0)
		{
			Scp079Speaker.SpeakersInRooms.Remove(this.Room);
		}
	}

	public static bool TryGetSpeaker(Scp079Camera cam, out Scp079Speaker best)
	{
		best = null;
		if (!Scp079Speaker.SpeakersInRooms.TryGetValue(cam.Room, out var value))
		{
			return false;
		}
		Vector3 position = cam.Position;
		bool flag = false;
		float num = 0f;
		foreach (Scp079Speaker item in value)
		{
			float sqrMagnitude = (item.Position - position).sqrMagnitude;
			if (!(sqrMagnitude > num && flag))
			{
				flag = true;
				num = sqrMagnitude;
				best = item;
			}
		}
		return flag;
	}
}
