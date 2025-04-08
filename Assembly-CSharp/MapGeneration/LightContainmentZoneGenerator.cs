using System;
using UnityEngine;
using Utils.NonAllocLINQ;

namespace MapGeneration
{
	public class LightContainmentZoneGenerator : AtlasZoneGenerator
	{
		public override void Generate(global::System.Random rng)
		{
			base.Generate(rng);
			LCZ_Label.AllLabels.ForEach(new Action<LCZ_Label>(this.SetLabel));
		}

		private void SetLabel(LCZ_Label label)
		{
			SpawnableRoom spawnableRoom;
			if (!this.TryFindRoom(label.transform, out spawnableRoom))
			{
				return;
			}
			foreach (LightContainmentZoneGenerator.LabelSettings labelSettings in this._settings)
			{
				if (labelSettings.Name == spawnableRoom.Room.Name && labelSettings.Shape == spawnableRoom.Room.Shape)
				{
					int num = labelSettings.DefaultNumber + spawnableRoom.DuplicateId;
					label.Refresh(labelSettings.Label, this._numbers[num % this._numbers.Length]);
					return;
				}
			}
		}

		private bool TryFindRoom(Transform labelTr, out SpawnableRoom ret)
		{
			Vector3 vector = labelTr.position + labelTr.forward * 8f;
			foreach (AtlasZoneGenerator.SpawnedRoomData spawnedRoomData in this.Spawned)
			{
				RoomIdentifier room = spawnedRoomData.Instance.Room;
				Bounds bounds = new Bounds(room.transform.position, RoomIdentifier.GridScale);
				if (bounds.Contains(vector))
				{
					ret = spawnedRoomData.Instance;
					return true;
				}
			}
			ret = null;
			return false;
		}

		[SerializeField]
		private LightContainmentZoneGenerator.LabelSettings[] _settings;

		[SerializeField]
		private Material[] _numbers;

		private const float NextRoomLabelScanRange = 8f;

		[Serializable]
		private class LabelSettings
		{
			public RoomName Name;

			public RoomShape Shape;

			public Material Label;

			public int DefaultNumber;
		}
	}
}
