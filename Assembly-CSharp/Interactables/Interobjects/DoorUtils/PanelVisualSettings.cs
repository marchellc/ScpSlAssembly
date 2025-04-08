using System;
using UnityEngine;

namespace Interactables.Interobjects.DoorUtils
{
	[CreateAssetMenu(fileName = "New Panel Visual Settings", menuName = "ScriptableObject/Doors/PanelVisualSettings")]
	public class PanelVisualSettings : ScriptableObject
	{
		public string TextOpen
		{
			get
			{
				if (!this._textOpenSet)
				{
					this._textOpenCache = TranslationReader.Get("Doors", this._textOpenTranslationId, "<color=#00ff00>OPEN</color>");
					this._textOpenSet = true;
				}
				return this._textOpenCache;
			}
		}

		public string TextClosed
		{
			get
			{
				if (!this._textClosedSet)
				{
					this._textClosedCache = TranslationReader.Get("Doors", this._textClosedTranslationId, "<color=#07A2FE>CLOSED</color>");
					this._textClosedSet = true;
				}
				return this._textClosedCache;
			}
		}

		public string TextMoving
		{
			get
			{
				if (!this._textMovingSet)
				{
					this._textMovingCache = TranslationReader.Get("Doors", this._textMovingTranslationId, "<color=#FFA600>MOVING</color>");
					this._textMovingSet = true;
				}
				return this._textMovingCache;
			}
		}

		public string TextLockedDown
		{
			get
			{
				if (!this._textLockedDownSet)
				{
					this._textLockedDownCache = TranslationReader.Get("Doors", this._textLockedDownTranslationId, "<color=#FF0000>LOCKDOWN</color>");
					this._textLockedDownSet = true;
				}
				return this._textLockedDownCache;
			}
		}

		public string TextError
		{
			get
			{
				if (!this._textErrorSet)
				{
					this._textErrorCache = TranslationReader.Get("Doors", this._textErrorTranslationId, "<color=#FF0000>ERROR</color>");
					this._textErrorSet = true;
				}
				return this._textErrorCache;
			}
		}

		public string TextDenied
		{
			get
			{
				if (!this._textDeniedSet)
				{
					this._textDeniedCache = TranslationReader.Get("Doors", this._textDeniedTranslationId, "<color=#FF0000>ACCESS DENIED</color>");
					this._textDeniedSet = true;
				}
				return this._textDeniedCache;
			}
		}

		public void Reset()
		{
			this._textOpenSet = false;
			this._textClosedSet = false;
			this._textMovingSet = false;
			this._textLockedDownSet = false;
			this._textErrorSet = false;
			this._textDeniedSet = false;
		}

		public Material PanelOpenMat;

		public Material PanelClosedMat;

		public Material PanelMovingMat;

		public Material PanelErrorMat;

		public Material PanelDeniedMat;

		public const string DoorTranslationKey = "Doors";

		[SerializeField]
		private int _textOpenTranslationId;

		[SerializeField]
		private int _textClosedTranslationId;

		[SerializeField]
		private int _textMovingTranslationId;

		[SerializeField]
		private int _textLockedDownTranslationId;

		[SerializeField]
		private int _textErrorTranslationId;

		[SerializeField]
		private int _textDeniedTranslationId;

		private string _textOpenCache;

		private string _textClosedCache;

		private string _textMovingCache;

		private string _textLockedDownCache;

		private string _textErrorCache;

		private string _textDeniedCache;

		private bool _textOpenSet;

		private bool _textClosedSet;

		private bool _textMovingSet;

		private bool _textLockedDownSet;

		private bool _textErrorSet;

		private bool _textDeniedSet;
	}
}
