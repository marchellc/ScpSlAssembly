using System;

namespace MapGeneration.RoomConnectors
{
	public enum SpawnableRoomConnectorType : byte
	{
		None,
		EzStandardDoor,
		HczStandardDoor,
		LczStandardDoor,
		OpenHallway = 6,
		ClutterPipesLong,
		ClutterSimpleBoxes,
		ClutterPipesShort,
		ClutterBrokenElectricalBox,
		HczBulkDoor,
		ClutterBoxesLadder,
		ClutterTankSupportedShelf,
		ClutterAngledFences,
		ClutterHugeOrangePipes
	}
}
