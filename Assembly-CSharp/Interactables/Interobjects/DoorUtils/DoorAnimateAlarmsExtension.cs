using MapGeneration.Decoration;

namespace Interactables.Interobjects.DoorUtils;

public class DoorAnimateAlarmsExtension : DoorVariantExtension
{
	public EnvironmentalAlarm[] Alarms;

	public bool EnableOnOpen = true;

	public bool EnableOnClose = true;

	public bool EnableOnMoving = true;

	public bool EnableOnPry = true;

	private bool _previousState;

	private void Start()
	{
		EnvironmentalAlarm[] alarms = this.Alarms;
		for (int i = 0; i < alarms.Length; i++)
		{
			alarms[i].IsEnabled = false;
		}
	}

	private void Update()
	{
		bool targetState = base.TargetDoor.TargetState;
		bool isMoving = base.TargetDoor.IsMoving;
		bool flag = base.TargetDoor is PryableDoor pryableDoor && pryableDoor.IsBeingPried;
		bool flag2 = (this.EnableOnPry || !flag) && ((this.EnableOnOpen && targetState) || (this.EnableOnClose && !targetState) || (this.EnableOnMoving && isMoving) || (this.EnableOnPry && flag));
		if (flag2 != this._previousState)
		{
			this._previousState = flag2;
			EnvironmentalAlarm[] alarms = this.Alarms;
			for (int i = 0; i < alarms.Length; i++)
			{
				alarms[i].IsEnabled = flag2;
			}
		}
	}
}
