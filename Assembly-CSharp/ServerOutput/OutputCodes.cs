namespace ServerOutput;

public enum OutputCodes : byte
{
	RoundRestart = 16,
	IdleEnter,
	IdleExit,
	ExitActionReset,
	ExitActionShutdown,
	ExitActionSilentShutdown,
	ExitActionRestart,
	Heartbeat
}
