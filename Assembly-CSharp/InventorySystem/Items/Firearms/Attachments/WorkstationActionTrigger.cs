using System;
using Interactables;
using Interactables.Verification;
using UnityEngine;

namespace InventorySystem.Items.Firearms.Attachments
{
	public class WorkstationActionTrigger : InteractableCollider, IClientInteractable, IInteractable
	{
		public Action<Vector2> TargetAction { get; internal set; }

		public IVerificationRule VerificationRule
		{
			get
			{
				return StandardDistanceVerification.Default;
			}
		}

		protected override void Awake()
		{
			this.Target = this;
			this._rt = base.GetComponent<RectTransform>();
			this._col = base.gameObject.AddComponent<BoxCollider>();
			this._depth = 0.61f;
			Transform transform = this._rt.parent;
			while (transform != null)
			{
				WorkstationActionTrigger workstationActionTrigger;
				if (transform.TryGetComponent<WorkstationActionTrigger>(out workstationActionTrigger))
				{
					this._depth += 0.25f;
				}
				transform = transform.parent;
			}
			this.UpdateSize();
		}

		private void Update()
		{
			this.UpdateSize();
		}

		private void UpdateSize()
		{
			this._col.size = new Vector3(this._rt.rect.size.x, this._rt.rect.size.y, this._depth);
			this._halfSize = new Vector2(this._col.size.x, this._col.size.y) * 0.5f;
		}

		public void ClientInteract(InteractableCollider _)
		{
			Vector3 point = InteractionCoordinator.LastRaycastHit.point;
			Vector3 vector = this._rt.InverseTransformPoint(point);
			Action<Vector2> targetAction = this.TargetAction;
			if (targetAction == null)
			{
				return;
			}
			targetAction(vector / this._halfSize);
		}

		private const float BaselineWidth = 0.61f;

		private const float ParentWidth = 0.25f;

		private RectTransform _rt;

		private BoxCollider _col;

		private float _depth;

		private Vector2 _halfSize;
	}
}
