using System;
using UnityEngine;

namespace MapGeneration;

[Serializable]
public struct AtlasInterpretation
{
	public RoomShape RoomShape;

	public RoomName[] SpecificRooms;

	public float RotationY;

	public Vector2Int Coords;

	public AtlasInterpretation(GlyphShapePair scannedPair, System.Random rng, int pixelX, int pixelY)
	{
		this.Coords = new Vector2Int(pixelX / 3, pixelY / 3);
		this.RoomShape = scannedPair.RoomShape;
		this.SpecificRooms = scannedPair.SpecificRooms;
		float[] roomRotations = scannedPair.RoomRotations;
		this.RotationY = roomRotations[rng.Next(roomRotations.Length)];
	}

	public override string ToString()
	{
		return $"{base.GetType().Name} (Shape={this.RoomShape} Spcf={this.SpecificRooms.Length} Rot={this.RotationY} Coords={this.Coords.x}x{this.Coords.y})";
	}
}
