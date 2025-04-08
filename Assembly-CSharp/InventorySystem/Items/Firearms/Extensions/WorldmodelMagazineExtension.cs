using System;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Modules;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Extensions
{
	public class WorldmodelMagazineExtension : MonoBehaviour, IWorldmodelExtension
	{
		public void SetupWorldmodel(FirearmWorldmodel worldmodel)
		{
			this._lastId = worldmodel.Identifier;
			this._lastAttCode = worldmodel.AttachmentCode;
			this._forceInserted = worldmodel.WorldmodelType == FirearmWorldmodelType.Presentation;
			this.UpdateAllMags();
		}

		private void UpdateAllMags()
		{
			bool flag = this._forceInserted || MagazineModule.GetMagazineInserted(this._lastId.SerialNumber);
			WorldmodelMagazineExtension.Magazine[] magazines = this._magazines;
			for (int i = 0; i < magazines.Length; i++)
			{
				magazines[i].UpdateState(this, flag);
			}
		}

		[RuntimeInitializeOnLoadMethod]
		private static void Init()
		{
			MagazineModule.OnDataReceived += delegate(ushort serial)
			{
				FirearmWorldmodel firearmWorldmodel;
				if (!FirearmWorldmodel.Instances.TryGetValue(serial, out firearmWorldmodel))
				{
					return;
				}
				WorldmodelMagazineExtension worldmodelMagazineExtension;
				if (!firearmWorldmodel.TryGetExtension<WorldmodelMagazineExtension>(out worldmodelMagazineExtension))
				{
					return;
				}
				worldmodelMagazineExtension.UpdateAllMags();
			};
		}

		private ItemIdentifier _lastId;

		private uint _lastAttCode;

		private bool _forceInserted;

		[SerializeField]
		private WorldmodelMagazineExtension.Magazine[] _magazines;

		[SerializeField]
		private WorldmodelMagazineExtension.RemovalMode _removalMode;

		[Serializable]
		private class Magazine
		{
			public void UpdateState(WorldmodelMagazineExtension extRef, bool magInserted)
			{
				if (magInserted)
				{
					if (this._filter == null)
					{
						this.SetFilter(extRef._lastId);
					}
					uint num = extRef._lastAttCode & this._filter.Value;
					this.SetVisibility(extRef._removalMode, num > 0U);
					return;
				}
				this.SetVisibility(extRef._removalMode, false);
			}

			private void SetVisibility(WorldmodelMagazineExtension.RemovalMode removalMode, bool isActive)
			{
				if (this._lastIsActive != null)
				{
					bool? lastIsActive = this._lastIsActive;
					if ((lastIsActive.GetValueOrDefault() == isActive) & (lastIsActive != null))
					{
						return;
					}
				}
				if (removalMode != WorldmodelMagazineExtension.RemovalMode.DeactivateObject)
				{
					if (removalMode == WorldmodelMagazineExtension.RemovalMode.SetScaleZero)
					{
						this.Target.transform.localScale = (isActive ? Vector3.one : Vector3.zero);
					}
				}
				else
				{
					this.Target.SetActive(isActive);
				}
				this._lastIsActive = new bool?(isActive);
			}

			private void SetFilter(ItemIdentifier id)
			{
				if (this.Attachments == null || this.Attachments.Length == 0)
				{
					this._filter = new uint?(uint.MaxValue);
					return;
				}
				uint num = 0U;
				AttachmentLink[] attachments = this.Attachments;
				for (int i = 0; i < attachments.Length; i++)
				{
					uint num2;
					if (attachments[i].TryGetFilter(id.TypeId, out num2))
					{
						num |= num2;
					}
				}
				this._filter = new uint?(num);
			}

			private uint? _filter;

			private bool? _lastIsActive;

			public GameObject Target;

			public AttachmentLink[] Attachments;
		}

		private enum RemovalMode
		{
			DeactivateObject,
			SetScaleZero
		}
	}
}
