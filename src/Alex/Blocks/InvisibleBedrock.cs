﻿using Alex.Graphics.Items;

namespace Alex.Blocks
{
	public class InvisibleBedrock : Block
	{
		public InvisibleBedrock() : base(95, 0)
		{
			Renderable = false;
			HasHitbox = false;
		}
	}
}