using System;
using System.Collections.Generic;
using System.Linq;
using InventorySystem.Items.Firearms.Attachments;
using InventorySystem.Items.Firearms.Extensions;
using UnityEngine;

namespace InventorySystem.Items.Firearms
{
	public class FirearmWorldmodel : MonoBehaviour
	{
		public static event Action<FirearmWorldmodel> OnSetup;

		public ItemIdentifier Identifier { get; private set; }

		public uint AttachmentCode { get; private set; }

		public FirearmWorldmodelType WorldmodelType { get; private set; }

		public AttachmentGameObjectGroup[] Attachments { get; private set; }

		public Component[] Extensions { get; private set; }

		public Collider[] Colliders { get; private set; }

		public Renderer[] Renderers { get; private set; }

		private void Unlink()
		{
			FirearmWorldmodel firearmWorldmodel;
			if (!FirearmWorldmodel.Instances.TryGetValue(this.Identifier.SerialNumber, out firearmWorldmodel))
			{
				return;
			}
			if (firearmWorldmodel != this)
			{
				return;
			}
			FirearmWorldmodel.Instances.Remove(this.Identifier.SerialNumber);
		}

		private void OnDestroy()
		{
			this.Unlink();
			Component[] extensions = this.Extensions;
			for (int i = 0; i < extensions.Length; i++)
			{
				IDestroyExtensionReceiver destroyExtensionReceiver = extensions[i] as IDestroyExtensionReceiver;
				if (destroyExtensionReceiver != null)
				{
					destroyExtensionReceiver.OnDestroyExtension();
				}
			}
		}

		private void OnValidate()
		{
			this.Colliders = base.GetComponentsInChildren<Collider>(true);
			this.Renderers = base.GetComponentsInChildren<Renderer>(true);
			this.Extensions = (from x in base.GetComponentsInChildren<Component>(true)
				where x is IWorldmodelExtension
				select x).ToArray<Component>();
		}

		private void UpdateWorldmodelContext()
		{
			bool flag;
			bool flag2;
			switch (this.WorldmodelType)
			{
			case FirearmWorldmodelType.Pickup:
				flag = true;
				flag2 = false;
				break;
			case FirearmWorldmodelType.Thirdperson:
				flag = false;
				flag2 = true;
				break;
			case FirearmWorldmodelType.Presentation:
				flag = false;
				flag2 = false;
				break;
			default:
				return;
			}
			Collider[] colliders = this.Colliders;
			for (int i = 0; i < colliders.Length; i++)
			{
				colliders[i].enabled = flag;
			}
			if (flag2)
			{
				if (this._prevLayers == null)
				{
					this._prevLayers = new Dictionary<Renderer, int>();
				}
				foreach (Renderer renderer in this.Renderers)
				{
					if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
					{
						GameObject gameObject = renderer.gameObject;
						this._prevLayers[renderer] = gameObject.layer;
						gameObject.layer = 13;
					}
				}
				return;
			}
			if (this._prevLayers == null)
			{
				return;
			}
			foreach (Renderer renderer2 in this.Renderers)
			{
				int num;
				if (this._prevLayers.TryGetValue(renderer2, out num))
				{
					renderer2.gameObject.layer = num;
				}
			}
		}

		public void Setup(ItemIdentifier identifier, FirearmWorldmodelType worldmodelType)
		{
			uint num;
			if (!AttachmentCodeSync.TryGet(identifier.SerialNumber, out num))
			{
				Firearm firearm;
				if (!InventoryItemLoader.TryGetItem<Firearm>(identifier.TypeId, out firearm))
				{
					return;
				}
				num = firearm.ValidateAttachmentsCode(0U);
			}
			this.Setup(identifier, worldmodelType, num);
		}

		public void Setup(ItemIdentifier identifier, FirearmWorldmodelType worldmodelType, uint attachmentCode)
		{
			if (this.Identifier != identifier)
			{
				this.Unlink();
				this.Identifier = identifier;
				FirearmWorldmodel.Instances[this.Identifier.SerialNumber] = this;
			}
			if (this.WorldmodelType != worldmodelType || !this._wasEverSetup)
			{
				this.WorldmodelType = worldmodelType;
				this.UpdateWorldmodelContext();
			}
			if (this.AttachmentCode == attachmentCode && this._wasEverSetup)
			{
				return;
			}
			this.AttachmentCode = attachmentCode;
			foreach (AttachmentGameObjectGroup attachmentGameObjectGroup in this.Attachments)
			{
				attachmentGameObjectGroup.SetActive(false);
			}
			uint num = 1U;
			for (int j = 0; j < this.Attachments.Length; j++)
			{
				if ((attachmentCode & num) == num)
				{
					this.Attachments[j].SetActive(true);
				}
				num *= 2U;
			}
			Component[] extensions = this.Extensions;
			for (int i = 0; i < extensions.Length; i++)
			{
				(extensions[i] as IWorldmodelExtension).SetupWorldmodel(this);
			}
			this._wasEverSetup = true;
			Action<FirearmWorldmodel> onSetup = FirearmWorldmodel.OnSetup;
			if (onSetup == null)
			{
				return;
			}
			onSetup(this);
		}

		public bool TryGetExtension<T>(out T extension)
		{
			foreach (Component component in this.Extensions)
			{
				if (component is T)
				{
					T t = component as T;
					extension = t;
					return true;
				}
			}
			extension = default(T);
			return false;
		}

		public static readonly Dictionary<ushort, FirearmWorldmodel> Instances = new Dictionary<ushort, FirearmWorldmodel>();

		private bool _wasEverSetup;

		private Dictionary<Renderer, int> _prevLayers;

		private const int HitboxLayer = 13;
	}
}
