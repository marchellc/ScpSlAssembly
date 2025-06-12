using System;
using Mirror;
using UnityEngine;

namespace MapGeneration.Decoration;

public abstract class AlarmTriggerBase : MonoBehaviour
{
	protected static readonly int MaxTriggerMeters = 20;

	private bool _hasRoom;

	protected RoomIdentifier CurrentRoom { get; private set; }

	protected EnvironmentalAlarmSync TargetAlarm { get; private set; }

	protected abstract float Duration { get; }

	protected Vector3 RoomPosition
	{
		get
		{
			if (!this._hasRoom)
			{
				return base.transform.position;
			}
			return this.CurrentRoom.transform.position;
		}
	}

	protected virtual void ServerTriggerAlarm(float? customDuration = null)
	{
		float duration = this.Duration;
		if (customDuration.HasValue)
		{
			duration = customDuration.Value;
		}
		this.TargetAlarm.ServerTriggerAlarm(duration);
	}

	protected virtual void Start()
	{
		if (NetworkServer.active)
		{
			if (!base.TryGetComponent<EnvironmentalAlarmSync>(out var component))
			{
				throw new NullReferenceException();
			}
			this.TargetAlarm = component;
			if (base.transform.position.TryGetRoom(out var room))
			{
				this._hasRoom = true;
				this.CurrentRoom = room;
			}
		}
	}

	protected bool IsInRange(Vector3 targetPos)
	{
		float sqrMagnitude = (this.RoomPosition - targetPos).sqrMagnitude;
		int num = AlarmTriggerBase.MaxTriggerMeters * AlarmTriggerBase.MaxTriggerMeters;
		return sqrMagnitude <= (float)num;
	}
}
