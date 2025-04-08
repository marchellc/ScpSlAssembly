using System;
using UnityEngine;

namespace MapGeneration
{
	[Serializable]
	public struct AtlasInterpretation
	{
		public AtlasInterpretation(GlyphShapePair scannedPair, global::System.Random rng, int pixelX, int pixelY)
		{
			this.Coords = new Vector2Int(pixelX / 3, pixelY / 3);
			this.RoomShape = scannedPair.RoomShape;
			this.SpecificRooms = scannedPair.SpecificRooms;
			float[] roomRotations = scannedPair.RoomRotations;
			this.RotationY = roomRotations[rng.Next(roomRotations.Length)];
		}

		public override string ToString()
		{
			return string.Format("{0} (Shape={1} Spcf={2} Rot={3} Coords={4}x{5})", new object[]
			{
				base.GetType().Name,
				this.RoomShape,
				this.SpecificRooms.Length,
				this.RotationY,
				this.Coords.x,
				this.Coords.y
			});
		}

		public RoomShape RoomShape;

		public RoomName[] SpecificRooms;

		public float RotationY;

		public Vector2Int Coords;
	}
}
