using System.Windows;
using Microsoft.Xna.Framework;

namespace DevMap.MonoGameControls;

public class DragStateArgs(IInputElement element, DragEventArgs args)
{
	private readonly IInputElement _element = element;
	private readonly DragEventArgs _args = args;

	public Vector2 Position => _args.GetPosition(_element).ToVector2();
	public T? GetData<T>() where T : class => _args?.Data?.GetData(typeof(T)) as T;
}
