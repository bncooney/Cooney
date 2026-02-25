using Microsoft.Xna.Framework;

namespace DevMap.MonoGameControls;

public static class WpfToMonoGameExtensions
{
	public static Vector2 ToVector2(this System.Windows.Point point) =>
		new((float)point.X, (float)point.Y);
}
