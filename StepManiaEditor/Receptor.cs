﻿using System.Diagnostics;
using Fumen.Converters;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace StepManiaEditor
{
	/// <summary>
	/// Class for managing the arrow receptors and playing various receptor animations.
	/// </summary>
	public class Receptor
	{
		// Animation parameters for scaling the receptor when tapped.
		private const float TapAnimMidScale = 0.9f;
		private const float TapAnimEndScale = 1.0f;
		private const float TapAnimScaleDownTime = 0.02f;
		private const float TapAnimScaleUpTime = 0.08f;

		// Animation parameters for pulsing the receptor with the beat.
		private const float BeatAnimStartBrightness = 1.0f;
		private const float BeatAnimEndBrightness = 0.8f;
		private const float BeatAnimTimeEnd = 0.3f; // Percentage through beat.

		// Animation parameters for showing a rim around the receptor when held.
		private const float RimAnimEndScale = 1.3f;
		private const float RimAnimTimeEnd = 0.2f;

		// Animation parameters for showing a glow effect on the receptor when hitting a note.
		private const float GlowAnimTimeEnd = 0.3f;

		// Initialization parameters.
		private readonly int Lane;
		private readonly ArrowGraphicManager ArrowGraphicManager;
		private readonly EditorChart ActiveChart;

		// Hold tracking for rim.
		private bool Held = false;
		private bool AutoplayHeld = false;

		// Animated values.
		private float BeatBrightness = 1.0f;
		private float TapScale = 1.0f;
		private float RimScale = 1.0f;
		private float RimAlpha = 0.0f;
		private float GlowAlpha = 0.0f;

		// Animation timers.
		private double TapAnimStartOffset = 0.0;
		private Stopwatch TapAnimStopwatch = null;
		private double RimAnimStartOffset = 0.0;
		private Stopwatch RimAnimStopwatch = null;
		private double GlowAnimStartOffset = 0.0;
		private Stopwatch GlowAnimStopwatch = null;

		/// <summary>
		/// Constructor.
		/// </summary>
		/// <param name="lane">The lane this receptor is for.</param>
		/// <param name="arrowGraphicManager">The ArrowGraphicManager to use for getting arrow texture information.</param>
		/// <param name="activeChart">The active EditorChart.</param>
		public Receptor(int lane, ArrowGraphicManager arrowGraphicManager, EditorChart activeChart)
		{
			Lane = lane;
			ArrowGraphicManager = arrowGraphicManager;
			ActiveChart = activeChart;
		}

		/// <summary>
		/// Perform time based updates.
		/// </summary>
		/// <param name="playing">Whether or not the song is being played.</param>
		/// <param name="chartPosition">The current chart position.</param>
		public void Update(bool playing, double chartPosition)
		{
			UpdateTapAnimation();
			UpdateRimAnimation();
			UpdateGlowAnimation();
			UpdateBeatBrightness(playing, chartPosition);
		}

		/// <summary>
		/// Draws the receptor without any foreground effects.
		/// </summary>
		/// <param name="focalPoint">The focal point of the receptor group.</param>
		/// <param name="zoom">The current zoom level.</param>
		/// <param name="textureAtlas">TextureAtlas containing receptor images.</param>
		/// <param name="spriteBatch">SpriteBatch to use for rendering.</param>
		public void Draw(Vector2 focalPoint, double zoom, TextureAtlas textureAtlas, SpriteBatch spriteBatch)
		{
			// Determine positioning information needed for drawing.
			var numArrows = ActiveChart.NumInputs;
			var (textureId, rot) = ArrowGraphicManager.GetReceptorTexture(Lane);
			var (textureWidth, textureHeight) = textureAtlas.GetDimensions(textureId);
			if (zoom > 1.0)
				zoom = 1.0;
			var arrowWidth = textureWidth * zoom;
			var xStart = focalPoint.X - (numArrows * arrowWidth * 0.5f);

			// Draw receptor texture.
			textureAtlas.Draw(
				textureId,
				spriteBatch,
				new Vector2((float)(xStart + (Lane + 0.5f) * arrowWidth), focalPoint.Y),
				new Vector2(textureWidth * 0.5f, textureHeight * 0.5f),
				new Color(BeatBrightness, BeatBrightness, BeatBrightness, 1.0f),
				(float)(zoom * TapScale),
				rot,
				SpriteEffects.None);
		}

		/// <summary>
		/// Draws the foreground effects on the receptor.
		/// </summary>
		/// <param name="focalPoint">The focal point of the receptor group.</param>
		/// <param name="zoom">The current zoom level.</param>
		/// <param name="textureAtlas">TextureAtlas containing receptor images.</param>
		/// <param name="spriteBatch">SpriteBatch to use for rendering.</param>
		public void DrawForegroundEffects(Vector2 focalPoint, double zoom, TextureAtlas textureAtlas, SpriteBatch spriteBatch)
		{
			// Determine positioning information needed for drawing.
			var numArrows = ActiveChart.NumInputs;
			var (textureId, _) = ArrowGraphicManager.GetReceptorTexture(Lane);
			var (textureWidth, _) = textureAtlas.GetDimensions(textureId);
			if (zoom > 1.0)
				zoom = 1.0;
			var arrowWidth = textureWidth * zoom;
			var xStart = focalPoint.X - (numArrows * arrowWidth * 0.5f);

			// Draw rim texture.
			if (RimAlpha > 0.0f)
			{
				var (rimTextureId, rimRot) = ArrowGraphicManager.GetReceptorHeldTexture(Lane);
				var (rimTextureWidth, rimTextureHeight) = textureAtlas.GetDimensions(rimTextureId);

				textureAtlas.Draw(
					rimTextureId,
					spriteBatch,
					new Vector2((float)(xStart + (Lane + 0.5f) * arrowWidth), focalPoint.Y),
					new Vector2(rimTextureWidth * 0.5f, rimTextureHeight * 0.5f),
					new Color(1.0f, 1.0f, 1.0f, RimAlpha),
					(float)(zoom * RimScale),
					rimRot,
					SpriteEffects.None);
			}

			// Draw glow texture.
			if (GlowAlpha > 0.0f)
			{
				var (glowTextureId, glowRot) = ArrowGraphicManager.GetReceptorGlowTexture(Lane);
				var (glowTextureWidth, glowTextureHeight) = textureAtlas.GetDimensions(glowTextureId);

				textureAtlas.Draw(
					glowTextureId,
					spriteBatch,
					new Vector2((float)(xStart + (Lane + 0.5f) * arrowWidth), focalPoint.Y),
					new Vector2(glowTextureWidth * 0.5f, glowTextureHeight * 0.5f),
					new Color(1.0f, 1.0f, 1.0f, GlowAlpha),
					(float)(zoom),
					glowRot,
					SpriteEffects.None);
			}
		}

		/// <summary>
		/// Starts the tap animation to scale the receptor.
		/// </summary>
		/// <param name="timeDelta">How far into the animation we should start.</param>
		private void StartTapAnimation(double timeDelta = 0.0)
		{
			TapAnimStopwatch = new Stopwatch();
			TapAnimStopwatch.Start();
			TapAnimStartOffset = timeDelta;
		}

		/// <summary>
		/// Starts the rim animation.
		/// </summary>
		/// <param name="timeDelta">How far into the animation we should start.</param>
		private void StartRimAnimation(double timeDelta = 0.0)
		{
			RimAnimStopwatch = new Stopwatch();
			RimAnimStopwatch.Start();
			RimAnimStartOffset = timeDelta;
		}

		/// <summary>
		/// Starts the glow animation.
		/// </summary>
		/// <param name="timeDelta">How far into the animation we should start.</param>
		private void StartGlowAnimation(double timeDelta = 0.0)
		{
			GlowAlpha = 1.0f;
			GlowAnimStopwatch = new Stopwatch();
			GlowAnimStopwatch.Start();
			GlowAnimStartOffset = timeDelta;
		}

		private void UpdateTapAnimation()
		{
			// Set default value.
			TapScale = 1.0f;

			// Check for completion.
			if (TapAnimStopwatch == null)
				return;
			var t = (float)(TapAnimStopwatch.Elapsed.TotalSeconds + TapAnimStartOffset);
			if (t > TapAnimScaleDownTime + TapAnimScaleUpTime)
			{
				TapAnimStopwatch = null;
				return;
			}

			// Animate.
			if (t < TapAnimScaleDownTime)
			{
				TapScale = Fumen.Interpolation.Lerp(TapAnimEndScale, TapAnimMidScale, 0.0f, TapAnimScaleDownTime, t);
				return;
			}
			TapScale = Fumen.Interpolation.Lerp(TapAnimMidScale, TapAnimEndScale, 0.0f, TapAnimScaleUpTime, t - TapAnimScaleDownTime);
		}

		private void UpdateRimAnimation()
		{
			// Set default values.
			RimScale = 1.0f;
			RimAlpha = IsHeldForRimAnimation() ? 1.0f : 0.0f;

			// Check for completion.
			if (RimAnimStopwatch == null)
				return;
			var t = (float)(RimAnimStopwatch.Elapsed.TotalSeconds + RimAnimStartOffset);
			if (t > RimAnimTimeEnd)
			{
				RimAnimStopwatch = null;
				return;
			}

			// Animate.
			RimAlpha = Fumen.Interpolation.Lerp(1.0f, 0.0f, 0.0f, RimAnimTimeEnd, t);
			RimScale = Fumen.Interpolation.Lerp(1.0f, RimAnimEndScale, 0.0f, RimAnimTimeEnd, t);
		}

		private void UpdateGlowAnimation()
		{
			// Set default values.
			GlowAlpha = 0.0f;

			// Check for completion.
			if (GlowAnimStopwatch == null)
				return;
			var t = (float)(GlowAnimStopwatch.Elapsed.TotalSeconds + GlowAnimStartOffset);
			if (t > GlowAnimTimeEnd)
			{
				GlowAnimStopwatch = null;
				return;
			}

			// Animate.
			GlowAlpha = Fumen.Interpolation.Lerp(1.0f, 0.0f, 0.0f, GlowAnimTimeEnd, t);
		}

		private void UpdateBeatBrightness(bool playing, double chartPosition)
		{
			BeatBrightness = BeatAnimEndBrightness;
			if (playing && Preferences.Instance.PreferencesAnimations.PulseReceptorsWithTempo)
			{
				if (chartPosition < 0.0)
					chartPosition += ((int)((-chartPosition) / SMCommon.MaxValidDenominator) + 1) * SMCommon.MaxValidDenominator;
				var percentageBetweenBeats = (float)((chartPosition % SMCommon.MaxValidDenominator) / SMCommon.MaxValidDenominator);
				BeatBrightness = Fumen.Interpolation.Lerp(BeatAnimStartBrightness, BeatAnimEndBrightness, 0.0f, BeatAnimTimeEnd, percentageBetweenBeats);
			}
		}

		private bool IsHeldForRimAnimation()
		{
			return (Preferences.Instance.PreferencesAnimations.TapRimEffect && Held)
			       || (Preferences.Instance.PreferencesAnimations.AutoPlayRimEffect && AutoplayHeld);
		}

		/// <summary>
		/// Called when the user presses a key used for input on this receptor's lane.
		/// </summary>
		public void OnInputDown()
		{
			Held = true;

			if (Preferences.Instance.PreferencesAnimations.TapShrinkEffect)
			{
				StartTapAnimation();
			}

			if (Preferences.Instance.PreferencesAnimations.TapRimEffect)
			{
				if (IsHeldForRimAnimation())
					RimAlpha = 1.0f;
			}
		}

		/// <summary>
		/// Called when the user releases a key used for input on this receptor's lane.
		/// </summary>
		public void OnInputUp()
		{
			var wasHeld = IsHeldForRimAnimation();
			Held = false;

			if (Preferences.Instance.PreferencesAnimations.TapRimEffect)
			{
				if (wasHeld && !IsHeldForRimAnimation())
					StartRimAnimation();
			}
		}

		/// <summary>
		/// Called when an autoplay key press should occur on this receptor's lane.
		/// </summary>
		public void OnAutoplayInputDown(double timeDelta = 0.0)
		{
			AutoplayHeld = true;

			if (Preferences.Instance.PreferencesAnimations.AutoPlayRimEffect)
			{
				if (IsHeldForRimAnimation())
					RimAlpha = 1.0f;
			}

			if (Preferences.Instance.PreferencesAnimations.AutoPlayShrinkEffect)
				StartTapAnimation(timeDelta);

			// Autoplay inputs occur on note hits, so we should start the glow animation.
			if (Preferences.Instance.PreferencesAnimations.AutoPlayGlowEffect)
				StartGlowAnimation(timeDelta);
		}

		/// <summary>
		/// Called when an autoplay key release should occur on this receptor's lane.
		/// </summary>
		public void OnAutoplayInputUp(double timeDelta = 0.0)
		{
			var wasHeld = IsHeldForRimAnimation();
			AutoplayHeld = false;

			if (Preferences.Instance.PreferencesAnimations.AutoPlayRimEffect)
			{
				if (wasHeld && !IsHeldForRimAnimation())
					StartRimAnimation(timeDelta);
			}

			// The glow effect should also play on release.
			if (Preferences.Instance.PreferencesAnimations.AutoPlayGlowEffect)
			{
				StartGlowAnimation(timeDelta);
			}
		}

		/// <summary>
		/// Called when autoplay input was cancelled for this receptor's lane.
		/// </summary>
		public void OnAutoplayInputCancel()
		{
			var wasHeld = IsHeldForRimAnimation();
			AutoplayHeld = false;

			if (Preferences.Instance.PreferencesAnimations.AutoPlayRimEffect)
			{
				if (wasHeld && !IsHeldForRimAnimation())
					StartRimAnimation();
			}

			GlowAnimStopwatch = null;
		}

		public bool IsAutoplayHeld()
		{
			return AutoplayHeld;
		}
	}
}