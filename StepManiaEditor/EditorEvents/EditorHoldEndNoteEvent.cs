﻿using System;
using Fumen.ChartDefinition;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static StepManiaEditor.Utils;

namespace StepManiaEditor
{
	public class EditorHoldEndNoteEvent : EditorEvent
	{
		private EditorHoldStartNoteEvent EditorHoldStartNoteEvent;
		private readonly LaneHoldEndNote LaneHoldEndNote;

		/// <summary>
		/// Whether or not this hold should be considered active for rendering.
		/// </summary>
		private bool NextDrawActive;
		private double NextDrawActiveYCutoffPoint;

		public EditorHoldEndNoteEvent(EditorChart editorChart, LaneHoldEndNote chartEvent) : base(editorChart, chartEvent)
		{
			LaneHoldEndNote = chartEvent;
		}

		public EditorHoldEndNoteEvent(EditorChart editorChart, LaneHoldEndNote chartEvent, bool isBeingEdited) : base(editorChart, chartEvent, isBeingEdited)
		{
			LaneHoldEndNote = chartEvent;
		}

		public void SetHoldStartNote(EditorHoldStartNoteEvent editorHoldStartNoteEvent)
		{
			EditorHoldStartNoteEvent = editorHoldStartNoteEvent;
		}

		public EditorHoldStartNoteEvent GetHoldStartNote()
		{
			return EditorHoldStartNoteEvent;
		}

		public override int GetLane()
		{
			return LaneHoldEndNote.Lane;
		}

		public bool IsRoll()
		{
			return EditorHoldStartNoteEvent.IsRoll();
		}

		public void SetIsRoll(bool roll)
		{
			EditorHoldStartNoteEvent.SetIsRoll(roll);
		}

		public void SetNextDrawActive(bool active, double y)
		{
			NextDrawActive = active;
			NextDrawActiveYCutoffPoint = y;
		}

		public override void Draw(TextureAtlas textureAtlas, SpriteBatch spriteBatch, ArrowGraphicManager arrowGraphicManager)
		{
			var roll = IsRoll();
			var alpha = IsBeingEdited() ? ActiveEditEventAlpha : 1.0f;

			var active = NextDrawActive && Preferences.Instance.PreferencesReceptors.AutoPlayLightHolds;
			var activeAndCutoff = NextDrawActive && Preferences.Instance.PreferencesReceptors.AutoPlayHideArrows;

			// The hold body texture is a tiled texture that starts at the end of the hold and ends at the arrow.
			var (holdBodyTextureId, holdBodyMirrored) = roll ?
				arrowGraphicManager.GetRollBodyTexture(LaneHoldEndNote.IntegerPosition, LaneHoldEndNote.Lane, active) :
				arrowGraphicManager.GetHoldBodyTexture(LaneHoldEndNote.IntegerPosition, LaneHoldEndNote.Lane, active);
			// The hold cap texture is a texture that is drawn once at the end of the hold.
			var (holdCapTextureId, holdCapRotation) = roll ?
				arrowGraphicManager.GetRollEndTexture(LaneHoldEndNote.IntegerPosition, LaneHoldEndNote.Lane, active) :
				arrowGraphicManager.GetHoldEndTexture(LaneHoldEndNote.IntegerPosition, LaneHoldEndNote.Lane, active);
			// The hold start texture is only used to extend the start of the hold upward into the arrow for certain
			// arrow graphics which wouldn't otherwise mask the hold start, like solo diagonals.
			var (holdBodyStartTexture, holdBodyStartMirror) = roll ?
				arrowGraphicManager.GetRollStartTexture(GetHoldStartNote().GetRow(), GetLane(), NextDrawActive) :
				arrowGraphicManager.GetHoldStartTexture(GetHoldStartNote().GetRow(), GetLane(), NextDrawActive);

			var (_, capH) = textureAtlas.GetDimensions(holdCapTextureId);
			var (bodyTexW, bodyTexH) = textureAtlas.GetDimensions(holdBodyTextureId);

			// Determine the Y value and height to use.
			// If the note is active, we should bring down the top to the cutoff point.
			var noteY = GetY();
			var noteH = GetH();
			if (activeAndCutoff)
			{
				noteH -= (NextDrawActiveYCutoffPoint - noteY);
				noteY = NextDrawActiveYCutoffPoint;
			}

			capH = (int)(capH * GetScale() + 0.5);
			var bodyTileH = (int)(bodyTexH * GetScale() + 0.5);
			var y = (int)(noteY + noteH + 0.5) - capH;
			var minY = (int)(noteY + 0.5);
			var x = (int)(GetX() + 0.5);
			var w = (int)(GetW() + 0.5);

			// Record the cap position for drawing later.
			var capY = y;
			if (arrowGraphicManager.AreHoldCapsCentered())
				capY -= (int)(capH * 0.5f);

			// Adjust the starting y value so we don't needlessly loop when zoomed in and a large
			// area of the hold is off the screen.
			if (y > ScreenHeight + capH)
			{
				y -= ((y - (int)ScreenHeight) / capH) * capH;
			}

			// Draw the body by looping up from the bottom, ensuring that each tiled body texture aligns
			// perfectly with the previous one.
			var spriteEffects = holdBodyMirrored ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
			while (y >= minY)
			{
				var h = Math.Min(bodyTileH, y - minY);
				if (h == 0)
					break;
				y -= h;
				if (y < -capH)
					break;
				if (h < bodyTileH)
				{
					var sourceH = (int)(bodyTexH * ((double)h / bodyTileH));
					textureAtlas.Draw(holdBodyTextureId, spriteBatch, new Rectangle(0, bodyTexH - sourceH, bodyTexW, sourceH), new Rectangle(x, y, w, h), 0.0f, alpha, spriteEffects);
				}
				else
				{
					textureAtlas.Draw(holdBodyTextureId, spriteBatch, new Rectangle(x, y, w, h), 0.0f, alpha, spriteEffects);
				}
			}

			// Some arrows, like solo diagonals need a hold start graphic to fill the gap at the top of the hold
			// between the arrow midpoint and the widest part of the arrow.
			if (holdBodyStartTexture != null)
			{
				// It is assumed there is no height padding baked into this texture.
				var (_, holdBodyStartHeight) = textureAtlas.GetDimensions(holdBodyStartTexture);
				var holdBodyStartH = (int)(holdBodyStartHeight * GetScale());

				textureAtlas.Draw(
					holdBodyStartTexture,
					spriteBatch,
					new Rectangle(x, minY - holdBodyStartH, w, holdBodyStartH),
					0.0f,
					alpha,
					holdBodyStartMirror ? SpriteEffects.FlipHorizontally : SpriteEffects.None);
			}

			// Draw the cap, if it is visible.
			// The cap should be drawn after the body as some caps render on top of the body.
			if (capY > -capH && capY < ScreenHeight)
				textureAtlas.Draw(holdCapTextureId, spriteBatch, new Rectangle(x, capY, w, capH), holdCapRotation, alpha, SpriteEffects.None);

			// If active, draw the hold start note on top of the receptors.
			// The actual hold start note will not draw since it is above the receptors.
			if (activeAndCutoff)
			{
				var (startArrowTexture, _) = arrowGraphicManager.GetArrowTexture(GetHoldStartNote().GetRow(), GetLane());
				var (_, startArrowHeight) = textureAtlas.GetDimensions(startArrowTexture);
				var holdStartY = noteY - (startArrowHeight * 0.5 * GetScale());
				GetHoldStartNote().DrawAtY(textureAtlas, spriteBatch, arrowGraphicManager, holdStartY);
			}

			// Reset active flags.
			NextDrawActive = false;
			NextDrawActiveYCutoffPoint = 0.0;
		}
	}
}
