using System;
using System.Collections.Generic;
using NorthwoodLib.Pools;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry
{
	public class MimicryRecordingsMenu : MimicryMenuBase
	{
		protected override void Setup(Scp939Role role)
		{
			base.Setup(role);
			role.SubroutineModule.TryGetSubroutine<MimicryRecorder>(out this._recorder);
			int maxRecordings = this._recorder.MaxRecordings;
			this._instancesUserOrder = new MimicryRecordingIcon[maxRecordings];
			for (int i = 0; i < maxRecordings; i++)
			{
				MimicryRecordingIcon mimicryRecordingIcon = global::UnityEngine.Object.Instantiate<MimicryRecordingIcon>(this._iconTemplate, this._iconsParent);
				mimicryRecordingIcon.IsEmpty = true;
				mimicryRecordingIcon.gameObject.SetActive(true);
				this._instancesUserOrder[i] = mimicryRecordingIcon;
			}
			this.UpdateInstancePositions(1f);
			this._recorder.OnSavedVoicesModified += this.OnVoicesModified;
		}

		private void OnDestroy()
		{
			if (this._recorder == null)
			{
				return;
			}
			this._recorder.OnSavedVoicesModified -= this.OnVoicesModified;
		}

		private void OnVoicesModified()
		{
			this._updateNextFrame = true;
			if (this._recorder.SavedVoices.Count <= this._recorder.MaxRecordings)
			{
				return;
			}
			List<int> list = ListPool<int>.Shared.Rent();
			for (int i = 0; i < this._recorder.MaxRecordings; i++)
			{
				if (!this._instancesUserOrder[i].IsFavorite)
				{
					list.Add(i);
				}
			}
			int num = ((list.Count > 0) ? list.RandomItem<int>() : global::UnityEngine.Random.Range(0, this._recorder.MaxRecordings));
			ListPool<int>.Shared.Return(list);
			this._recorder.RemoveVoice(this._instancesUserOrder[num].VoiceRecord);
		}

		private bool GetDragData(out int occupiedIndex, out int targetIndex, out float targetHeight)
		{
			occupiedIndex = this._instancesUserOrder.IndexOf(this._draggedInstance);
			if (occupiedIndex < 0)
			{
				targetIndex = -1;
				targetHeight = -1f;
				return false;
			}
			Vector2 vector;
			float num = (RectTransformUtility.ScreenPointToLocalPointInRectangle(this._iconsParent, Input.mousePosition, null, out vector) ? (-vector.y) : 0f);
			targetHeight = num + this._dragIconOffset;
			targetIndex = Mathf.CeilToInt((num + this._dragPositionOffset) / this._spacing) - 1;
			return true;
		}

		private void UpdateInstancePositions(float interpolant)
		{
			int num;
			int num2;
			float num3;
			bool dragData = this.GetDragData(out num, out num2, out num3);
			for (int i = 0; i < this._instancesUserOrder.Length; i++)
			{
				float num5;
				if (dragData)
				{
					int num4 = i + this.GetPositionOffset(i, num, num2);
					num5 = ((i == num) ? num3 : (this._spacing * (float)num4));
				}
				else
				{
					num5 = this._spacing * (float)i;
				}
				MimicryRecordingIcon mimicryRecordingIcon = this._instancesUserOrder[i];
				mimicryRecordingIcon.Height = Mathf.Lerp(mimicryRecordingIcon.Height, -num5, interpolant);
			}
			if (!dragData || Input.GetKey(KeyCode.Mouse0))
			{
				return;
			}
			this._draggedInstance = null;
			Array.Sort<MimicryRecordingIcon>(this._instancesUserOrder, (MimicryRecordingIcon x, MimicryRecordingIcon y) => y.Height.CompareTo(x.Height));
		}

		private int GetPositionOffset(int instanceId, int origin, int target)
		{
			bool flag = instanceId > origin;
			if (target >= origin)
			{
				if (flag && instanceId <= target)
				{
					return -1;
				}
			}
			else if (!flag && instanceId >= target)
			{
				return 1;
			}
			return 0;
		}

		private void AddInstance(int recordingId)
		{
			MimicryRecordingIcon mimicryRecordingIcon = global::UnityEngine.Object.Instantiate<MimicryRecordingIcon>(this._iconTemplate, this._iconsParent);
			mimicryRecordingIcon.Setup(this._recorder, recordingId);
			mimicryRecordingIcon.gameObject.SetActive(true);
			this._instancesChronological.Add(mimicryRecordingIcon);
			for (int i = 0; i < this._recorder.MaxRecordings; i++)
			{
				MimicryRecordingIcon mimicryRecordingIcon2 = this._instancesUserOrder[i];
				if (mimicryRecordingIcon2.IsEmpty)
				{
					this._instancesUserOrder[i] = mimicryRecordingIcon;
					mimicryRecordingIcon.Height = mimicryRecordingIcon2.Height;
					global::UnityEngine.Object.Destroy(mimicryRecordingIcon2.gameObject);
					return;
				}
			}
		}

		private void RemoveInstance(MimicryRecordingIcon instance)
		{
			instance.IsEmpty = true;
			this._instancesChronological.Remove(instance);
		}

		private void Update()
		{
			this.UpdateInstancePositions(Time.deltaTime * this._lerpSpeed);
			if (!this._updateNextFrame)
			{
				return;
			}
			int num = this._instancesChronological.Count;
			int count = this._recorder.SavedVoices.Count;
			for (int i = 0; i < num; i++)
			{
				MimicryRecordingIcon mimicryRecordingIcon = this._instancesChronological[i];
				if (i >= count || mimicryRecordingIcon.VoiceRecord != this._recorder.SavedVoices[i].Buffer)
				{
					this.RemoveInstance(mimicryRecordingIcon);
					num--;
					i--;
				}
			}
			for (int j = num; j < count; j++)
			{
				this.AddInstance(j);
			}
			this._updateNextFrame = false;
		}

		internal void BeginDrag(MimicryRecordingIcon icon)
		{
			this._draggedInstance = icon;
		}

		[SerializeField]
		private MimicryRecordingIcon _iconTemplate;

		[SerializeField]
		private RectTransform _iconsParent;

		[SerializeField]
		private float _spacing;

		[SerializeField]
		private float _lerpSpeed;

		[SerializeField]
		private float _dragIconOffset;

		[SerializeField]
		private float _dragPositionOffset;

		private bool _updateNextFrame;

		private MimicryRecorder _recorder;

		private MimicryRecordingIcon[] _instancesUserOrder;

		private MimicryRecordingIcon _draggedInstance;

		private readonly List<MimicryRecordingIcon> _instancesChronological = new List<MimicryRecordingIcon>();
	}
}
