using Alex.Worlds;
using MiNET.Entities;

namespace Alex.Entities.Passive
{
	public class Mule : ChestedHorse
	{
		public Mule(World level) : base((EntityType)25, level)
		{
			Height = 1.6;
			Width = 1.396484;
		}
	}
}
