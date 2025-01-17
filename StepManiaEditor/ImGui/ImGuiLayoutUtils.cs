﻿using System;
using System.Collections.Generic;
using System.Numerics;
using ImGuiNET;
using StepManiaEditor.AutogenConfig;
using StepManiaLibrary.PerformedChart;
using static Fumen.FumenExtensions;
using static StepManiaEditor.ImGuiUtils;
using static StepManiaLibrary.PerformedChart.Config;
using static StepManiaEditor.Utils;
using static Fumen.Converters.SMCommon;
using static StepManiaEditor.AutogenConfig.EditorPatternConfig;
using static StepManiaEditor.Editor;

namespace StepManiaEditor;

/// <summary>
/// This class offers methods for drawing complex elements using ImGui that typically use the following behavior:
///  - They can alter fields or properties on an object.
///  - They can be undoable, using EditorActions to perform edits.
///  - They can leverage cached state so that edits can occur in the UI before being committed to the underlying value.
///  - They are laid out using a table with a name on the left, an optional help tool tip marker, and a control on the right.
/// </summary>
internal sealed class ImGuiLayoutUtils
{
	/// <summary>
	/// Cache of values being edited through ImGui controls.
	/// We often need to edit a cached value instead of a real value
	///  Example: Text entry where we do not want to commit the text until editing is complete.
	/// We also often need to enqueue an undo action of a cached value from before editing began.
	///  Example: With a slider the value is changing continuously and we do not want to enqueue
	///  an event until the user releases the control and the before value in this case should be
	///  a previously cached value.
	/// </summary>
	private static readonly Dictionary<string, object> Cache = new();

	private static string CacheKeyPrefix = "";
	private const string DragHelpText = "\nShift+drag for large adjustments.\nAlt+drag for small adjustments.";

	private static ImFontPtr ImGuiFont;

	public static readonly float CheckBoxWidth = UiScaled(20);
	public static readonly float FileBrowseXWidth = UiScaled(20);
	public static readonly float FileBrowseBrowseWidth = UiScaled(50);
	public static readonly float FileBrowseAutoWidth = UiScaled(50);
	public static readonly float DisplayTempoEnumWidth = UiScaled(120);
	public static readonly float RangeToWidth = UiScaled(14);
	public static readonly float TextXWidth = UiScaled(8);
	public static readonly float TextYWidth = UiScaled(8);
	public static readonly float TextLatWidth = UiScaled(26);
	public static readonly float TextLongWidth = UiScaled(26);
	public static readonly float SliderResetWidth = UiScaled(50);
	public static readonly float ConfigFromListEditWidth = UiScaled(40);
	public static readonly float ConfigFromListViewAllWidth = UiScaled(60);
	public static readonly float ConfigFromListNewWidth = UiScaled(30);
	public static readonly float NoteTypeComboWidth = UiScaled(60);
	public static readonly float NotesFromTextWidth = UiScaled(60);
	public static readonly float BpmTextWidth = UiScaled(20);
	public static readonly float PatternConfigShortEnumWidth = UiScaled(160);
	public static readonly float ArrowIconWidth = UiScaled(16);
	public static readonly float ArrowIconHeight = UiScaled(16);
	public static readonly Vector2 ArrowIconSize = new(ArrowIconWidth, ArrowIconHeight);

	public static void SetFont(ImFontPtr font)
	{
		ImGuiFont = font;
	}

	public static bool BeginTable(string title, float titleColumnWidth)
	{
		CacheKeyPrefix = title ?? "";
		var ret = ImGui.BeginTable(title, 2, ImGuiTableFlags.None);
		if (ret)
		{
			ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, titleColumnWidth);
			ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthStretch, 100);
		}

		return ret;
	}

	public static bool BeginTable(string title, float titleColumnWidth, float contentColumnWidth)
	{
		CacheKeyPrefix = title ?? "";
		var ret = ImGui.BeginTable(title, 2, ImGuiTableFlags.None,
			new Vector2(titleColumnWidth + contentColumnWidth, -1.0f));
		if (ret)
		{
			ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, titleColumnWidth);
			ImGui.TableSetupColumn("", ImGuiTableColumnFlags.WidthFixed, contentColumnWidth);
		}

		return ret;
	}

	public static void EndTable()
	{
		ImGui.EndTable();
	}

	public static void DrawRowTitleAndAdvanceColumn(string title)
	{
		ImGui.TableNextRow();

		ImGui.TableSetColumnIndex(0);
		if (!string.IsNullOrEmpty(title))
			ImGui.Text(title);

		ImGui.TableSetColumnIndex(1);
	}

	public static void DrawTitle(string title, string help = null)
	{
		ImGui.TableNextRow();

		ImGui.TableSetColumnIndex(0);
		if (!string.IsNullOrEmpty(title))
			ImGui.Text(title);

		ImGui.TableSetColumnIndex(1);
		ImGui.SetNextItemWidth(DrawHelp(help, ImGui.GetContentRegionAvail().X));
	}

	public static void DrawTitleAndText(string title, string text, string help = null)
	{
		ImGui.TableNextRow();

		ImGui.TableSetColumnIndex(0);
		if (!string.IsNullOrEmpty(title))
			ImGui.Text(title);

		ImGui.TableSetColumnIndex(1);
		ImGui.SetNextItemWidth(DrawHelp(help, ImGui.GetContentRegionAvail().X));
		ImGui.SameLine();
		ImGui.Text(text);
	}

	private static string GetDragHelpText(string helpText)
	{
		return string.IsNullOrEmpty(helpText) ? null : helpText + DragHelpText;
	}

	#region Checkbox

	public static bool DrawRowCheckbox(string title, ref bool value, string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawCheckbox(title, ref value, ImGui.GetContentRegionAvail().X, help);
	}

	public static bool DrawRowCheckbox(bool undoable, string title, object o, string fieldName, bool affectsFile,
		string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawCheckbox(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, affectsFile, help);
	}

	private static bool DrawCheckbox(
		string title,
		ref bool value,
		float width,
		string help = null)
	{
		ImGui.SetNextItemWidth(DrawHelp(help, width));
		return ImGui.Checkbox(GetElementTitle(title), ref value);
	}

	private static bool DrawCheckbox(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		bool affectsFile,
		string help = null)
	{
		ImGui.SetNextItemWidth(DrawHelp(help, width));

		var value = GetValueFromFieldOrProperty<bool>(o, fieldName);
		var ret = ImGui.Checkbox(GetElementTitle(title, fieldName), ref value);
		if (ret)
		{
			if (undoable)
			{
				ActionQueue.Instance.Do(new ActionSetObjectFieldOrPropertyValue<bool>(o, fieldName, value, affectsFile));
			}
			else
			{
				SetFieldOrPropertyToValue(o, fieldName, value);
			}
		}

		return ret;
	}

	#endregion Checkbox

	#region Button

	public static bool DrawRowButton(string title, string buttonText, string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);
		ImGui.SetNextItemWidth(DrawHelp(help, ImGui.GetContentRegionAvail().X));
		return ImGui.Button(buttonText);
	}

	#endregion Button

	#region Text Input

	public static void DrawRowTextInput(string title, ref string value, string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);
		DrawTextInput(title, ref value, ImGui.GetContentRegionAvail().X, help);
	}

	private static void DrawTextInput(string title, ref string value, float width, string help = null)
	{
		ImGui.SetNextItemWidth(DrawHelp(help, width));
		ImGui.InputTextWithHint(GetElementTitle(title), title, ref value, 256);
	}

	public static void DrawRowTextInput(bool undoable, string title, object o, string fieldName, bool affectsFile,
		string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);
		DrawTextInput(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, affectsFile, null, help);
	}

	public static void DrawRowTextInput(bool undoable, string title, object o, string fieldName, bool affectsFile,
		Func<string, bool> validationFunc, string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);
		DrawTextInput(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, affectsFile, validationFunc, help);
	}

	public static void DrawRowTextInputWithTransliteration(bool undoable, string title, object o, string fieldName,
		string transliterationFieldName, bool affectsFile, string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);
		DrawTextInput(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X * 0.5f, affectsFile, null, help);
		ImGui.SameLine();
		DrawTextInput(undoable, "Transliteration", o, transliterationFieldName, ImGui.GetContentRegionAvail().X, affectsFile,
			null,
			"Optional text to use when sorting by this value.\nStepMania sorts values lexicographically, preferring transliterations.");
	}

	private static void DrawTextInput(bool undoable, string title, object o, string fieldName, float width,
		bool affectsFile, Func<string, bool> validationFunc = null, string help = null)
	{
		(bool, string) Func(string v)
		{
			v ??= "";
			var r = ImGui.InputTextWithHint(GetElementTitle(title, fieldName), title, ref v, 256);
			return (r, v);
		}

		DrawCachedEditReference(undoable, title, o, fieldName, width, affectsFile, Func, StringCompare, validationFunc, help);
	}

	#endregion Text Input

	#region Time Signature

	private static void DrawTimeSignatureInput(bool undoable, string title, object o, string fieldName, float width,
		bool affectsFile, string help = null)
	{
		(bool, string) Func(string v)
		{
			v ??= "";
			var r = ImGui.InputText(GetElementTitle(title, fieldName), ref v, 256);
			return (r, v);
		}

		bool ValidationFunc(string v)
		{
			var (valid, _) = EditorTimeSignatureEvent.IsValidTimeSignatureString(v);
			return valid;
		}

		DrawCachedEditReference<string>(undoable, title, o, fieldName, width, affectsFile, Func, StringCompare, ValidationFunc,
			help);
	}

	#endregion Time Signature

	#region Multipliers

	private static void DrawMultipliersInput(bool undoable, string title, object o, string fieldName, float width,
		bool affectsFile, string help = null)
	{
		(bool, string) Func(string v)
		{
			v ??= "";
			var r = ImGui.InputText(GetElementTitle(title, fieldName), ref v, 256);
			return (r, v);
		}

		bool ValidationFunc(string v)
		{
			var (valid, _, _) = EditorMultipliersEvent.IsValidMultipliersString(v);
			return valid;
		}

		DrawCachedEditReference<string>(undoable, title, o, fieldName, width, affectsFile, Func, StringCompare, ValidationFunc,
			help);
	}

	#endregion Multipliers

	#region Label

	private static void DrawLabelInput(bool undoable, string title, object o, string fieldName, float width,
		bool affectsFile, string help = null)
	{
		(bool, string) Func(string v)
		{
			v ??= "";
			var r = ImGui.InputText(GetElementTitle(title, fieldName), ref v, 256);
			return (r, v);
		}

		// Consider all input valid. Assume the EditorLabelEvent will sanitize input.
		bool ValidationFunc(string v)
		{
			return true;
		}

		DrawCachedEditReference<string>(undoable, title, o, fieldName, width, affectsFile, Func, StringCompare, ValidationFunc,
			help);
	}

	#endregion Label

	#region Scroll Rate Interpolation

	private static void DrawScrollRateInterpolationInput(bool undoable, string title, object o, string fieldName, float width,
		bool affectsFile, string help = null)
	{
		(bool, string) Func(string v)
		{
			v ??= "";
			var r = ImGui.InputText(GetElementTitle(title, fieldName), ref v, 256);
			return (r, v);
		}

		bool ValidationFunc(string v)
		{
			var (valid, _, _, _, _) = EditorInterpolatedRateAlteringEvent.IsValidScrollRateInterpolationString(v);
			return valid;
		}

		DrawCachedEditReference<string>(undoable, title, o, fieldName, width, affectsFile, Func, StringCompare, ValidationFunc,
			help);
	}

	#endregion Scroll Rate Interpolation

	#region File Browse

	public static void DrawRowFileBrowse(
		string title, object o, string fieldName, Action browseAction, Action clearAction, bool affectsFile, string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);

		// Display the text input but do not allow edits to it.
		PushDisabled();
		DrawTextInput(false, title, o, fieldName,
			Math.Max(1.0f,
				ImGui.GetContentRegionAvail().X - (FileBrowseXWidth + FileBrowseBrowseWidth) -
				ImGui.GetStyle().ItemSpacing.X * 2), affectsFile, null, help);
		PopDisabled();

		ImGui.SameLine();
		if (ImGui.Button($"X{GetElementTitle(title, fieldName)}", new Vector2(FileBrowseXWidth, 0.0f)))
		{
			clearAction();
		}

		ImGui.SameLine();
		if (ImGui.Button($"Browse{GetElementTitle(title, fieldName)}", new Vector2(FileBrowseBrowseWidth, 0.0f)))
		{
			browseAction();
		}
	}

	public static void DrawRowAutoFileBrowse(
		string title, object o, string fieldName, Action autoAction, Action browseAction, Action clearAction,
		bool affectsFile, string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);

		// Display the text input but do not allow edits to it.
		PushDisabled();
		DrawTextInput(false, title, o, fieldName,
			Math.Max(1.0f,
				ImGui.GetContentRegionAvail().X - (FileBrowseXWidth + FileBrowseAutoWidth + FileBrowseBrowseWidth) -
				ImGui.GetStyle().ItemSpacing.X * 3), affectsFile, null, help);
		PopDisabled();

		ImGui.SameLine();
		if (ImGui.Button($"X{GetElementTitle(title, fieldName)}", new Vector2(FileBrowseXWidth, 0.0f)))
		{
			clearAction();
		}

		ImGui.SameLine();
		if (ImGui.Button($"Auto{GetElementTitle(title, fieldName)}", new Vector2(FileBrowseAutoWidth, 0.0f)))
		{
			autoAction();
		}

		ImGui.SameLine();
		if (ImGui.Button($"Browse{GetElementTitle(title, fieldName)}", new Vector2(FileBrowseBrowseWidth, 0.0f)))
		{
			browseAction();
		}
	}

	#endregion File Browse

	#region Config From List

	public static bool DrawSelectableConfigFromList(
		string title,
		string elementName,
		ref int selectedIndex,
		string[] values,
		Action editAction,
		Action viewAllAction,
		Action newAction,
		string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var elementTitle = GetElementTitle(title, elementName);

		var itemWidth = DrawHelp(help, ImGui.GetContentRegionAvail().X);
		var spacing = ImGui.GetStyle().ItemSpacing.X;
		var comboWidth = Math.Max(1.0f,
			itemWidth - ConfigFromListEditWidth - ConfigFromListViewAllWidth - ConfigFromListNewWidth - spacing * 3.0f);
		ImGui.SetNextItemWidth(comboWidth);

		var ret = ComboFromArray(elementTitle, ref selectedIndex, values);

		ImGui.SameLine();
		if (ImGui.Button($"Edit{elementTitle}", new Vector2(ConfigFromListEditWidth, 0.0f)))
		{
			editAction();
		}

		ImGui.SameLine();
		if (ImGui.Button($"View All{elementTitle}", new Vector2(ConfigFromListViewAllWidth, 0.0f)))
		{
			viewAllAction();
		}

		ImGui.SameLine();
		if (ImGui.Button($"New{elementTitle}", new Vector2(ConfigFromListNewWidth, 0.0f)))
		{
			newAction();
		}

		return ret;
	}

	public static bool DrawExpressedChartConfig(EditorChart editorChart, string title, string help)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var fieldName = nameof(EditorChart.ExpressedChartConfig);

		var elementTitle = GetElementTitle(title, fieldName);

		var itemWidth = DrawHelp(help, ImGui.GetContentRegionAvail().X);
		var spacing = ImGui.GetStyle().ItemSpacing.X;
		var comboWidth = Math.Max(1.0f,
			itemWidth - ConfigFromListEditWidth - ConfigFromListViewAllWidth - ConfigFromListNewWidth - spacing * 3.0f);
		ImGui.SetNextItemWidth(comboWidth);

		var currentValue = editorChart?.ExpressedChartConfig ??
		                   ExpressedChartConfigManager.DefaultExpressedChartDynamicConfigGuid;
		var configGuids = ExpressedChartConfigManager.Instance.GetSortedConfigGuids();
		var configNames = ExpressedChartConfigManager.Instance.GetSortedConfigNames();
		var selectedIndex = 0;
		for (var i = 0; i < configGuids.Length; i++)
		{
			if (configGuids[i].Equals(currentValue))
			{
				selectedIndex = i;
				break;
			}
		}

		var ret = ComboFromArray(elementTitle, ref selectedIndex, configNames);
		if (ret)
		{
			var newValue = configGuids[selectedIndex];
			if (!newValue.Equals(currentValue))
			{
				ActionQueue.Instance.Do(
					new ActionSetObjectFieldOrPropertyValue<Guid>(editorChart, fieldName, newValue, true));
			}
		}

		ImGui.SameLine();
		if (ImGui.Button($"Edit{elementTitle}", new Vector2(ConfigFromListEditWidth, 0.0f)))
		{
			if (editorChart != null)
				EditorExpressedChartConfig.ShowEditUI(editorChart.ExpressedChartConfig);
		}

		ImGui.SameLine();
		if (ImGui.Button($"View All{elementTitle}", new Vector2(ConfigFromListViewAllWidth, 0.0f)))
		{
			Preferences.Instance.ShowAutogenConfigsWindow = true;
			ImGui.SetWindowFocus(UIAutogenConfigs.WindowTitle);
		}

		ImGui.SameLine();
		if (ImGui.Button($"New{elementTitle}", new Vector2(ConfigFromListNewWidth, 0.0f)))
		{
			EditorExpressedChartConfig.CreateNewConfigAndShowEditUI(editorChart);
		}

		return ret;
	}

	#endregion Config From List

	#region Input Int

	public static void DrawRowInputInt(
		bool undoable,
		string title,
		object o,
		string fieldName,
		bool affectsFile,
		string help = null,
		int min = int.MinValue,
		int max = int.MaxValue)
	{
		DrawRowTitleAndAdvanceColumn(title);
		DrawInputInt(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, affectsFile, help, min, max);
	}

	private static void DrawInputInt(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		bool affectsFile,
		string help = null,
		int min = int.MinValue,
		int max = int.MaxValue)
	{
		(bool, int) Func(int v)
		{
			var r = InputInt(GetElementTitle(title, fieldName), ref v, min, max);
			return (r, v);
		}

		DrawLiveEditValue<int>(undoable, title, o, fieldName, width, affectsFile, Func, IntCompare, help);
	}

	#endregion Input Int

	#region Drag Int

	public static void DrawRowDragInt2(
		bool undoable,
		string title,
		object o,
		string fieldName1,
		string fieldName2,
		bool affectsFile,
		bool field1Enabled = true,
		bool field2Enabled = true,
		string help = null,
		float speed = 1.0f,
		string format = "%i",
		int min1 = int.MinValue,
		int max1 = int.MaxValue,
		int min2 = int.MinValue,
		int max2 = int.MaxValue)
	{
		DrawRowTitleAndAdvanceColumn(title);
		if (!field1Enabled)
			PushDisabled();
		DrawDragInt(undoable, "", o, fieldName1, ImGui.GetContentRegionAvail().X * 0.5f, affectsFile, help, speed, format, min1,
			max1);
		if (!field1Enabled)
			PopDisabled();
		ImGui.SameLine();
		if (!field2Enabled)
			PushDisabled();
		DrawDragInt(undoable, "", o, fieldName2, ImGui.GetContentRegionAvail().X, affectsFile, "", speed, format, min2, max2);
		if (!field2Enabled)
			PopDisabled();
	}

	public static void DrawRowDragInt2(
		bool undoable,
		string title,
		object o,
		string fieldName1,
		string fieldName2,
		bool affectsFile,
		string field1Title,
		string field2Title,
		float fieldTitleWidth,
		string help = null,
		float speed = 1.0f,
		string format = "%i",
		int min1 = int.MinValue,
		int max1 = int.MaxValue,
		int min2 = int.MinValue,
		int max2 = int.MaxValue)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var totalWidth = DrawHelp(help, ImGui.GetContentRegionAvail().X);
		var dragIntWidth = (totalWidth - 2 * fieldTitleWidth - ImGui.GetStyle().ItemSpacing.X * 3) * 0.5f;

		Text(field1Title, fieldTitleWidth);
		ImGui.SameLine();

		DrawDragInt(undoable, "", o, fieldName1, dragIntWidth, affectsFile, null, speed, format, min1, max1);
		ImGui.SameLine();

		Text(field2Title, fieldTitleWidth);
		ImGui.SameLine();

		DrawDragInt(undoable, "", o, fieldName2, dragIntWidth, affectsFile, null, speed, format, min2, max2);
	}

	public static bool DrawRowDragInt(
		string title,
		ref int value,
		string help = null,
		float speed = 1.0f,
		string format = "%i",
		int min = int.MinValue,
		int max = int.MaxValue)
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawDragInt(title, ref value, ImGui.GetContentRegionAvail().X, help, speed, format, min, max);
	}

	private static bool DrawDragInt(
		string title,
		ref int value,
		float width,
		string help,
		float speed,
		string format,
		int min = int.MinValue,
		int max = int.MaxValue)
	{
		var itemWidth = DrawHelp(GetDragHelpText(help), width);
		ImGui.SetNextItemWidth(itemWidth);
		return DragInt(ref value, GetElementTitle(title), speed, format, min, max);
	}

	public static bool DrawRowDragInt(
		bool undoable,
		string title,
		object o,
		string fieldName,
		bool affectsFile,
		string help = null,
		float speed = 1.0f,
		string format = "%i",
		int min = int.MinValue,
		int max = int.MaxValue)
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawDragInt(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, affectsFile, help, speed, format, min,
			max);
	}

	private static bool DrawDragInt(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		bool affectsFile,
		string help,
		float speed,
		string format,
		int min = int.MinValue,
		int max = int.MaxValue)
	{
		(bool, int) Func(int v)
		{
			var r = DragInt(ref v, GetElementTitle(title, fieldName), speed, format, min, max);
			return (r, v);
		}

		return DrawLiveEditValue<int>(undoable, title, o, fieldName, width, affectsFile, Func, IntCompare, GetDragHelpText(help));
	}

	public static void DrawDragIntRange(
		bool undoable,
		string title,
		object o,
		string beginFieldName,
		string endFieldName,
		bool affectsFile,
		float width,
		string help = null,
		float speed = 1.0f,
		string format = "%i",
		int min = int.MinValue,
		int max = int.MaxValue)
	{
		var remainingWidth = DrawHelp(GetDragHelpText(help), width);
		var controlWidth = Math.Max(0.0f, 0.5f * (remainingWidth - ImGui.GetStyle().ItemSpacing.X * 2.0f - RangeToWidth));

		var currentBeginValue = GetValueFromFieldOrProperty<int>(o, beginFieldName);
		var currentEndValue = GetValueFromFieldOrProperty<int>(o, endFieldName);
		var maxForBegin = Math.Min(max, currentEndValue);
		var minForEnd = Math.Max(min, currentBeginValue);

		// Controls for the int values.
		ImGui.SameLine();
		DrawDragInt(undoable, title, o, beginFieldName, controlWidth, affectsFile, "", speed, format, min, maxForBegin);

		// "to" text
		ImGui.SameLine();
		Text("to", RangeToWidth);

		ImGui.SameLine();
		DrawDragInt(undoable, title, o, endFieldName, controlWidth, affectsFile, "", speed, format, minForEnd, max);
	}

	public static void DrawRowDragIntWithEnabledCheckbox(
		bool undoable,
		string title,
		object o,
		string fieldName,
		string enabledFieldName,
		bool affectsFile,
		string help = null,
		float speed = 1.0f,
		string format = "%i",
		int min = int.MinValue,
		int max = int.MaxValue)
	{
		title ??= "";

		DrawRowTitleAndAdvanceColumn(title);

		bool enabled;

		var controlWidth = ImGui.GetContentRegionAvail().X - CheckBoxWidth - ImGui.GetStyle().ItemSpacing.X;

		// Draw the checkbox for enabling the other control.
		if (DrawCheckbox(false, title + "check", o, enabledFieldName, CheckBoxWidth, affectsFile, GetDragHelpText(help)))
		{
			if (undoable)
			{
				enabled = GetValueFromFieldOrProperty<bool>(o, enabledFieldName);
				ActionQueue.Instance.Do(new ActionSetObjectFieldOrPropertyValue<bool>(
					o, enabledFieldName, enabled, !enabled, affectsFile));
			}
		}

		enabled = GetValueFromFieldOrProperty<bool>(o, enabledFieldName);
		if (!enabled)
			PushDisabled();

		// Control for the int value.
		ImGui.SameLine();
		DrawDragInt(undoable, title, o, fieldName, controlWidth, affectsFile, null, speed, format, min, max);

		if (!enabled)
			PopDisabled();
	}

	#endregion Drag Int

	#region Slider Int

	public static bool DrawRowSliderInt(
		bool undoable,
		string title,
		object o,
		string fieldName,
		int min,
		int max,
		bool affectsFile,
		string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawSliderInt(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, min, max, affectsFile, help);
	}

	private static bool DrawSliderInt(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		int min,
		int max,
		bool affectsFile,
		string help = null)
	{
		(bool, int) Func(int v)
		{
			var r = ImGui.SliderInt(GetElementTitle(title, fieldName), ref v, min, max);
			return (r, v);
		}

		return DrawLiveEditValue<int>(undoable, title, o, fieldName, width, affectsFile, Func, IntCompare, help);
	}

	#endregion Slider Int

	#region Slider UInt

	public static bool DrawRowSliderUInt(
		bool undoable,
		string title,
		object o,
		string fieldName,
		uint min,
		uint max,
		bool affectsFile,
		string format = null,
		ImGuiSliderFlags flags = ImGuiSliderFlags.None,
		string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawSliderUInt(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, min, max, affectsFile, format,
			flags, help);
	}

	private static bool DrawSliderUInt(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		uint min,
		uint max,
		bool affectsFile,
		string format = null,
		ImGuiSliderFlags flags = ImGuiSliderFlags.None,
		string help = null)
	{
		(bool, uint) Func(uint v)
		{
			var r = SliderUInt(GetElementTitle(title, fieldName), ref v, min, max, format, flags);
			return (r, v);
		}

		return DrawLiveEditValue<uint>(undoable, title, o, fieldName, width, affectsFile, Func, UIntCompare, help);
	}

	#endregion Slider UInt

	#region Slider Float

	public static bool DrawRowSliderFloat(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float min,
		float max,
		bool affectsFile,
		string help = null,
		string format = "%.3f",
		ImGuiSliderFlags flags = ImGuiSliderFlags.None)
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawSliderFloat(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, min, max, affectsFile, help,
			format, flags);
	}

	private static bool DrawSliderFloat(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		float min,
		float max,
		bool affectsFile,
		string help = null,
		string format = "%.3f",
		ImGuiSliderFlags flags = ImGuiSliderFlags.None)
	{
		(bool, float) Func(float v)
		{
			var r = ImGui.SliderFloat(GetElementTitle(title, fieldName), ref v, min, max, format, flags);
			return (r, v);
		}

		return DrawLiveEditValue<float>(undoable, title, o, fieldName, width, affectsFile, Func, FloatCompare, help);
	}

	public static void DrawRowSliderFloatWithReset(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float min,
		float max,
		float resetValue,
		bool affectsFile,
		string help = null,
		string format = "%.3f",
		ImGuiSliderFlags flags = ImGuiSliderFlags.None)
	{
		DrawRowTitleAndAdvanceColumn(title);

		// Slider
		DrawSliderFloat(undoable, title, o, fieldName,
			ImGui.GetContentRegionAvail().X - SliderResetWidth - ImGui.GetStyle().ItemSpacing.X, min, max, affectsFile, help,
			format, flags);

		// Reset
		ImGui.SameLine();
		if (ImGui.Button($"Reset{GetElementTitle(title, fieldName)}", new Vector2(SliderResetWidth, 0.0f)))
		{
			var value = GetValueFromFieldOrProperty<float>(o, fieldName);
			if (!resetValue.FloatEquals(value))
			{
				if (undoable)
					ActionQueue.Instance.Do(
						new ActionSetObjectFieldOrPropertyValue<float>(o, fieldName, resetValue, affectsFile));
				else
					SetFieldOrPropertyToValue(o, fieldName, resetValue);
			}
		}
	}

	#endregion Slider Float

	#region Drag Float

	public static bool DrawRowDragFloat(
		string title,
		ref float value,
		string help = null,
		float speed = 0.0001f,
		string format = "%.6f",
		float min = float.MinValue,
		float max = float.MaxValue,
		ImGuiSliderFlags flags = ImGuiSliderFlags.None)
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawDragFloat(title, ref value, ImGui.GetContentRegionAvail().X, help, speed, format, min, max, flags);
	}

	private static bool DrawDragFloat(
		string title,
		ref float value,
		float width,
		string help,
		float speed,
		string format,
		float min = float.MinValue,
		float max = float.MaxValue,
		ImGuiSliderFlags flags = ImGuiSliderFlags.None)
	{
		var itemWidth = DrawHelp(GetDragHelpText(help), width);
		ImGui.SetNextItemWidth(itemWidth);
		return ImGui.DragFloat(GetElementTitle(title), ref value, speed, min, max, format, flags);
	}

	public static bool DrawRowDragFloat(
		bool undoable,
		string title,
		object o,
		string fieldName,
		bool affectsFile,
		string help = null,
		float speed = 0.0001f,
		string format = "%.6f",
		float min = float.MinValue,
		float max = float.MaxValue,
		ImGuiSliderFlags flags = ImGuiSliderFlags.None)
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawDragFloat(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, help, speed, format, affectsFile,
			min, max, flags);
	}

	public static void DrawRowDragFloatWithEnabledCheckbox(
		bool undoable,
		string title,
		object o,
		string fieldName,
		string enabledFieldName,
		bool affectsFile,
		string help = null,
		float speed = 0.0001f,
		string format = "%.6f",
		float min = float.MinValue,
		float max = float.MaxValue,
		ImGuiSliderFlags flags = ImGuiSliderFlags.None)
	{
		title ??= "";

		DrawRowTitleAndAdvanceColumn(title);

		bool enabled;

		var controlWidth = ImGui.GetContentRegionAvail().X - CheckBoxWidth - ImGui.GetStyle().ItemSpacing.X;

		// Draw the checkbox for enabling the other control.
		if (DrawCheckbox(false, title + "check", o, enabledFieldName, CheckBoxWidth, affectsFile, GetDragHelpText(help)))
		{
			if (undoable)
			{
				enabled = GetValueFromFieldOrProperty<bool>(o, enabledFieldName);

				// If disabling the checkbox enqueue an action for undoing both the bool
				// and the setting of the float value to 0.
				if (!enabled)
				{
					var multiple = new ActionMultiple();
					multiple.EnqueueAndDo(new ActionSetObjectFieldOrPropertyValue<float>(o,
						fieldName, 0.0f, GetValueFromFieldOrProperty<float>(o, fieldName), affectsFile));
					multiple.EnqueueAndDo(new ActionSetObjectFieldOrPropertyValue<bool>(o,
						enabledFieldName, false, true, affectsFile));
					ActionQueue.Instance.EnqueueWithoutDoing(multiple);
				}

				// If enabling the checkbox we only need to enqueue an action for the checkbox bool.
				else
				{
					ActionQueue.Instance.Do(new ActionSetObjectFieldOrPropertyValue<bool>(
						o,
						enabledFieldName, true, false, affectsFile));
				}
			}
		}

		enabled = GetValueFromFieldOrProperty<bool>(o, enabledFieldName);
		if (!enabled)
			PushDisabled();

		// Control for the float value.
		ImGui.SameLine();
		DrawDragFloat(undoable, title, o, fieldName, controlWidth, null, speed, format, affectsFile, min, max, flags);

		if (!enabled)
			PopDisabled();
	}

	private static bool DrawDragFloat(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		string help,
		float speed,
		string format,
		bool affectsFile,
		float min = float.MinValue,
		float max = float.MaxValue,
		ImGuiSliderFlags flags = ImGuiSliderFlags.None)
	{
		(bool, float) Func(float v)
		{
			var r = ImGui.DragFloat(GetElementTitle(title, fieldName), ref v, speed, min, max, format, flags);
			return (r, v);
		}

		return DrawLiveEditValue<float>(undoable, title, o, fieldName, width, affectsFile, Func, FloatCompare,
			GetDragHelpText(help));
	}

	#endregion Drag Float

	#region Drag Drouble

	public static bool DrawRowDragDouble(
		string title,
		ref double value,
		string help = null,
		float speed = 0.0001f,
		string format = "%.6f",
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawDragDouble(title, ref value, ImGui.GetContentRegionAvail().X, help, speed, format, min, max);
	}

	private static bool DrawDragDouble(
		string title,
		ref double value,
		float width,
		string help,
		float speed,
		string format,
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		var itemWidth = DrawHelp(GetDragHelpText(help), width);
		ImGui.SetNextItemWidth(itemWidth);
		return DragDouble(ref value, GetElementTitle(title), speed, format, min, max);
	}

	public static bool DrawRowDragDouble(
		bool undoable,
		string title,
		object o,
		string fieldName,
		bool affectsFile,
		string help = null,
		float speed = 0.0001f,
		string format = "%.6f",
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawDragDouble(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, help, speed, format, affectsFile,
			min, max);
	}

	public static void DrawRowDragDoubleWithEnabledCheckbox(
		bool undoable,
		string title,
		object o,
		string fieldName,
		string enabledFieldName,
		bool affectsFile,
		string help = null,
		float speed = 0.0001f,
		string format = "%.6f",
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		title ??= "";

		DrawRowTitleAndAdvanceColumn(title);

		bool enabled;

		var controlWidth = ImGui.GetContentRegionAvail().X - CheckBoxWidth - ImGui.GetStyle().ItemSpacing.X;

		// Draw the checkbox for enabling the other control.
		if (DrawCheckbox(false, title + "check", o, enabledFieldName, CheckBoxWidth, affectsFile, GetDragHelpText(help)))
		{
			if (undoable)
			{
				enabled = GetValueFromFieldOrProperty<bool>(o, enabledFieldName);
				ActionQueue.Instance.Do(new ActionSetObjectFieldOrPropertyValue<bool>(
					o, enabledFieldName, enabled, !enabled, affectsFile));
			}
		}

		enabled = GetValueFromFieldOrProperty<bool>(o, enabledFieldName);
		if (!enabled)
			PushDisabled();

		// Control for the double value.
		ImGui.SameLine();
		DrawDragDouble(undoable, title, o, fieldName, controlWidth, null, speed, format, affectsFile, min, max);

		if (!enabled)
			PopDisabled();
	}

	public static void DrawRowDragDoubleWithOneButton(
		bool undoable,
		string title,
		object o,
		string fieldName,
		bool affectsFile,
		Action action,
		string text,
		float width,
		bool enabled,
		string help = null,
		float speed = 0.0001f,
		string format = "%.6f",
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var dragDoubleWidth = ImGui.GetContentRegionAvail().X - width - ImGui.GetStyle().ItemSpacing.X;
		DrawDragDouble(undoable, title, o, fieldName, dragDoubleWidth, help, speed, format, affectsFile, min, max);

		ImGui.SameLine();
		if (!enabled)
			PushDisabled();
		if (ImGui.Button($"{text}{GetElementTitle(title, fieldName)}", new Vector2(width, 0.0f)))
		{
			action();
		}

		if (!enabled)
			PopDisabled();
	}

	public static void DrawRowDragDoubleWithTwoButtons(
		bool undoable,
		string title,
		object o,
		string fieldName,
		bool affectsFile,
		Action action1,
		string text1,
		float width1,
		Action action2,
		string text2,
		float width2,
		string help = null,
		float speed = 0.0001f,
		string format = "%.6f",
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var dragDoubleWidth = ImGui.GetContentRegionAvail().X - width1 - width2 - ImGui.GetStyle().ItemSpacing.X * 2;
		DrawDragDouble(undoable, title, o, fieldName, dragDoubleWidth, help, speed, format, affectsFile, min, max);

		ImGui.SameLine();
		if (ImGui.Button($"{text1}{GetElementTitle(title, fieldName)}", new Vector2(width1, 0.0f)))
		{
			action1();
		}

		ImGui.SameLine();
		if (ImGui.Button($"{text2}{GetElementTitle(title, fieldName)}", new Vector2(width2, 0.0f)))
		{
			action2();
		}
	}

	private static bool DrawDragDouble(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		string help,
		float speed,
		string format,
		bool affectsFile,
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		(bool, double) Func(double v)
		{
			var r = DragDouble(ref v, GetElementTitle(title, fieldName), speed, format, min, max);
			return (r, v);
		}

		return DrawLiveEditValue<double>(undoable, title, o, fieldName, width, affectsFile, Func, DoubleCompare,
			GetDragHelpText(help));
	}

	// ReSharper disable once UnusedMethodReturnValue.Local
	private static bool DrawDragDoubleCached(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		string help,
		float speed,
		string format,
		bool affectsFile,
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		(bool, double) Func(double v)
		{
			var r = DragDouble(ref v, GetElementTitle(title, fieldName), speed, format, min, max);
			return (r, v);
		}

		bool ValidationFunc(double v)
		{
			return true;
		}

		return DrawCachedEditValue<double>(undoable, title, o, fieldName, width, affectsFile, Func, DoubleCompare, ValidationFunc,
			GetDragHelpText(help));
	}

	public static void DrawRowDragDoubleRange(
		bool undoable,
		string title,
		object o,
		string beginFieldName,
		string endFieldName,
		bool affectsFile,
		string help = null,
		float speed = 0.0001f,
		string format = "%.6f",
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		title ??= "";

		DrawRowTitleAndAdvanceColumn(title);

		var remainingWidth = DrawHelp(GetDragHelpText(help), ImGui.GetContentRegionAvail().X);

		DrawDragDoubleRange(undoable, title, o, beginFieldName, endFieldName, affectsFile, remainingWidth, speed, format, min,
			max);
	}

	public static void DrawDragDoubleRange(
		bool undoable,
		string title,
		object o,
		string beginFieldName,
		string endFieldName,
		bool affectsFile,
		float width,
		float speed = 0.0001f,
		string format = "%.6f",
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		var currentBeginValue = GetValueFromFieldOrProperty<double>(o, beginFieldName);
		var currentEndValue = GetValueFromFieldOrProperty<double>(o, endFieldName);
		var maxForBegin = Math.Min(max, currentEndValue);
		var minForEnd = Math.Max(min, currentBeginValue);

		var controlWidth = Math.Max(0.0f, 0.5f * (width - ImGui.GetStyle().ItemSpacing.X * 2.0f - RangeToWidth));

		// Controls for the double values.
		ImGui.SameLine();
		DrawDragDouble(undoable, title, o, beginFieldName, controlWidth, null, speed, format, affectsFile, min, maxForBegin);

		// "to" text
		ImGui.SameLine();
		Text("to", RangeToWidth);

		ImGui.SameLine();
		DrawDragDouble(undoable, title, o, endFieldName, controlWidth, null, speed, format, affectsFile, minForEnd, max);
	}

	public static void DrawRowDragDoubleXY(
		bool undoable,
		string title,
		object o,
		string xFieldName,
		string yFieldName,
		bool affectsFile,
		string help = null,
		float speed = 0.0001f,
		string format = "%.6f",
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		title ??= "";

		DrawRowTitleAndAdvanceColumn(title);

		var remainingWidth = DrawHelp(GetDragHelpText(help), ImGui.GetContentRegionAvail().X);
		var controlWidth = Math.Max(0.0f,
			0.5f * (remainingWidth - ImGui.GetStyle().ItemSpacing.X * 3.0f - TextXWidth - TextYWidth));

		// X text
		ImGui.SameLine();
		Text("X", TextXWidth);

		// Control for X.
		ImGui.SameLine();
		DrawDragDouble(undoable, title, o, xFieldName, controlWidth, null, speed, format, affectsFile, min, max);

		// Y text
		ImGui.SameLine();
		Text("Y", TextYWidth);

		// Control for Y
		ImGui.SameLine();
		DrawDragDouble(undoable, title, o, yFieldName, controlWidth, null, speed, format, affectsFile, min, max);
	}

	public static void DrawRowDragDoubleLatitudeLongitude(
		bool undoable,
		string title,
		object o,
		string latFieldName,
		string longFieldName,
		bool affectsFile,
		string help = null,
		float speed = 0.0001f,
		string format = "%.6f",
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		title ??= "";

		DrawRowTitleAndAdvanceColumn(title);

		var remainingWidth = DrawHelp(GetDragHelpText(help), ImGui.GetContentRegionAvail().X);
		var controlWidth = Math.Max(0.0f,
			0.5f * (remainingWidth - ImGui.GetStyle().ItemSpacing.X * 3.0f - TextLatWidth - TextLongWidth));

		// Latitude text
		ImGui.SameLine();
		Text("Lat", TextLatWidth);

		// Control for X.
		ImGui.SameLine();
		DrawDragDouble(undoable, title, o, latFieldName, controlWidth, null, speed, format, affectsFile, min, max);

		// Longitude text
		ImGui.SameLine();
		Text("Long", TextLongWidth);

		// Control for Y
		ImGui.SameLine();
		DrawDragDouble(undoable, title, o, longFieldName, controlWidth, null, speed, format, affectsFile, min, max);
	}

	#endregion Drag Double

	#region Enum

	public static bool DrawRowEnum<T>(string title, string elementName, ref T value, T[] allowedValues = null, string help = null)
		where T : struct, Enum
	{
		DrawRowTitleAndAdvanceColumn(title);
		ImGui.SetNextItemWidth(DrawHelp(help, ImGui.GetContentRegionAvail().X));

		if (allowedValues != null)
			return ComboFromEnum(GetElementTitle(title, elementName), ref value, allowedValues,
				GetElementTitle(title, elementName));
		return ComboFromEnum(GetElementTitle(title, elementName), ref value);
	}

	public static bool DrawRowEnum<T>(bool undoable, string title, object o, string fieldName, bool affectsFile,
		string help = null, T defaultValue = default)
		where T : struct, Enum
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawEnum(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, null, affectsFile, help, defaultValue);
	}

	public static bool DrawRowEnum<T>(bool undoable, string title, object o, string fieldName, T[] allowedValues,
		bool affectsFile, string help = null, T defaultValue = default) where T : struct, Enum
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawEnum(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, allowedValues, affectsFile, help,
			defaultValue);
	}

	public static bool DrawEnum<T>(bool undoable, string title, object o, string fieldName, float width, T[] allowedValues,
		bool affectsFile, string help = null, T defaultValue = default) where T : struct, Enum
	{
		var value = defaultValue;
		if (o != null)
			value = GetValueFromFieldOrProperty<T>(o, fieldName);

		var itemWidth = DrawHelp(help, width);
		ImGui.SetNextItemWidth(itemWidth);
		var newValue = value;
		bool ret;
		if (allowedValues != null)
			ret = ComboFromEnum(GetElementTitle(title, fieldName), ref newValue, allowedValues,
				GetElementTitle(title, fieldName));
		else
			ret = ComboFromEnum(GetElementTitle(title, fieldName), ref newValue);
		if (ret)
		{
			if (!newValue.Equals(value))
			{
				if (undoable)
					ActionQueue.Instance.Do(
						new ActionSetObjectFieldOrPropertyValue<T>(o, fieldName, newValue, value, affectsFile));
				else
					SetFieldOrPropertyToValue(o, fieldName, newValue);
			}
		}

		return ret;
	}

	#endregion Enum

	#region Selectable

	public static bool DrawRowSelectableTree<T>(bool undoable, string title, object o, string fieldName, bool affectsFile,
		string help = null)
		where T : Enum
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawSelectableTree<T>(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, affectsFile, help);
	}

	public static bool DrawSelectableTree<T>(bool undoable, string title, object o, string fieldName, float width,
		bool affectsFile, string help = null) where T : Enum
	{
		var value = GetValueFromFieldOrProperty<bool[]>(o, fieldName);

		var itemWidth = DrawHelp(help, width);
		ImGui.SetNextItemWidth(itemWidth);

		var (ret, originalValues) = SelectableTree<T>(title, ref value);
		if (ret && undoable)
			ActionQueue.Instance.Do(
				new ActionSetObjectFieldOrPropertyReference<bool[]>(o, fieldName, (bool[])value.Clone(), originalValues,
					affectsFile));

		return ret;
	}

	public static bool DrawRowSelectableTree<T>(bool undoable, string title, object o, string fieldName, bool affectsFile,
		T[] validChoices, string help = null)
		where T : Enum
	{
		DrawRowTitleAndAdvanceColumn(title);
		return DrawSelectableTree(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, affectsFile, validChoices,
			help);
	}

	public static bool DrawSelectableTree<T>(bool undoable, string title, object o, string fieldName, float width,
		bool affectsFile, T[] validChoices, string help = null) where T : Enum
	{
		var value = GetValueFromFieldOrProperty<bool[]>(o, fieldName);

		var itemWidth = DrawHelp(help, width);
		ImGui.SetNextItemWidth(itemWidth);

		var (ret, originalValues) = SelectableTree(title, validChoices, ref value);
		if (ret && undoable)
			ActionQueue.Instance.Do(
				new ActionSetObjectFieldOrPropertyReference<bool[]>(o, fieldName, (bool[])value.Clone(), originalValues,
					affectsFile));

		return ret;
	}

	#endregion Selectable

	#region Color Edit 3

	public static void DrawRowColorEdit3(
		bool undoable,
		string title,
		object o,
		string fieldName,
		ImGuiColorEditFlags flags,
		bool affectsFile,
		string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);
		DrawColorEdit3(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, flags, affectsFile, help);
	}

	private static void DrawColorEdit3(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		ImGuiColorEditFlags flags,
		bool affectsFile,
		string help = null)
	{
		(bool, Vector3) Func(Vector3 v)
		{
			var r = ImGui.ColorEdit3(GetElementTitle(title, fieldName), ref v, flags);
			return (r, v);
		}

		DrawLiveEditValue<Vector3>(undoable, title, o, fieldName, width, affectsFile, Func, Vector3Compare, help);
	}

	#endregion Color Edit 3

	#region Color Edit 4

	public static void DrawRowColorEdit4(
		bool undoable,
		string title,
		object o,
		string fieldName,
		ImGuiColorEditFlags flags,
		bool affectsFile,
		string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);
		DrawColorEdit4(undoable, title, o, fieldName, ImGui.GetContentRegionAvail().X, flags, affectsFile, help);
	}

	private static void DrawColorEdit4(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		ImGuiColorEditFlags flags,
		bool affectsFile,
		string help = null)
	{
		(bool, Vector4) Func(Vector4 v)
		{
			var r = ImGui.ColorEdit4(GetElementTitle(title, fieldName), ref v, flags);
			return (r, v);
		}

		DrawLiveEditValue<Vector4>(undoable, title, o, fieldName, width, affectsFile, Func, Vector4Compare, help);
	}

	#endregion Color Edit 4

	#region Display Tempo

	/// <summary>
	/// Draws a row for a custom set of controls to edit an EditorChart's display tempo.
	/// </summary>
	/// <param name="undoable">Whether operations should be undoable or not.</param>
	/// <param name="chart">EditorChart object to control.</param>
	/// <param name="actualMinTempo">Actual min tempo of the Chart.</param>
	/// <param name="actualMaxTempo">Actual max tempo of the Chart.</param>
	public static void DrawRowDisplayTempo(
		bool undoable,
		EditorChart chart,
		double actualMinTempo,
		double actualMaxTempo)
	{
		DrawRowTitleAndAdvanceColumn("Display Tempo");

		var spacing = ImGui.GetStyle().ItemSpacing.X;

		var tempoControlWidth = ImGui.GetContentRegionAvail().X - DisplayTempoEnumWidth - spacing;
		var splitTempoWidth = Math.Max(1.0f,
			(ImGui.GetContentRegionAvail().X - DisplayTempoEnumWidth - RangeToWidth - spacing * 3.0f) * 0.5f);

		// Draw an enum for choosing the DisplayTempoMode.
		DrawEnum(undoable, "", chart, nameof(EditorChart.DisplayTempoMode), DisplayTempoEnumWidth, null, true,
			"How the tempo for this chart should be displayed." +
			"\nRandom:    The actual tempo will be hidden and replaced with a random display." +
			"\nSpecified: A specified tempo or tempo range will be displayed." +
			"\n           This is a good option when tempo gimmicks would result in a misleading actual tempo range." +
			"\nActual:    The actual tempo or tempo range will be displayed.",
			chart?.DisplayTempoMode ?? DisplayTempoMode.Actual);

		// The remainder of the row depends on the mode.
		switch (chart?.DisplayTempoMode ?? DisplayTempoMode.Actual)
		{
			// For a Random display, just draw a disabled InputText with "???".
			case DisplayTempoMode.Random:
			{
				PushDisabled();
				var text = "???";
				ImGui.SetNextItemWidth(Math.Max(1.0f, tempoControlWidth));
				ImGui.SameLine();
				ImGui.InputText("", ref text, 4);
				PopDisabled();
				break;
			}

			// For a Specified display, draw the specified range.
			case DisplayTempoMode.Specified:
			{
				// ReSharper disable PossibleNullReferenceException
				// DragDouble for the min.
				ImGui.SameLine();
				ImGui.SetNextItemWidth(splitTempoWidth);
				DrawDragDouble(undoable, "", chart, nameof(EditorChart.DisplayTempoSpecifiedTempoMin), splitTempoWidth, null,
					0.001f, "%.6f", true);

				// "to" text to split the min and max.
				ImGui.SameLine();
				Text("to", RangeToWidth);

				// Checkbox for whether or not to use a distinct max.
				ImGui.SameLine();
				if (DrawCheckbox(false, "", chart, nameof(EditorChart.DisplayTempoShouldAllowEditsOfMax), 10.0f, true))
				{
					if (undoable)
					{
						// Enqueue a custom action so that the ShouldAllowEditsOfMax and previous max tempo can be undone together.
						ActionQueue.Instance.Do(
							new ActionSetDisplayTempoAllowEditsOfMax(chart, chart.DisplayTempoShouldAllowEditsOfMax));
					}
				}

				// If not using a distinct max, disable the max DragDouble and ensure that the max is set to the min.
				if (!chart.DisplayTempoShouldAllowEditsOfMax)
				{
					PushDisabled();

					if (!chart.DisplayTempoSpecifiedTempoMin.DoubleEquals(chart.DisplayTempoSpecifiedTempoMax))
						chart.DisplayTempoSpecifiedTempoMax = chart.DisplayTempoSpecifiedTempoMin;
				}

				// DragDouble for the max.
				ImGui.SameLine();
				ImGui.SetNextItemWidth(splitTempoWidth);
				DrawDragDouble(undoable, "", chart, nameof(EditorChart.DisplayTempoSpecifiedTempoMax),
					ImGui.GetContentRegionAvail().X, null,
					0.001f, "%.6f", true);

				// Pop the disabled setting if we pushed it before.
				if (!chart.DisplayTempoShouldAllowEditsOfMax)
				{
					PopDisabled();
				}
				// ReSharper restore PossibleNullReferenceException

				break;
			}

			case DisplayTempoMode.Actual:
			{
				// The controls for the actual tempo are always disabled.
				PushDisabled();

				// If the actual tempo is one value then just draw one DragDouble.
				if (actualMinTempo.DoubleEquals(actualMaxTempo))
				{
					ImGui.SetNextItemWidth(Math.Max(1.0f, tempoControlWidth));
					ImGui.SameLine();
					DragDouble(ref actualMinTempo, "");
				}

				// If the actual tempo is a range then draw the min and max.
				else
				{
					// DragDouble for the min.
					ImGui.SetNextItemWidth(splitTempoWidth);
					ImGui.SameLine();
					DragDouble(ref actualMinTempo, "");

					// "to" text to split the min and max.
					ImGui.SameLine();
					ImGui.Text("to");

					// DragDouble for the max.
					ImGui.SetNextItemWidth(splitTempoWidth);
					ImGui.SameLine();
					DragDouble(ref actualMaxTempo, "");
				}

				PopDisabled();
				break;
			}
		}
	}

	#endregion Display Tempo

	#region Step Tightening

	public static void DrawRowPerformedChartConfigDistanceTightening(
		StepTighteningConfig config,
		string title,
		string help)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var width = DrawHelp(help, ImGui.GetContentRegionAvail().X);
		var spacing = ImGui.GetStyle().ItemSpacing.X;
		var rangeControlWidth = width - (CheckBoxWidth + spacing);

		// Enabled checkbox.
		DrawCheckbox(true, title + "check", config, nameof(Config.StepTightening.DistanceTighteningEnabled),
			CheckBoxWidth, false);
		var enabled = config.IsDistanceTighteningEnabled();
		if (!enabled)
			PushDisabled();

		DrawDragDoubleRange(true, title, config, nameof(StepTighteningConfig.DistanceMin),
			nameof(StepTighteningConfig.DistanceMax),
			false, rangeControlWidth, 0.01f, "%.6f", 0.0, 10.0);

		if (!enabled)
			PopDisabled();
	}

	public static void DrawRowPerformedChartConfigStretchTightening(
		StepTighteningConfig config,
		string title,
		string help)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var width = DrawHelp(help, ImGui.GetContentRegionAvail().X);
		var spacing = ImGui.GetStyle().ItemSpacing.X;
		var rangeControlWidth = width - (CheckBoxWidth + spacing);

		// Enabled checkbox.
		DrawCheckbox(true, title + "check", config, nameof(Config.StepTightening.StretchTighteningEnabled),
			CheckBoxWidth, false);
		var enabled = config.IsStretchTighteningEnabled();
		if (!enabled)
			PushDisabled();

		DrawDragDoubleRange(true, title, config, nameof(StepTighteningConfig.StretchDistanceMin),
			nameof(StepTighteningConfig.StretchDistanceMax),
			false, rangeControlWidth, 0.01f, "%.6f", 0.0, 10.0);

		if (!enabled)
			PopDisabled();
	}

	public static void DrawRowPerformedChartConfigSpeedTightening(
		EditorPerformedChartConfig config,
		string title,
		string help)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var width = DrawHelp(help, ImGui.GetContentRegionAvail().X);
		var spacing = ImGui.GetStyle().ItemSpacing.X;
		var rangeWidth = width - (CheckBoxWidth + NoteTypeComboWidth + NotesFromTextWidth + BpmTextWidth + spacing * 5);

		// Enabled checkbox.
		DrawCheckbox(true, title + "check", config.Config.StepTightening, nameof(Config.StepTightening.SpeedTighteningEnabled),
			CheckBoxWidth, false);
		var enabled = config.Config.StepTightening.IsSpeedTighteningEnabled();
		if (!enabled)
			PushDisabled();

		// Note Type.
		var index = config.TravelSpeedNoteTypeDenominatorIndex;
		ImGui.SameLine();
		ImGui.SetNextItemWidth(NoteTypeComboWidth);
		var newIndex = index;
		var ret = ComboFromArray("", ref newIndex, ValidNoteTypeStrings);
		if (ret)
		{
			if (newIndex != index)
			{
				ActionQueue.Instance.Do(
					new ActionSetObjectFieldOrPropertyValue<int>(config,
						nameof(EditorPerformedChartConfig.TravelSpeedNoteTypeDenominatorIndex), newIndex,
						false));
			}
		}

		// From text.
		ImGui.SameLine();
		Text("notes from", NotesFromTextWidth);

		// BPM Range.
		ImGui.SameLine();
		DrawDragIntRange(
			true,
			"",
			config,
			nameof(EditorPerformedChartConfig.TravelSpeedMinBPM),
			nameof(EditorPerformedChartConfig.TravelSpeedMaxBPM),
			false,
			rangeWidth,
			null,
			1F,
			"%i",
			1, 1000);

		// BPM text.
		ImGui.SameLine();
		Text("BPM", BpmTextWidth);

		if (!enabled)
			PopDisabled();
	}

	#endregion Step Tightening

	#region Foot Select

	public static void DrawRowPatternConfigStartFootChoice(
		bool undoable,
		string title,
		EditorPatternConfig editorConfig,
		string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var config = editorConfig.Config;
		var choice = config.StartingFootChoice;

		// For simple enum-only choices just draw the enum and help.
		if (choice != PatternConfigStartingFootChoice.Specified)
		{
			DrawEnum<PatternConfigStartingFootChoice>(undoable, title, config, nameof(PatternConfig.StartingFootChoice),
				ImGui.GetContentRegionAvail().X, null, false, help);
			return;
		}

		// For the Specified choice, draw the help, then a shorter enum, then the foot choice enum.
		var footComboWidth = ImGui.GetContentRegionAvail().X - PatternConfigShortEnumWidth - ImGui.GetStyle().ItemSpacing.X;
		DrawEnum<PatternConfigStartingFootChoice>(undoable, title, config, nameof(PatternConfig.StartingFootChoice),
			PatternConfigShortEnumWidth, null, false, help);
		ImGui.SameLine();
		DrawEnum<Foot>(undoable, title, editorConfig, nameof(EditorPatternConfig.StartingFootSpecified),
			footComboWidth, null, false);
	}

	#endregion Foot Select

	#region Lane Select

	public static void DrawRowPatternConfigStartFootLaneChoice(
		Editor editor,
		bool undoable,
		string title,
		EditorPatternConfig editorConfig,
		ChartType? chartType,
		bool left,
		string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var config = editorConfig.Config;
		var choice = left ? config.LeftFootStartChoice : config.RightFootStartChoice;
		var choiceFieldName = left ? nameof(PatternConfig.LeftFootStartChoice) : nameof(PatternConfig.RightFootStartChoice);
		var valueFieldName =
			left ? nameof(PatternConfig.LeftFootStartLaneSpecified) : nameof(PatternConfig.RightFootStartLaneSpecified);

		// For simple enum-only choices just draw the enum and help.
		if (choice != PatternConfigStartFootChoice.SpecifiedLane)
		{
			DrawEnum<PatternConfigStartFootChoice>(undoable, title, config, choiceFieldName,
				ImGui.GetContentRegionAvail().X, null, false, help);
			return;
		}

		// For the SpecifiedLane choice, draw the help, then a shorter enum, then the single lane choice control.
		var laneChoiceWidth = ImGui.GetContentRegionAvail().X - PatternConfigShortEnumWidth - ImGui.GetStyle().ItemSpacing.X;
		DrawEnum<PatternConfigStartFootChoice>(undoable, title, config, choiceFieldName,
			PatternConfigShortEnumWidth, null, false, help);
		ImGui.SameLine();

		DrawSingleLaneChoice(GetElementTitle(title, valueFieldName), editor, undoable, config, valueFieldName, false,
			laneChoiceWidth, chartType);
	}

	public static void DrawRowPatternConfigEndFootLaneChoice(
		Editor editor,
		bool undoable,
		string title,
		EditorPatternConfig editorConfig,
		ChartType? chartType,
		bool left,
		string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var config = editorConfig.Config;
		var choice = left ? config.LeftFootEndChoice : config.RightFootEndChoice;
		var choiceFieldName = left ? nameof(PatternConfig.LeftFootEndChoice) : nameof(PatternConfig.RightFootEndChoice);
		var valueFieldName =
			left ? nameof(PatternConfig.LeftFootEndLaneSpecified) : nameof(PatternConfig.RightFootEndLaneSpecified);

		// For simple enum-only choices just draw the enum and help.
		if (choice != PatternConfigEndFootChoice.SpecifiedLane)
		{
			DrawEnum<PatternConfigEndFootChoice>(undoable, title, config, choiceFieldName,
				ImGui.GetContentRegionAvail().X, null, false, help);
			return;
		}

		// For the SpecifiedLane choice, draw the help, then a shorter enum, then the single lane choice control.
		var laneChoiceWidth = ImGui.GetContentRegionAvail().X - PatternConfigShortEnumWidth - ImGui.GetStyle().ItemSpacing.X;
		DrawEnum<PatternConfigEndFootChoice>(undoable, title, config, choiceFieldName,
			PatternConfigShortEnumWidth, null, false, help);
		ImGui.SameLine();

		DrawSingleLaneChoice(GetElementTitle(title, valueFieldName), editor, undoable, config, valueFieldName, false,
			laneChoiceWidth, chartType);
	}

	private static void DrawSingleLaneChoice(
		string id,
		Editor editor,
		bool undoable,
		object objectToUpdate,
		string fieldNameToUpdate,
		bool affectsFile,
		float width,
		ChartType? chartType)
	{
		var selectedLane = GetValueFromFieldOrProperty<int>(objectToUpdate, fieldNameToUpdate);
		var originalSelectedLane = selectedLane;

		var dragIntWidth = width;
		var defaultXSpacing = ImGui.GetStyle().ItemSpacing.X;

		if (chartType != null)
		{
			// Determine the width of the buttons and update the drag int width.
			var numLanes = GetChartProperties(chartType.Value).GetNumInputs();
			var buttonHeight = ImGui.GetFontSize() + ImGui.GetStyle().FramePadding.Y * 2;
			var padding = (int)((buttonHeight - ArrowIconHeight) * 0.5f);
			var buttonsWidth = numLanes * (ArrowIconWidth + padding * 2);
			dragIntWidth -= buttonsWidth + defaultXSpacing;

			// Set tighter spacing.
			var originalItemSpacingX = defaultXSpacing;
			var originalItemSpacingY = ImGui.GetStyle().ItemSpacing.Y;

			// Remove button color.
			// This looks better and works around an issue where when using ImageButton
			// we can't set an odd pixel height, which in practice is needed to match the normal
			// element height. This looks a pixel off if button colors are enabled.
			ImGui.PushStyleColor(ImGuiCol.Button, Vector4.Zero);

			// Icons.
			var textureAtlas = editor.GetTextureAtlas();
			var imGuiTextureAtlasTexture = editor.GetTextureAtlasImGuiTexture();
			var (atlasW, atlasH) = textureAtlas.GetDimensions();
			var icons = ArrowGraphicManager.GetIcons(chartType.Value);
			var dimIcons = ArrowGraphicManager.GetDimIcons(chartType.Value);
			for (var lane = 0; lane < numLanes; lane++)
			{
				ImGui.SameLine();

				var (x, y, w, h) = textureAtlas.GetSubTextureBounds(selectedLane == lane ? icons[lane] : dimIcons[lane]);

				ImGui.PushID($"##{id}SingleLaneChoice{lane}");
				if (ImGui.ImageButton(
					    imGuiTextureAtlasTexture,
					    ArrowIconSize,
					    new Vector2(x / (float)atlasW, y / (float)atlasH),
					    new Vector2((x + w) / (float)atlasW, (y + h) / (float)atlasH),
					    padding))
				{
					selectedLane = lane;
				}

				ImGui.PopID();

				// We need to set the spacing before calling SameLine.
				if (lane == 0)
					ImGui.GetStyle().ItemSpacing.X = 0;
				else if (lane == numLanes - 1)
					ImGui.GetStyle().ItemSpacing.X = originalItemSpacingX;

				ImGui.SameLine();
			}

			// Restore button color.
			ImGui.PopStyleColor(1);

			// Restore spacing.
			ImGui.GetStyle().ItemSpacing.X = originalItemSpacingX;
			ImGui.GetStyle().ItemSpacing.Y = originalItemSpacingY;
		}

		// Persist new selection if it changed.
		if (selectedLane != originalSelectedLane)
		{
			if (undoable)
			{
				ActionQueue.Instance.Do(
					new ActionSetObjectFieldOrPropertyValue<int>(objectToUpdate, fieldNameToUpdate, selectedLane, affectsFile));
			}
			else
			{
				SetFieldOrPropertyToValue(objectToUpdate, fieldNameToUpdate, selectedLane);
			}
		}

		// Lane value.
		var maxLane = GetMaxNumLanesForAnySupportedChartType();
		DrawDragInt(true, $"##{id}SingleLaneChoiceDragInt", objectToUpdate, fieldNameToUpdate, dragIntWidth, false, "", 0.1f,
			"%i", 0, maxLane - 1);
	}

	#endregion Lane Select

	#region Subdivisions

	public static void DrawRowSubdivisions(
		bool undoable,
		string title,
		object o,
		string fieldName,
		bool affectsFile,
		string help = null)
	{
		DrawRowTitleAndAdvanceColumn(title);

		var itemWidth = DrawHelp(help, ImGui.GetContentRegionAvail().X);
		ImGui.SetNextItemWidth(itemWidth);

		var elementTitle = GetElementTitle(title, fieldName);
		var currentValue = GetValueFromFieldOrProperty<SubdivisionType>(o, fieldName);
		var originalValue = currentValue;
		var currentBeatSubdivision = GetBeatSubdivision(currentValue);
		var currentMeasureSubdivision = GetMeasureSubdivision(currentValue);

		ImGui.PushStyleColor(ImGuiCol.Text, ArrowGraphicManager.GetArrowColorForSubdivision(currentBeatSubdivision));

		if (ImGui.BeginCombo($"{elementTitle}Combo", $"1/{currentMeasureSubdivision} Notes"))
		{
			ImGui.PopStyleColor();

			foreach (var subdivisionType in Enum.GetValues(typeof(SubdivisionType)))
			{
				var beatSubdivision = GetBeatSubdivision((SubdivisionType)subdivisionType);
				var measureSubdivision = GetMeasureSubdivision((SubdivisionType)subdivisionType);
				ImGui.PushStyleColor(ImGuiCol.Text, ArrowGraphicManager.GetArrowColorForSubdivision(beatSubdivision));

				var isSelected = currentMeasureSubdivision == measureSubdivision;
				if (ImGui.Selectable($"1/{measureSubdivision} Notes", isSelected))
				{
					currentValue = (SubdivisionType)subdivisionType;
				}

				if (isSelected)
				{
					ImGui.SetItemDefaultFocus();
				}

				ImGui.PopStyleColor();
			}

			ImGui.EndCombo();
		}
		else
		{
			ImGui.PopStyleColor();
		}

		if (currentValue != originalValue)
		{
			if (undoable)
				ActionQueue.Instance.Do(
					new ActionSetObjectFieldOrPropertyValue<SubdivisionType>(o, fieldName, currentValue, affectsFile));
			else
				SetFieldOrPropertyToValue(o, fieldName, currentValue);
		}
	}

	#endregion Subdivisions

	#region Misc Editor Events

	public static double GetMiscEditorEventHeight()
	{
		ImGui.PushFont(ImGuiFont);
		var h = ImGui.GetStyle().FramePadding.Y * 2 + ImGui.GetFontSize() + 2;
		ImGui.PopFont();
		return h;
	}

	public static double GetMiscEditorEventDragIntWidgetWidth(int i, string format)
	{
		return GetMiscEditorEventStringWidth(FormatImGuiInt(format, i));
	}

	public static void MiscEditorEventDragIntWidget(
		string id,
		EditorEvent e,
		string fieldName,
		int x,
		int y,
		int width,
		uint colorRGBA,
		bool selected,
		bool canBeDeleted,
		float speed,
		string format,
		float alpha,
		string help,
		int min = int.MinValue,
		int max = int.MaxValue)
	{
		void Func(float elementWidth)
		{
			DrawDragInt(true, $"##{id}", e, fieldName, elementWidth, true, "", speed, format, min, max);
		}

		MiscEditorEventWidget(id, e, x, y, width, colorRGBA, selected, canBeDeleted, alpha, help, Func);
	}

	public static double GetMiscEditorEventDragDoubleWidgetWidth(double d, string format)
	{
		return GetMiscEditorEventStringWidth(FormatImGuiDouble(format, d));
	}

	public static void MiscEditorEventDragDoubleWidget(
		string id,
		EditorEvent e,
		string fieldName,
		int x,
		int y,
		int width,
		uint colorRGBA,
		bool selected,
		bool canBeDeleted,
		float speed,
		string format,
		float alpha,
		string help,
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		void Func(float elementWidth)
		{
			DrawDragDouble(true, $"##{id}", e, fieldName, elementWidth, "", speed, format, true, min, max);
		}

		MiscEditorEventWidget(id, e, x, y, width, colorRGBA, selected, canBeDeleted, alpha, help, Func);
	}

	public static void MiscEditorEventPreviewDragDoubleWidget(
		string id,
		EditorEvent e,
		string fieldName,
		int x,
		int y,
		int width,
		uint colorRGBA,
		bool selected,
		float speed,
		string format,
		float alpha,
		string help,
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		void Func(float elementWidth)
		{
			DrawDragDouble(true, $"##{id}", e, fieldName, elementWidth, "", speed, format, true, min, max);
		}

		MiscEditorEventWidget(id, e, x, y, width, colorRGBA, selected, false, alpha, help, Func);
	}

	public static double GetMiscEditorEventStringWidth(string s)
	{
		ImGui.PushFont(ImGuiFont);
		var width = ImGui.CalcTextSize(s).X + GetCloseWidth();
		ImGui.PopFont();
		return width;
	}

	public static void MiscEditorEventTimeSignatureWidget(
		string id,
		EditorEvent e,
		string fieldName,
		int x,
		int y,
		int width,
		uint colorRGBA,
		bool selected,
		bool canBeDeleted,
		float alpha,
		string help)
	{
		void Func(float elementWidth)
		{
			DrawTimeSignatureInput(true, $"##{id}", e, fieldName, elementWidth, true);
		}

		MiscEditorEventWidget(id, e, x, y, width, colorRGBA, selected, canBeDeleted, alpha, help, Func);
	}

	public static void MiscEditorEventMultipliersWidget(
		string id,
		EditorEvent e,
		string fieldName,
		int x,
		int y,
		int width,
		uint colorRGBA,
		bool selected,
		bool canBeDeleted,
		float alpha,
		string help)
	{
		void Func(float elementWidth)
		{
			DrawMultipliersInput(true, $"##{id}", e, fieldName, elementWidth, true);
		}

		MiscEditorEventWidget(id, e, x, y, width, colorRGBA, selected, canBeDeleted, alpha, help, Func);
	}

	public static void MiscEditorEventLabelWidget(
		string id,
		EditorEvent e,
		string fieldName,
		int x,
		int y,
		int width,
		uint colorRGBA,
		bool selected,
		bool canBeDeleted,
		float alpha,
		string help)
	{
		void Func(float elementWidth)
		{
			DrawLabelInput(true, $"##{id}", e, fieldName, elementWidth, true);
		}

		MiscEditorEventWidget(id, e, x, y, width, colorRGBA, selected, canBeDeleted, alpha, help, Func);
	}

	public static void MiscEditorEventLastSecondHintWidget(
		string id,
		EditorEvent e,
		string fieldName,
		int x,
		int y,
		int width,
		uint colorRGBA,
		bool selected,
		bool canBeDeleted,
		float speed,
		string format,
		float alpha,
		string help,
		double min = double.MinValue,
		double max = double.MaxValue)
	{
		void Func(float elementWidth)
		{
			DrawDragDoubleCached(true, $"##{id}", e, fieldName, elementWidth, "", speed, format, true, min, max);
		}

		MiscEditorEventWidget(id, e, x, y, width, colorRGBA, selected, canBeDeleted, alpha, help, Func);
	}

	public static void MiscEditorEventScrollRateInterpolationInputWidget(
		string id,
		EditorEvent e,
		string fieldName,
		int x,
		int y,
		int width,
		uint colorRGBA,
		bool selected,
		bool canBeDeleted,
		float alpha,
		string help)
	{
		void Func(float elementWidth)
		{
			DrawScrollRateInterpolationInput(true, $"##{id}", e, fieldName, elementWidth, true);
		}

		MiscEditorEventWidget(id, e, x, y, width, colorRGBA, selected, canBeDeleted, alpha, help, Func);
	}

	private static void MiscEditorEventWidget(
		string id,
		EditorEvent e,
		int x,
		int y,
		int width,
		uint colorRGBA,
		bool selected,
		bool canBeDeleted,
		float alpha,
		string help,
		Action<float> func)
	{
		var colorPushCount = 0;
		var drawHelp = false;

		// Selected coloring.
		if (selected)
		{
			ImGui.PushStyleColor(ImGuiCol.WindowBg, 0x484848FF);
			ImGui.PushStyleColor(ImGuiCol.Border, 0xFFFFFFFF);
			colorPushCount += 2;
		}

		// Color the frame background to help differentiate controls.
		ImGui.PushStyleColor(ImGuiCol.FrameBg, colorRGBA);
		colorPushCount += 1;

		// If fading out, multiply key window elements by the alpha value.
		if (alpha < 1.0f)
		{
			PushAlpha(ImGuiCol.WindowBg, alpha);
			PushAlpha(ImGuiCol.Button, alpha);
			PushAlpha(ImGuiCol.FrameBg, alpha);
			PushAlpha(ImGuiCol.Text, alpha);
			PushAlpha(ImGuiCol.Border, alpha);
			colorPushCount += 5;
		}

		var height = (int)GetMiscEditorEventHeight();

		// Record window size and padding values so we can edit and restore them.
		var originalWindowPaddingX = ImGui.GetStyle().WindowPadding.X;
		var originalWindowPaddingY = ImGui.GetStyle().WindowPadding.Y;
		var originalMinWindowSize = ImGui.GetStyle().WindowMinSize;
		var originalItemSpacingX = ImGui.GetStyle().ItemSpacing.X;
		var originalInnerItemSpacingX = ImGui.GetStyle().ItemInnerSpacing.X;
		var originalFramePaddingX = ImGui.GetStyle().FramePadding.X;

		// Set the padding and spacing so we can draw a table with precise dimensions.
		ImGui.GetStyle().WindowPadding.X = 1;
		ImGui.GetStyle().WindowPadding.Y = 1;
		ImGui.GetStyle().WindowMinSize = new Vector2(width, height);
		ImGui.GetStyle().ItemSpacing.X = 0;
		ImGui.GetStyle().ItemInnerSpacing.X = 0;
		ImGui.GetStyle().FramePadding.X = 0;

		// Start the window.
		ImGui.SetNextWindowPos(new Vector2(x, y));
		ImGui.SetNextWindowSize(new Vector2(width, height));
		if (ImGui.Begin($"##Widget{id}",
			    ImGuiWindowFlags.NoMove
			    | ImGuiWindowFlags.NoDecoration
			    | ImGuiWindowFlags.NoSavedSettings
			    | ImGuiWindowFlags.NoDocking
			    | ImGuiWindowFlags.NoBringToFrontOnFocus
			    | ImGuiWindowFlags.NoFocusOnAppearing))
		{
			var elementWidth = width - GetCloseWidth();

			// Draw the control.
			func(elementWidth);

			// Record whether or not we should draw help text.
			if (!string.IsNullOrEmpty(help) && ImGui.IsItemHovered())
				drawHelp = true;

			// Delete button
			ImGui.SameLine();
			if (!canBeDeleted)
				PushDisabled();
			if (ImGui.Button($"X##{id}", new Vector2(GetCloseWidth(), 0.0f)))
				ActionQueue.Instance.Do(new ActionDeleteEditorEvents(e));
			if (!canBeDeleted)
				PopDisabled();
		}

		ImGui.End();

		// Restore window size and padding values.
		ImGui.GetStyle().FramePadding.X = originalFramePaddingX;
		ImGui.GetStyle().ItemInnerSpacing.X = originalInnerItemSpacingX;
		ImGui.GetStyle().ItemSpacing.X = originalItemSpacingX;
		ImGui.GetStyle().WindowPadding.X = originalWindowPaddingX;
		ImGui.GetStyle().WindowPadding.Y = originalWindowPaddingY;
		ImGui.GetStyle().WindowMinSize = originalMinWindowSize;

		ImGui.PopStyleColor(colorPushCount);

		// Draw help after restoring style elements.
		if (drawHelp)
		{
			ImGui.BeginTooltip();
			ImGui.PushTextWrapPos(ImGui.GetFontSize() * 80.0f);
			ImGui.TextUnformatted(help);
			ImGui.PopTextWrapPos();
			ImGui.EndTooltip();
		}
	}

	#endregion Misc Editor Events

	#region Compare Functions

	private static bool IntCompare(int a, int b)
	{
		return a == b;
	}

	private static bool UIntCompare(uint a, uint b)
	{
		return a == b;
	}

	private static bool FloatCompare(float a, float b)
	{
		return a.FloatEquals(b);
	}

	private static bool DoubleCompare(double a, double b)
	{
		return a.DoubleEquals(b);
	}

	private static bool StringCompare(string a, string b)
	{
		return a == b;
	}

	private static bool Vector3Compare(Vector3 a, Vector3 b)
	{
		return a == b;
	}

	private static bool Vector4Compare(Vector4 a, Vector4 b)
	{
		return a == b;
	}

	#endregion

	private static string GetElementTitle(string title, string fieldOrPropertyName)
	{
		return $"##{title}{fieldOrPropertyName}{CacheKeyPrefix}";
	}

	private static string GetElementTitle(string title)
	{
		return $"##{title}{CacheKeyPrefix}";
	}

	private static string GetCacheKey(string title, string fieldOrPropertyName)
	{
		return $"{CacheKeyPrefix}{title}{fieldOrPropertyName}";
	}

	private static T GetCachedValue<T>(string key)
	{
		return (T)Cache[key];
	}

	private static bool TryGetCachedValue<T>(string key, out T value)
	{
		value = default;
		var result = Cache.TryGetValue(key, out var outValue);
		if (result)
			value = (T)outValue;
		return result;
	}

	private static void SetCachedValue<T>(string key, T value)
	{
		Cache[key] = value;
	}

	public static float DrawHelp(string help, float width)
	{
		var hasHelp = !string.IsNullOrEmpty(help);
		var remainderWidth = hasHelp ? Math.Max(1.0f, width - GetHelpWidth() - ImGui.GetStyle().ItemSpacing.X) : width;
		//var remainderWidth = Math.Max(1.0f, width - GetHelpWidth() - ImGui.GetStyle().ItemSpacing.X);

		if (hasHelp)
		{
			HelpMarker(help);
			ImGui.SameLine();
		}
		//else
		//{
		//	ImGui.Dummy(new Vector2(HelpWidth, 1));
		//	ImGui.SameLine();
		//}

		return remainderWidth;
	}

	// ReSharper disable once UnusedMethodReturnValue.Local
	private static bool DrawCachedEditReference<T>(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		bool affectsFile,
		Func<T, (bool, T)> imGuiFunc,
		Func<T, T, bool> compareFunc,
		Func<T, bool> validationFunc = null,
		string help = null) where T : class, ICloneable
	{
		// Get the cached value.
		var cacheKey = GetCacheKey(title, fieldName);
		if (!TryGetCachedValue(cacheKey, out T cachedValue))
		{
			cachedValue = default;
			// ReSharper disable once ExpressionIsAlwaysNull
			SetCachedValue(cacheKey, cachedValue);
		}

		// Get the current value.
		var value = GetValueFromFieldOrProperty<T>(o, fieldName);

		// Draw the help marker and determine the remaining width.
		var textWidth = DrawHelp(help, width);
		ImGui.SetNextItemWidth(textWidth);

		// Draw the ImGui control using the cached value.
		// We do not want to see the effect of changing the value outside of the control
		// until after editing is complete.
		var (result, resultValue) = imGuiFunc(cachedValue);
		cachedValue = resultValue;
		SetCachedValue(cacheKey, cachedValue);

		// At the moment of releasing the control, enqueue an event to update the value to the
		// newly edited cached value.
		if (ImGui.IsItemDeactivatedAfterEdit()
		    && o != null
		    && (validationFunc == null || validationFunc(cachedValue))
		    && !compareFunc(cachedValue, value))
		{
			if (undoable)
				ActionQueue.Instance.Do(
					new ActionSetObjectFieldOrPropertyReference<T>(o, fieldName, (T)cachedValue.Clone(), affectsFile));
			else
				SetFieldOrPropertyToValue(o, fieldName, cachedValue);
			value = cachedValue;
		}

		// Always update the cached value if the control is not active.
		if (!ImGui.IsItemActive())
			SetCachedValue(cacheKey, value);

		return result;
	}

	private static bool DrawCachedEditValue<T>(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		bool affectsFile,
		Func<T, (bool, T)> imGuiFunc,
		Func<T, T, bool> compareFunc,
		Func<T, bool> validationFunc = null,
		string help = null) where T : struct
	{
		// Get the cached value.
		var cacheKey = GetCacheKey(title, fieldName);
		if (!TryGetCachedValue(cacheKey, out T cachedValue))
		{
			cachedValue = default;
			SetCachedValue(cacheKey, cachedValue);
		}

		// Get the current value.
		var value = GetValueFromFieldOrProperty<T>(o, fieldName);

		// Draw the help marker and determine the remaining width.
		var textWidth = DrawHelp(help, width);
		ImGui.SetNextItemWidth(textWidth);

		// Draw the ImGui control using the cached value.
		// We do not want to see the effect of changing the value outside of the control
		// until after editing is complete.
		var (result, resultValue) = imGuiFunc(cachedValue);
		cachedValue = resultValue;
		SetCachedValue(cacheKey, cachedValue);

		// At the moment of releasing the control, enqueue an event to update the value to the
		// newly edited cached value.
		if (ImGui.IsItemDeactivatedAfterEdit()
		    && o != null
		    && (validationFunc == null || validationFunc(cachedValue))
		    && !compareFunc(cachedValue, value))
		{
			if (undoable)
				ActionQueue.Instance.Do(new ActionSetObjectFieldOrPropertyValue<T>(o, fieldName, cachedValue, affectsFile));
			else
				SetFieldOrPropertyToValue(o, fieldName, cachedValue);
			value = cachedValue;
		}

		// Always update the cached value if the control is not active.
		if (!ImGui.IsItemActive())
			SetCachedValue(cacheKey, value);

		return result;
	}

	/// <summary>
	/// Draws an ImGui control where the value bound to the control is edited directly
	/// as the user interacts with the control.
	/// </summary>
	/// <typeparam name="T">Type of value edited.</typeparam>
	/// <param name="undoable">
	/// Whether or not the edits to the value should be enqueued as action for undo or not.
	/// </param>
	/// <param name="title">ImGui element title.</param>
	/// <param name="o">Object being edited.</param>
	/// <param name="fieldName">Name of field or property on object to edit.</param>
	/// <param name="width">ImGui element width.</param>
	/// <param name="affectsFile">Whether or not editing this value affects the saved file.</param>
	/// <param name="imGuiFunc">
	/// Function to wrap the ImGui control.
	/// Takes the value to be edited.
	/// Returns a tuple where the first entry is the return value of the ImGui control and
	/// the second entry is the new value after being edited. This returned because Funcs
	/// cannot take values by ref.
	/// </param>
	/// <param name="compareFunc">
	/// Function to compare to values of type T for equality.
	/// Returns true if equal and false otherwise.
	/// Used to avoid updating values or enqueueing actions when no changes occur.
	/// </param>
	/// <param name="help"></param>
	/// <returns></returns>
	private static bool DrawLiveEditValue<T>(
		bool undoable,
		string title,
		object o,
		string fieldName,
		float width,
		bool affectsFile,
		Func<T, (bool, T)> imGuiFunc,
		Func<T, T, bool> compareFunc,
		string help = null) where T : struct
	{
		var cacheKey = GetCacheKey(title, fieldName);

		// Get the current value.
		var value = GetValueFromFieldOrProperty<T>(o, fieldName);
		var beforeValue = value;

		// Draw the help marker and determine the remaining width.
		var itemWidth = DrawHelp(help, width);
		ImGui.SetNextItemWidth(itemWidth);

		// Draw a the ImGui control using the actual value, not the cached value.
		// We want to see the effect of changing this value immediately.
		// Do not however enqueue an EditorAction for this change yet.
		var (result, resultValue) = imGuiFunc(value);
		value = resultValue;
		if (result)
		{
			SetFieldOrPropertyToValue(o, fieldName, value);
		}

		// At the moment of activating the ImGui control, record the current value so we can undo to it later.
		if (ImGui.IsItemActivated())
		{
			SetCachedValue(cacheKey, beforeValue);
		}

		// At the moment of releasing the control, enqueue an event so we can undo to the previous value.
		if (undoable)
		{
			if (ImGui.IsItemDeactivatedAfterEdit() && o != null && !compareFunc(value, GetCachedValue<T>(cacheKey)))
			{
				ActionQueue.Instance.Do(
					new ActionSetObjectFieldOrPropertyValue<T>(o, fieldName, value, GetCachedValue<T>(cacheKey), affectsFile));
			}
		}

		return result;
	}
}
