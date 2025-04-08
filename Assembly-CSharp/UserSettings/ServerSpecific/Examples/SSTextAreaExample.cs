using System;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using TMPro;
using UnityEngine;

namespace UserSettings.ServerSpecific.Examples
{
	public class SSTextAreaExample : SSExampleImplementationBase
	{
		public override string Name
		{
			get
			{
				return "Different types of Text Areas";
			}
		}

		public override void Activate()
		{
			ServerSpecificSettingsSync.DefinedSettings = new ServerSpecificSettingBase[]
			{
				new SSGroupHeader("Different Text Area Types", false, null),
				new SSTextArea(null, "This is a single-line text area.", SSTextArea.FoldoutMode.NotCollapsable, null, TextAlignmentOptions.TopLeft),
				new SSTextArea(null, "<color=green>This</color> <size=30>text</size> <color=red>area</color> <u>supports</u> <i>Rich</i> <b>Text</b> <rotate=\"25\">Tags</rotate>.", SSTextArea.FoldoutMode.NotCollapsable, null, TextAlignmentOptions.TopLeft),
				new SSTextArea(null, "This is a multi-line text area which is collapsed by default. It can be expanded, retaining its previous state when toggling this tab on and off. As you probably noticed, it also features a custom text while collapsed.\n\nYou can use the newline symbol (or \"<noparse><br></noparse>\") to create\nnew\nlines\nwithout\nrestrictions. \n\nRich Text Tags are still supported, but due to technical limitations, links only work on non-collapsable text areas.\n\nDifferent writing systems are also supported:\nA B C D   А Б В Г    Α Β Γ Δ    أ ب ت ث   א ב ג ד   山 水 火 木   あ い う え   가 나 다 라", SSTextArea.FoldoutMode.CollapsedByDefault, "Expand me!", TextAlignmentOptions.TopLeft),
				new SSTextArea(null, "This is another multi-line text area, but this one features auto-generated preview text when collapsed, with ellipses appearing when the text no longer fits. It also has an option enabled to collapse automatically when you switch off this settings tab. In other words, you will need to re-expand this text area each time you visit this tab.", SSTextArea.FoldoutMode.CollapseOnEntry, null, TextAlignmentOptions.TopLeft),
				new SSTextArea(null, "This multi-line text area is expanded by default but can be collapsed if needed. It will retain its previous state when toggling this tab on and off.", SSTextArea.FoldoutMode.ExtendedByDefault, null, TextAlignmentOptions.TopLeft),
				new SSTextArea(null, "This multi-line text area is similar to the one above, but it will re-expand itself after collapsing each time you visit this tab.", SSTextArea.FoldoutMode.ExtendOnEntry, null, TextAlignmentOptions.TopLeft),
				new SSTextArea(null, "This multi-line text area cannot be collapsed.\nIt remains fully expanded at all times, but supports URL links.\nExample link: <link=\"https://www.youtube.com/watch?v=dQw4w9WgXcQ\"><mark=#5865f215>[Click]</mark></link>", SSTextArea.FoldoutMode.NotCollapsable, null, TextAlignmentOptions.TopLeft),
				new SSGroupHeader("Human Scanner", false, "Generates a list of alive humans with their distances to you. The size is automatically adjusted based on the number of results."),
				this._responseScan = new SSTextArea(null, "Not scanned yet", SSTextArea.FoldoutMode.NotCollapsable, null, TextAlignmentOptions.TopLeft),
				this._requestScanButton = new SSButton(null, "Scan for players", "Scan", null, null)
			};
			ServerSpecificSettingsSync.ServerOnSettingValueReceived += this.ServerOnSettingValueReceived;
			ServerSpecificSettingsSync.SendToAll();
		}

		public override void Deactivate()
		{
			ServerSpecificSettingsSync.ServerOnSettingValueReceived -= this.ServerOnSettingValueReceived;
		}

		private void ServerOnSettingValueReceived(ReferenceHub sender, ServerSpecificSettingBase modifiedSetting)
		{
			if (modifiedSetting.SettingId != this._requestScanButton.SettingId)
			{
				return;
			}
			this.OnScannerButtonPressed(sender);
		}

		private void OnScannerButtonPressed(ReferenceHub sender)
		{
			IFpcRole fpcRole = sender.roleManager.CurrentRole as IFpcRole;
			if (fpcRole == null)
			{
				this._responseScan.SendTextUpdate("Your current role is not supported.", false, (ReferenceHub x) => x == sender);
				return;
			}
			string text = null;
			foreach (ReferenceHub referenceHub in ReferenceHub.AllHubs)
			{
				HumanRole humanRole = referenceHub.roleManager.CurrentRole as HumanRole;
				if (humanRole != null && !(referenceHub == sender))
				{
					float num = Vector3.Distance(humanRole.FpcModule.Position, fpcRole.FpcModule.Position);
					string text2 = string.Format("\n-{0} ({1}) - {2} m", referenceHub.nicknameSync.DisplayName, humanRole.GetColoredName(), num);
					if (text == null)
					{
						text = "Detected humans: ";
					}
					text += text2;
				}
			}
			this._responseScan.SendTextUpdate(text ?? "No humans detected.", false, (ReferenceHub x) => x == sender);
		}

		private const string DemoSingleLine = "This is a single-line text area.";

		private const string DemoRichText = "<color=green>This</color> <size=30>text</size> <color=red>area</color> <u>supports</u> <i>Rich</i> <b>Text</b> <rotate=\"25\">Tags</rotate>.";

		private const string DemoMultiLineCustomText = "This is a multi-line text area which is collapsed by default. It can be expanded, retaining its previous state when toggling this tab on and off. As you probably noticed, it also features a custom text while collapsed.\n\nYou can use the newline symbol (or \"<noparse><br></noparse>\") to create\nnew\nlines\nwithout\nrestrictions. \n\nRich Text Tags are still supported, but due to technical limitations, links only work on non-collapsable text areas.\n\nDifferent writing systems are also supported:\nA B C D   А Б В Г    Α Β Γ Δ    أ ب ت ث   א ב ג ד   山 水 火 木   あ い う え   가 나 다 라";

		private const string DemoMultiLineAutoGenerated = "This is another multi-line text area, but this one features auto-generated preview text when collapsed, with ellipses appearing when the text no longer fits. It also has an option enabled to collapse automatically when you switch off this settings tab. In other words, you will need to re-expand this text area each time you visit this tab.";

		private const string DemoExtendedByDefault = "This multi-line text area is expanded by default but can be collapsed if needed. It will retain its previous state when toggling this tab on and off.";

		private const string DemoExtendedEveryTime = "This multi-line text area is similar to the one above, but it will re-expand itself after collapsing each time you visit this tab.";

		private const string DemoExtendedPermanent = "This multi-line text area cannot be collapsed.\nIt remains fully expanded at all times, but supports URL links.\nExample link: <link=\"https://www.youtube.com/watch?v=dQw4w9WgXcQ\"><mark=#5865f215>[Click]</mark></link>";

		private SSButton _requestScanButton;

		private SSTextArea _responseScan;
	}
}
