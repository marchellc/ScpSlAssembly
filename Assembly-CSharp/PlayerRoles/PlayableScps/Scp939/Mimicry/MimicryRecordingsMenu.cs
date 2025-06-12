using System;
using System.Collections.Generic;
using NorthwoodLib.Pools;
using UnityEngine;

namespace PlayerRoles.PlayableScps.Scp939.Mimicry;

public class MimicryRecordingsMenu : MimicryMenuBase
{
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

	protected override void Setup(Scp939Role role)
	{
		base.Setup(role);
		role.SubroutineModule.TryGetSubroutine<MimicryRecorder>(out this._recorder);
		int maxRecordings = this._recorder.MaxRecordings;
		this._instancesUserOrder = new MimicryRecordingIcon[maxRecordings];
		for (int i = 0; i < maxRecordings; i++)
		{
			MimicryRecordingIcon mimicryRecordingIcon = UnityEngine.Object.Instantiate(this._iconTemplate, this._iconsParent);
			mimicryRecordingIcon.IsEmpty = true;
			mimicryRecordingIcon.gameObject.SetActive(value: true);
			this._instancesUserOrder[i] = mimicryRecordingIcon;
		}
		this.UpdateInstancePositions(1f);
		this._recorder.OnSavedVoicesModified += OnVoicesModified;
	}

	private void OnDestroy()
	{
		if (!(this._recorder == null))
		{
			this._recorder.OnSavedVoicesModified -= OnVoicesModified;
		}
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
		int num = ((list.Count > 0) ? list.RandomItem() : UnityEngine.Random.Range(0, this._recorder.MaxRecordings));
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
		Vector2 localPoint;
		float num = (RectTransformUtility.ScreenPointToLocalPointInRectangle(this._iconsParent, Input.mousePosition, null, out localPoint) ? (0f - localPoint.y) : 0f);
		targetHeight = num + this._dragIconOffset;
		targetIndex = Mathf.CeilToInt((num + this._dragPositionOffset) / this._spacing) - 1;
		return true;
	}

	private void UpdateInstancePositions(float interpolant)
	{
		int occupiedIndex;
		int targetIndex;
		float targetHeight;
		bool dragData = this.GetDragData(out occupiedIndex, out targetIndex, out targetHeight);
		for (int i = 0; i < this._instancesUserOrder.Length; i++)
		{
			float num2;
			if (dragData)
			{
				int num = i + this.GetPositionOffset(i, occupiedIndex, targetIndex);
				num2 = ((i == occupiedIndex) ? targetHeight : (this._spacing * (float)num));
			}
			else
			{
				num2 = this._spacing * (float)i;
			}
			MimicryRecordingIcon obj = this._instancesUserOrder[i];
			obj.Height = Mathf.Lerp(obj.Height, 0f - num2, interpolant);
		}
		if (dragData && !Input.GetKey(KeyCode.Mouse0))
		{
			this._draggedInstance = null;
			Array.Sort(this._instancesUserOrder, (MimicryRecordingIcon x, MimicryRecordingIcon y) => y.Height.CompareTo(x.Height));
		}
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
		MimicryRecordingIcon mimicryRecordingIcon = UnityEngine.Object.Instantiate(this._iconTemplate, this._iconsParent);
		mimicryRecordingIcon.Setup(this._recorder, recordingId);
		mimicryRecordingIcon.gameObject.SetActive(value: true);
		this._instancesChronological.Add(mimicryRecordingIcon);
		for (int i = 0; i < this._recorder.MaxRecordings; i++)
		{
			MimicryRecordingIcon mimicryRecordingIcon2 = this._instancesUserOrder[i];
			if (mimicryRecordingIcon2.IsEmpty)
			{
				this._instancesUserOrder[i] = mimicryRecordingIcon;
				mimicryRecordingIcon.Height = mimicryRecordingIcon2.Height;
				UnityEngine.Object.Destroy(mimicryRecordingIcon2.gameObject);
				break;
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
}
