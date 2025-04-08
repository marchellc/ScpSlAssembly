using System;
using UnityEngine;

[CreateAssetMenu(fileName = "New Door Audio Settings", menuName = "ScriptableObject/Doors/DoorAudioSettings")]
public class DoorAudioSettings : ScriptableObject
{
	public AudioClip RandomOpeningSound
	{
		get
		{
			return this.DoorOpeningSound[DoorAudioSettings.Rng.Next(this.DoorOpeningSound.Length)];
		}
	}

	public AudioClip RandomClosingSound
	{
		get
		{
			return this.DoorClosingSound[DoorAudioSettings.Rng.Next(this.DoorClosingSound.Length)];
		}
	}

	private static readonly global::System.Random Rng = Misc.CreateRandom();

	public AudioClip[] DoorOpeningSound;

	public AudioClip[] DoorClosingSound;

	public AudioClip AccessDenied;

	public AudioClip AccessGranted;
}
