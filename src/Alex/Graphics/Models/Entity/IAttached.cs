using Alex.Api;
using Alex.API.Graphics;
using Alex.Gamestates;
using Microsoft.Xna.Framework;

namespace Alex.Graphics.Models.Entity
{
	public interface IAttached
	{
		IAttached Parent { get; set; }
		void Render(IRenderArgs args, Microsoft.Xna.Framework.Graphics.Effect effect);
		void Update(IUpdateArgs args, MCMatrix characterMatrix, Vector3 parentScale);

		string Name { get; }

		void AddChild(IAttached modelBone);
		void Remove(IAttached modelBone);
	}

	public class AttachedRenderArgs : RenderArgs
	{
		public PooledVertexBuffer Buffer { get; set; }
	}
}