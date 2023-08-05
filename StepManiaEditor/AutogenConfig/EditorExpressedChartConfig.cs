﻿using System;
using System.Text.Json.Serialization;
using Fumen;
using StepManiaLibrary.ExpressedChart;
using ImGuiNET;

namespace StepManiaEditor.AutogenConfig;

/// <summary>
/// EditorExpressedChartConfig is a wrapper around an ExpressedChartConfig with additional
/// data and functionality for the editor.
/// </summary>
internal class EditorExpressedChartConfig : IEditorConfig, IEquatable<EditorExpressedChartConfig>
{
	public const string NewConfigName = "New Config";

	// Default values.
	public const BracketParsingMethod DefaultDefaultBracketParsingMethod = BracketParsingMethod.Balanced;

	public const BracketParsingDetermination DefaultBracketParsingDetermination =
		BracketParsingDetermination.ChooseMethodDynamically;

	public const int DefaultMinLevelForBrackets = 7;
	public const bool DefaultUseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets = true;
	public const double DefaultBalancedBracketsPerMinuteForAggressiveBrackets = 3.0;
	public const double DefaultBalancedBracketsPerMinuteForNoBrackets = 1.0;

	/// <summary>
	/// Guid for this NamedConfig. Not readonly so that it can be set from deserialization.
	/// </summary>
	[JsonInclude] public Guid Guid;

	// Preferences.
	[JsonInclude]
	public string Name
	{
		get => NameInternal;
		set
		{
			// Null check around IsNewNameValid because this property is set during deserialization.
			if (!(IsNewNameValid?.Invoke(value) ?? true))
				return;
			if (!string.IsNullOrEmpty(NameInternal) && NameInternal.Equals(value))
				return;
			NameInternal = value;
			// Null check around OnNameUpdated because this property is set during deserialization.
			OnNameUpdated?.Invoke();
		}
	}

	private string NameInternal;
	[JsonInclude] public string Description;

	/// <summary>
	/// The ExpressedChart Config wrapped by this EditorExpressedChartConfig.
	/// </summary>
	[JsonInclude] public Config Config = new();

	/// <summary>
	/// A cloned EditorExpressedChartConfig to use for comparisons to see
	/// if this EditorExpressedChartConfig has unsaved changes or not.
	/// </summary>
	private EditorExpressedChartConfig LastSavedState;

	/// <summary>
	/// Function to determine if a new name is valid.
	/// </summary>
	private Func<string, bool> IsNewNameValid;

	/// <summary>
	/// Callback function to invoke when the name is updated.
	/// </summary>
	private Action OnNameUpdated;

	/// <summary>
	/// Constructor.
	/// </summary>
	public EditorExpressedChartConfig()
	{
		Guid = Guid.NewGuid();
	}

	/// <summary>
	/// Constructor taking a previously generated Guid.
	/// </summary>
	/// <param name="guid">Guid for this EditorPerformedChartConfig.</param>
	public EditorExpressedChartConfig(Guid guid)
	{
		Guid = guid;
	}

	/// <summary>
	/// Returns a new EditorExpressedChartConfig that is a clone of this EditorExpressedChartConfig.
	/// The Guid of the returned EditorExpressedChartConfig will be unique and the name will be the default new name.
	/// </summary>
	/// <returns>Cloned EditorExpressedChartConfig.</returns>
	public EditorExpressedChartConfig CloneEditorExpressedChartConfig()
	{
		return CloneEditorExpressedChartConfig(false);
	}


	/// <summary>
	/// Returns a new EditorExpressedChartConfig that is a clone of this EditorExpressedChartConfig.
	/// </summary>
	/// <param name="snapshot">
	/// If true then everything on this EditorExpressedChartConfig will be cloned.
	/// If false then the Guid and Name will be changed.</param>
	/// <returns>Cloned EditorExpressedChartConfig.</returns>
	private EditorExpressedChartConfig CloneEditorExpressedChartConfig(bool snapshot)
	{
		return new EditorExpressedChartConfig(snapshot ? Guid : Guid.NewGuid())
		{
			Config = Config.Clone(),
			Name = snapshot ? Name : NewConfigName,
			Description = Description,
			IsNewNameValid = IsNewNameValid,
			OnNameUpdated = OnNameUpdated,
		};
	}

	/// <summary>
	/// Validates this EditorExpressedChartConfig and logs any errors on invalid data.
	/// </summary>
	/// <returns>True if no errors were found and false otherwise.</returns>
	public bool Validate()
	{
		var errors = false;
		if (Guid == Guid.Empty)
		{
			Logger.Error("EditorExpressedChartConfig has no Guid.");
			errors = true;
		}

		if (string.IsNullOrEmpty(Name))
		{
			Logger.Error($"EditorExpressedChartConfig {Guid} has no name.");
			errors = true;
		}

		errors = !Config.Validate(Name) || errors;
		return !errors;
	}

	#region IEditorConfig

	public Guid GetGuid()
	{
		return Guid;
	}

	public string GetName()
	{
		return Name;
	}

	public bool IsDefault()
	{
		return Guid.Equals(ConfigManager.DefaultExpressedChartDynamicConfigGuid)
		       || Guid.Equals(ConfigManager.DefaultExpressedChartAggressiveBracketsConfigGuid)
		       || Guid.Equals(ConfigManager.DefaultExpressedChartNoBracketsConfigGuid);
	}

	public IEditorConfig Clone()
	{
		return CloneEditorExpressedChartConfig();
	}

	public bool HasUnsavedChanges()
	{
		return LastSavedState == null || !Equals(LastSavedState);
	}

	public void UpdateLastSavedState()
	{
		LastSavedState = CloneEditorExpressedChartConfig(true);
	}

	public void InitializeWithDefaultValues()
	{
		Config.DefaultBracketParsingMethod = DefaultDefaultBracketParsingMethod;
		Config.BracketParsingDetermination = DefaultBracketParsingDetermination;
		Config.MinLevelForBrackets = DefaultMinLevelForBrackets;
		Config.UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets =
			DefaultUseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets;
		Config.BalancedBracketsPerMinuteForAggressiveBrackets = DefaultBalancedBracketsPerMinuteForAggressiveBrackets;
		Config.BalancedBracketsPerMinuteForNoBrackets = DefaultBalancedBracketsPerMinuteForNoBrackets;
	}

	#endregion IEditorConfig

	/// <summary>
	/// Sets function to use for calling back to when the name is updated.
	/// </summary>
	/// <param name="onNameUpdated">Callback function to invoke when the name is updated.</param>
	public void SetNameUpdatedFunction(Action onNameUpdated)
	{
		OnNameUpdated = onNameUpdated;
	}

	/// <summary>
	/// Returns whether or not this EditorExpressedChartConfig is using all default values.
	/// </summary>
	/// <returns>
	/// True if this EditorExpressedChartConfig is using all default values and false otherwise.
	/// </returns>
	public bool IsUsingDefaults()
	{
		return Config.DefaultBracketParsingMethod == DefaultDefaultBracketParsingMethod
		       && Config.BracketParsingDetermination == DefaultBracketParsingDetermination
		       && Config.MinLevelForBrackets == DefaultMinLevelForBrackets
		       && Config.UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets ==
		       DefaultUseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets
		       && Config.BalancedBracketsPerMinuteForAggressiveBrackets.DoubleEquals(
			       DefaultBalancedBracketsPerMinuteForAggressiveBrackets)
		       && Config.BalancedBracketsPerMinuteForNoBrackets.DoubleEquals(DefaultBalancedBracketsPerMinuteForNoBrackets);
	}

	/// <summary>
	/// Restores this EditorExpressedChartConfig to its default values.
	/// </summary>
	public void RestoreDefaults()
	{
		// Don't enqueue an action if it would not have any effect.
		if (IsUsingDefaults())
			return;
		ActionQueue.Instance.Do(new ActionRestoreExpressedChartConfigDefaults(this));
	}

	public static void CreateNewConfigAndShowEditUI(EditorChart editorChart = null)
	{
		var newConfigGuid = Guid.NewGuid();
		ActionQueue.Instance.Do(new ActionAddExpressedChartConfig(newConfigGuid, editorChart));
		ShowEditUI(newConfigGuid);
	}

	public static void ShowEditUI(Guid configGuid)
	{
		Preferences.Instance.ActiveExpressedChartConfigForWindow = configGuid;
		Preferences.Instance.ShowExpressedChartListWindow = true;
		ImGui.SetWindowFocus(UIExpressedChartConfig.WindowTitle);
	}

	#region IEquatable

	public bool Equals(EditorExpressedChartConfig other)
	{
		if (ReferenceEquals(null, other))
			return false;
		if (ReferenceEquals(this, other))
			return true;

		return Guid == other.Guid
		       && Name == other.Name
		       && Description == other.Description
		       && Config.Equals(other.Config);
	}

	public override bool Equals(object obj)
	{
		if (ReferenceEquals(null, obj))
			return false;
		if (ReferenceEquals(this, obj))
			return true;
		if (obj.GetType() != GetType())
			return false;
		return Equals((EditorExpressedChartConfig)obj);
	}

	public override int GetHashCode()
	{
		// ReSharper disable NonReadonlyMemberInGetHashCode
		return HashCode.Combine(
			Guid,
			Name,
			Description,
			Config);
		// ReSharper restore NonReadonlyMemberInGetHashCode
	}

	#endregion IEquatable
}

#region ActionRestoreExpressedChartConfigDefaults

/// <summary>
/// Action to restore an EditorExpressedChartConfig to its default values.
/// </summary>
internal sealed class ActionRestoreExpressedChartConfigDefaults : EditorAction
{
	private readonly EditorExpressedChartConfig Config;
	private readonly BracketParsingMethod PreviousDefaultBracketParsingMethod;
	private readonly BracketParsingDetermination PreviousBracketParsingDetermination;
	private readonly int PreviousMinLevelForBrackets;
	private readonly bool PreviousUseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets;
	private readonly double PreviousBalancedBracketsPerMinuteForAggressiveBrackets;
	private readonly double PreviousBalancedBracketsPerMinuteForNoBrackets;

	public ActionRestoreExpressedChartConfigDefaults(EditorExpressedChartConfig config) : base(false, false)
	{
		Config = config;
		PreviousDefaultBracketParsingMethod = Config.Config.DefaultBracketParsingMethod;
		PreviousBracketParsingDetermination = Config.Config.BracketParsingDetermination;
		PreviousMinLevelForBrackets = Config.Config.MinLevelForBrackets;
		PreviousUseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets =
			Config.Config.UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets;
		PreviousBalancedBracketsPerMinuteForAggressiveBrackets = Config.Config.BalancedBracketsPerMinuteForAggressiveBrackets;
		PreviousBalancedBracketsPerMinuteForNoBrackets = Config.Config.BalancedBracketsPerMinuteForNoBrackets;
	}

	public override bool AffectsFile()
	{
		return false;
	}

	public override string ToString()
	{
		return $"Restore {Config.Name} Expressed Chart Config to default values.";
	}

	protected override void DoImplementation()
	{
		Config.Config.DefaultBracketParsingMethod =
			EditorExpressedChartConfig.DefaultDefaultBracketParsingMethod;
		Config.Config.BracketParsingDetermination =
			EditorExpressedChartConfig.DefaultBracketParsingDetermination;
		Config.Config.MinLevelForBrackets = EditorExpressedChartConfig.DefaultMinLevelForBrackets;
		Config.Config.UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets =
			EditorExpressedChartConfig
				.DefaultUseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets;
		Config.Config.BalancedBracketsPerMinuteForAggressiveBrackets = EditorExpressedChartConfig
			.DefaultBalancedBracketsPerMinuteForAggressiveBrackets;
		Config.Config.BalancedBracketsPerMinuteForNoBrackets =
			EditorExpressedChartConfig.DefaultBalancedBracketsPerMinuteForNoBrackets;
	}

	protected override void UndoImplementation()
	{
		Config.Config.DefaultBracketParsingMethod = PreviousDefaultBracketParsingMethod;
		Config.Config.BracketParsingDetermination = PreviousBracketParsingDetermination;
		Config.Config.MinLevelForBrackets = PreviousMinLevelForBrackets;
		Config.Config.UseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets =
			PreviousUseAggressiveBracketsWhenMoreSimultaneousNotesThanCanBeCoveredWithoutBrackets;
		Config.Config.BalancedBracketsPerMinuteForAggressiveBrackets = PreviousBalancedBracketsPerMinuteForAggressiveBrackets;
		Config.Config.BalancedBracketsPerMinuteForNoBrackets = PreviousBalancedBracketsPerMinuteForNoBrackets;
	}
}

#endregion ActionRestoreExpressedChartConfigDefaults
