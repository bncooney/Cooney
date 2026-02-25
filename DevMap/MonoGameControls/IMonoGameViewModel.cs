using System;
using System.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DevMap.MonoGameControls;

public interface IMonoGameViewModel : IDisposable
{
	IGraphicsDeviceService GraphicsDeviceService { get; set; }

	void Initialize();
	void LoadContent();
	void UnloadContent();
	void Update(GameTime gameTime);
	void Draw(GameTime gameTime);
	void AfterRender();
	void OnActivated(object sender, EventArgs args);
	void OnDeactivated(object sender, EventArgs args);
	void OnExiting(object sender, EventArgs args);

	void OnMouseDown(MouseStateArgs mouseState);
	void OnMouseMove(MouseStateArgs mouseState);
	void OnMouseUp(MouseStateArgs mouseState);
	void OnMouseWheel(MouseStateArgs args, int delta);
	void OnDrop(DragStateArgs dragState);

	void SizeChanged(object sender, SizeChangedEventArgs args);
}
