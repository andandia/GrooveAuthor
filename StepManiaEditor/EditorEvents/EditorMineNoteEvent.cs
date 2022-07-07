﻿using Fumen.ChartDefinition;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using static StepManiaEditor.Utils;

namespace StepManiaEditor
{
	public class EditorMineNoteEvent : EditorEvent
	{
		private readonly LaneNote LaneNote;

		public EditorMineNoteEvent(EditorChart editorChart, LaneNote chartEvent) : base(editorChart, chartEvent)
		{
			LaneNote = chartEvent;
		}

		public override int GetLane()
		{
			return LaneNote.Lane;
		}

		public override void Draw(TextureAtlas textureAtlas, SpriteBatch spriteBatch)
		{
			textureAtlas.Draw(
				TextureIdMine,
				spriteBatch,
				new Vector2((float)GetX(), (float)GetY()),
				(float)GetScale(),
				0.0f,
				1.0f);
		}
	}
}
