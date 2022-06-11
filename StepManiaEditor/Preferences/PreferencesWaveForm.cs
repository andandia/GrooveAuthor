﻿using System.Numerics;
using System.Text.Json.Serialization;

namespace StepManiaEditor
{
	/// <summary>
	/// Preferences for the WaveForm.
	/// </summary>
	public class PreferencesWaveForm
	{
		// Default values.
		public const bool DefaultShowWaveForm = true;
		public const bool DefaultWaveFormScaleXWhenZooming = true;
		public const UIWaveFormPreferences.SparseColorOption DefaultWaveFormSparseColorOption = UIWaveFormPreferences.SparseColorOption.DarkerDenseColor;
		public const float DefaultWaveFormSparseColorScale = 0.8f;
		public static readonly Vector3 DefaultWaveFormDenseColor = new Vector3(0.0f, 0.389f, 0.183f);
		public static readonly Vector3 DefaultWaveFormSparseColor = new Vector3(0.0f, 0.350f, 0.164f);
		public const float DefaultWaveFormMaxXPercentagePerChannel = 0.9f;
		public const int DefaultWaveFormLoadingMaxParallelism = 8;

		// Preferences.
		[JsonInclude] public bool ShowWaveFormPreferencesWindow = false;
		[JsonInclude] public bool ShowWaveForm = DefaultShowWaveForm;
		[JsonInclude] public bool WaveFormScaleXWhenZooming = DefaultWaveFormScaleXWhenZooming;
		[JsonInclude] public UIWaveFormPreferences.SparseColorOption WaveFormSparseColorOption = DefaultWaveFormSparseColorOption;
		[JsonInclude] public float WaveFormSparseColorScale = DefaultWaveFormSparseColorScale;
		[JsonInclude] public Vector3 WaveFormDenseColor = DefaultWaveFormDenseColor;
		[JsonInclude] public Vector3 WaveFormSparseColor = DefaultWaveFormSparseColor;
		[JsonInclude] public float WaveFormMaxXPercentagePerChannel = DefaultWaveFormMaxXPercentagePerChannel;
		[JsonInclude] public int WaveFormLoadingMaxParallelism = DefaultWaveFormLoadingMaxParallelism;

		public bool IsUsingDefaults()
		{
			return ShowWaveForm == DefaultShowWaveForm
			       && WaveFormScaleXWhenZooming == DefaultWaveFormScaleXWhenZooming
			       && WaveFormSparseColorOption == DefaultWaveFormSparseColorOption
			       && WaveFormSparseColorScale.FloatEquals(DefaultWaveFormSparseColorScale)
			       && WaveFormDenseColor.Equals(DefaultWaveFormDenseColor)
			       && WaveFormSparseColor.Equals(DefaultWaveFormSparseColor)
			       && WaveFormMaxXPercentagePerChannel.FloatEquals(DefaultWaveFormMaxXPercentagePerChannel)
			       && WaveFormLoadingMaxParallelism == DefaultWaveFormLoadingMaxParallelism;
		}

		public void RestoreDefaults()
		{
			// Don't enqueue an action if it would not have any effect.
			if (IsUsingDefaults())
				return;
			ActionQueue.Instance.Do(new ActionRestoreWaveFormPreferenceDefaults());
		}
	}

	/// <summary>
	/// Action to restore WaveForm preferences to their default values.
	/// </summary>
	public class ActionRestoreWaveFormPreferenceDefaults : EditorAction
	{
		private readonly bool PreviousShowWaveForm;
		private readonly bool PreviousWaveFormScaleXWhenZooming;
		private readonly UIWaveFormPreferences.SparseColorOption PreviousWaveFormSparseColorOption;
		private readonly float PreviousWaveFormSparseColorScale;
		private readonly Vector3 PreviousWaveFormDenseColor;
		private readonly Vector3 PreviousWaveFormSparseColor;
		private readonly float PreviousWaveFormMaxXPercentagePerChannel;
		private readonly int PreviousWaveFormLoadingMaxParallelism;

		public ActionRestoreWaveFormPreferenceDefaults()
		{
			var p = Preferences.Instance.PreferencesWaveForm;
			PreviousShowWaveForm = p.ShowWaveForm;
			PreviousWaveFormScaleXWhenZooming = p.WaveFormScaleXWhenZooming;
			PreviousWaveFormSparseColorOption = p.WaveFormSparseColorOption;
			PreviousWaveFormSparseColorScale = p.WaveFormSparseColorScale;
			PreviousWaveFormDenseColor = p.WaveFormDenseColor;
			PreviousWaveFormSparseColor = p.WaveFormSparseColor;
			PreviousWaveFormMaxXPercentagePerChannel = p.WaveFormMaxXPercentagePerChannel;
			PreviousWaveFormLoadingMaxParallelism = p.WaveFormLoadingMaxParallelism;
		}

		public override string ToString()
		{
			return "Restore Waveform default preferences.";
		}

		public override void Do()
		{
			var p = Preferences.Instance.PreferencesWaveForm;
			p.ShowWaveForm = PreferencesWaveForm.DefaultShowWaveForm;
			p.WaveFormScaleXWhenZooming = PreferencesWaveForm.DefaultWaveFormScaleXWhenZooming;
			p.WaveFormSparseColorOption = PreferencesWaveForm.DefaultWaveFormSparseColorOption;
			p.WaveFormSparseColorScale = PreferencesWaveForm.DefaultWaveFormSparseColorScale;
			p.WaveFormDenseColor = PreferencesWaveForm.DefaultWaveFormDenseColor;
			p.WaveFormSparseColor = PreferencesWaveForm.DefaultWaveFormSparseColor;
			p.WaveFormMaxXPercentagePerChannel = PreferencesWaveForm.DefaultWaveFormMaxXPercentagePerChannel;
			p.WaveFormLoadingMaxParallelism = PreferencesWaveForm.DefaultWaveFormLoadingMaxParallelism;
		}

		public override void Undo()
		{
			var p = Preferences.Instance.PreferencesWaveForm;
			p.ShowWaveForm = PreviousShowWaveForm;
			p.WaveFormScaleXWhenZooming = PreviousWaveFormScaleXWhenZooming;
			p.WaveFormSparseColorOption = PreviousWaveFormSparseColorOption;
			p.WaveFormSparseColorScale = PreviousWaveFormSparseColorScale;
			p.WaveFormDenseColor = PreviousWaveFormDenseColor;
			p.WaveFormSparseColor = PreviousWaveFormSparseColor;
			p.WaveFormMaxXPercentagePerChannel = PreviousWaveFormMaxXPercentagePerChannel;
			p.WaveFormLoadingMaxParallelism = PreviousWaveFormLoadingMaxParallelism;
		}
	}
}