namespace MapGeneration;

public enum MapGenerationPhase
{
	RoomCoordsRegistrations,
	ParentRoomRegistration,
	RelativePositioningWaypoints,
	ComplexDecorationsAndClutter,
	SimpleDecorations,
	CullingCaching,
	SpawnableStructures,
	StaticBatching
}
