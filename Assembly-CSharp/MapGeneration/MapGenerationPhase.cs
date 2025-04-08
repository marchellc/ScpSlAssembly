using System;

namespace MapGeneration
{
	public enum MapGenerationPhase
	{
		ParentRoomRegistration,
		RelativePositioningWaypoints,
		ComplexDecorationsAndClutter,
		SimpleDecorations,
		CullingCaching,
		SpawnableStructures
	}
}
