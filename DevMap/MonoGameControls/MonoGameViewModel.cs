using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace DevMap.MonoGameControls;

public class MonoGameViewModel : IMonoGameViewModel, INotifyPropertyChanged
{
	public event PropertyChangedEventHandler? PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null!)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}

	public IGraphicsDeviceService GraphicsDeviceService { get; set; } = default!;
	protected GraphicsDevice GraphicsDevice => GraphicsDeviceService?.GraphicsDevice!;
	protected MonoGameServiceProvider Services { get; private set; } = default!;
	protected ContentManager Content { get; set; } = default!;
	protected List<IGameComponent> Components { get; } = [];

	public virtual void Initialize()
	{
		Services = new MonoGameServiceProvider();
		Services.AddService(GraphicsDeviceService);
		Content = new ContentManager(Services) { RootDirectory = "Content" };
	}

	public virtual void LoadContent() { }
	public virtual void UnloadContent() { }

	public virtual void Update(GameTime gameTime)
	{
		foreach (var component in Components)
			if (component is IUpdateable updateable && updateable.Enabled)
				updateable.Update(gameTime);
	}

	public virtual void Draw(GameTime gameTime) { }

	void IMonoGameViewModel.Draw(GameTime gameTime)
	{
		foreach (var component in Components)
			if (component is IDrawable drawable && drawable.Visible)
				drawable.Draw(gameTime);
		Draw(gameTime);
	}

	public virtual void AfterRender() { }
	public virtual void OnActivated(object sender, EventArgs args) { }
	public virtual void OnDeactivated(object sender, EventArgs args) { }
	public virtual void OnExiting(object sender, EventArgs args) { }
	public virtual void OnMouseDown(MouseStateArgs mouseState) { }
	public virtual void OnMouseMove(MouseStateArgs mouseState) { }
	public virtual void OnMouseUp(MouseStateArgs mouseState) { }
	public virtual void OnMouseWheel(MouseStateArgs args, int delta) { }
	public virtual void OnDrop(DragStateArgs dragState) { }
	public virtual void SizeChanged(object sender, SizeChangedEventArgs args) { }

	public void Dispose()
	{
		Content?.Dispose();
	}
}
