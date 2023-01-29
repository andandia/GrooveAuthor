﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StepManiaEditor
{
	internal sealed class EditorLastSecondHintEvent : EditorEvent
	{
		public EditorLastSecondHintEvent(EditorChart editorChart, double chartPosition)
			: base(EventConfig.CreateConfigNoEvent(editorChart, chartPosition))
		{

		}

		public override bool IsMiscEvent() { return true; }
		public override bool IsSelectableWithoutModifiers() { return false; }
		public override bool IsSelectableWithModifiers() { return true; }
	}
}
