using System;

namespace Subtitles
{
	public enum SubtitleType : byte
	{
		NTFEntrance,
		AwaitContainPlural,
		AwaitContainSingle,
		ThreatRemains,
		TerminationCauseUnspecified,
		TerminatedBySCP,
		TerminatedBySecuritySystem,
		TerminatedByWarhead,
		ContainedByScientist,
		ContainedByClassD,
		ContainedByChaos,
		SCP,
		ContainUnitUnknown,
		ContainUnit,
		LostInDecontamination,
		GeneratorsActivated,
		AllGeneratorsEngaged,
		OverchargeIn,
		OperationalMode,
		DecontaminationStart,
		DecontaminationMinutes,
		Decontamination1Minute,
		DecontaminationCountdown,
		DecontaminationLockdown,
		AlphaWarheadEngage,
		AlphaWarheadCancelled,
		AlphaWarheadResumed,
		TerminatedByMarshmallowMan,
		NTFMiniwaveEntrance,
		ChaosEntrance,
		ChaosMiniwaveEntrance,
		Custom = 254,
		None
	}
}
