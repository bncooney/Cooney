// Based on MonoGame.WpfCore by craftworkgames
// Original D3DImage interop technique from SharpDX samples
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel (MIT License)

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DevMap.MonoGameControls;

public sealed class MonoGameContentControl : ContentControl, IDisposable
{
	private readonly MonoGameGraphicsDeviceService _graphicsDeviceService = new();
	private static int _instanceCount;
	private IMonoGameViewModel? _viewModel;
	private readonly GameTime _gameTime = new();
	private readonly Stopwatch _stopwatch = new();
	private D3DImage _direct3DImage = default!;
	private RenderTarget2D? _renderTarget;
	private RenderTarget2D? _msaaRenderTarget;
	private SharpDX.Direct3D9.Texture? _renderTargetD3D9;
	private bool _isFirstLoad = true;
	private bool _isInitialized;
	private SpriteBatch? _spriteBatch;
	private const int MsaaSampleCount = 4;

	public MonoGameContentControl()
	{
		if (DesignerProperties.GetIsInDesignMode(this))
			return;

		_instanceCount++;
		Focusable = true;
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		DataContextChanged += (sender, args) =>
		{
			_viewModel = args.NewValue as IMonoGameViewModel;

			_viewModel?.GraphicsDeviceService = _graphicsDeviceService;
		};
	}

	public GraphicsDevice GraphicsDevice => _graphicsDeviceService?.GraphicsDevice!;

	public bool IsDisposed { get; private set; }

	public void Dispose()
	{
		if (IsDisposed)
			return;

		_viewModel?.Dispose();
		_renderTarget?.Dispose();
		_renderTargetD3D9?.Dispose();
		_instanceCount--;

		_spriteBatch?.Dispose();

		if (_instanceCount <= 0)
			_graphicsDeviceService?.Dispose();

		IsDisposed = true;
	}

	protected override void OnDrop(DragEventArgs e)
	{
		_viewModel?.OnDrop(new DragStateArgs(this, e));
		base.OnDrop(e);
	}

	protected override void OnGotFocus(RoutedEventArgs e)
	{
		_viewModel?.OnActivated(this, EventArgs.Empty);
		base.OnGotFocus(e);
	}

	protected override void OnLostFocus(RoutedEventArgs e)
	{
		_viewModel?.OnDeactivated(this, EventArgs.Empty);
		base.OnLostFocus(e);
	}

	protected override void OnMouseDown(MouseButtonEventArgs e)
	{
		Focus();
		_viewModel?.OnMouseDown(new MouseStateArgs(this, e));
		base.OnMouseDown(e);
	}

	protected override void OnMouseMove(MouseEventArgs e)
	{
		_viewModel?.OnMouseMove(new MouseStateArgs(this, e));
		base.OnMouseMove(e);
	}

	protected override void OnMouseUp(MouseButtonEventArgs e)
	{
		_viewModel?.OnMouseUp(new MouseStateArgs(this, e));
		base.OnMouseUp(e);
	}

	protected override void OnMouseWheel(MouseWheelEventArgs e)
	{
		_viewModel?.OnMouseWheel(new MouseStateArgs(this, e), e.Delta);
		base.OnMouseWheel(e);
	}

	private void Start()
	{
		if (_isInitialized)
			return;

		var window = Window.GetWindow(this) ?? throw new InvalidOperationException("The control must be placed in a Window");

		window.Closing += (sender, args) => _viewModel?.OnExiting(this, EventArgs.Empty);
		window.ContentRendered += (sender, args) =>
		{
			if (_isFirstLoad)
			{
				_graphicsDeviceService.StartDirect3D(window);
				_viewModel?.Initialize();
				_viewModel?.LoadContent();
				_isFirstLoad = false;
			}
		};

		_direct3DImage = new D3DImage();
		AddChild(new Image { Source = _direct3DImage, Stretch = Stretch.None });

		_renderTarget = CreateRenderTarget();
		CompositionTarget.Rendering += OnRender;
		_stopwatch.Start();
		_isInitialized = true;
		SizeChanged += (sender, args) => _viewModel?.SizeChanged(sender, args);
	}

	protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
	{
		base.OnRenderSizeChanged(sizeInfo);
		Start();
		ResetBackBufferReference();
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		Start();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		_viewModel?.UnloadContent();

		if (_graphicsDeviceService != null)
		{
			CompositionTarget.Rendering -= OnRender;
			ResetBackBufferReference();
			_graphicsDeviceService.DeviceResetting -= OnGraphicsDeviceServiceDeviceResetting;
		}
	}

	private void OnGraphicsDeviceServiceDeviceResetting(object? sender, EventArgs e)
	{
		ResetBackBufferReference();
	}

	private void ResetBackBufferReference()
	{
		if (DesignerProperties.GetIsInDesignMode(this))
			return;

		_msaaRenderTarget?.Dispose();
		_msaaRenderTarget = null;

		_renderTarget?.Dispose();
		_renderTarget = null;

		_renderTargetD3D9?.Dispose();
		_renderTargetD3D9 = null;

		if (_direct3DImage != null)
		{
			_direct3DImage.Lock();
			_direct3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, IntPtr.Zero);
			_direct3DImage.Unlock();
		}
	}

	private RenderTarget2D? CreateRenderTarget()
	{
		var actualWidth = (int)ActualWidth;
		var actualHeight = (int)ActualHeight;

		if (actualWidth == 0 || actualHeight == 0)
			return null;

		if (GraphicsDevice == null)
			return null;

		// Shared render target for D3DImage interop (no MSAA — shared handles don't support it)
		var renderTarget = new RenderTarget2D(GraphicsDevice, actualWidth, actualHeight,
			false, SurfaceFormat.Bgra32, DepthFormat.Depth24Stencil8, 1,
			RenderTargetUsage.PlatformContents, true);

		// MSAA render target for anti-aliased rendering, resolved to shared target before present
		_msaaRenderTarget = new RenderTarget2D(GraphicsDevice, actualWidth, actualHeight,
			false, SurfaceFormat.Bgra32, DepthFormat.Depth24Stencil8, MsaaSampleCount,
			RenderTargetUsage.DiscardContents, false);

		var handle = renderTarget.GetSharedHandle();

		if (handle == IntPtr.Zero)
			throw new ArgumentException("Handle could not be retrieved");

		_renderTargetD3D9 = new SharpDX.Direct3D9.Texture(_graphicsDeviceService.Direct3DDevice,
			renderTarget.Width, renderTarget.Height,
			1, SharpDX.Direct3D9.Usage.RenderTarget, SharpDX.Direct3D9.Format.A8R8G8B8,
			SharpDX.Direct3D9.Pool.Default, ref handle);

		using (var surface = _renderTargetD3D9.GetSurfaceLevel(0))
		{
			_direct3DImage.Lock();
			_direct3DImage.SetBackBuffer(D3DResourceType.IDirect3DSurface9, surface.NativePointer);
			_direct3DImage.Unlock();
		}

		return renderTarget;
	}

	private void OnRender(object? sender, EventArgs e)
	{
		_gameTime.ElapsedGameTime = _stopwatch.Elapsed;
		_gameTime.TotalGameTime += _gameTime.ElapsedGameTime;
		_stopwatch.Restart();

		if (!_isFirstLoad && CanBeginDraw())
		{
			try
			{
				_direct3DImage.Lock();

				_renderTarget ??= CreateRenderTarget();

				_spriteBatch ??= new SpriteBatch(GraphicsDevice);

				if (_renderTarget != null && _msaaRenderTarget != null)
				{
					// Render to MSAA target
					GraphicsDevice.SetRenderTarget(_msaaRenderTarget);
					SetViewport();

					_viewModel?.Update(_gameTime);
					_viewModel?.Draw(_gameTime);

					// Resolve MSAA to shared target via SpriteBatch blit
					GraphicsDevice.SetRenderTarget(_renderTarget);

					_spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Opaque);
					_spriteBatch.Draw(_msaaRenderTarget, _msaaRenderTarget.Bounds, Microsoft.Xna.Framework.Color.White);
					_spriteBatch.End();

					GraphicsDevice.Flush();
					_direct3DImage.AddDirtyRect(new Int32Rect(0, 0, (int)ActualWidth, (int)ActualHeight));
				}
			}
			finally
			{
				_direct3DImage.Unlock();
				GraphicsDevice.SetRenderTarget(null);
				_viewModel?.AfterRender();
			}
		}
	}

	private bool CanBeginDraw()
	{
		if (_graphicsDeviceService == null)
			return false;

		if (!_direct3DImage.IsFrontBufferAvailable)
			return false;

		if (!HandleDeviceReset())
			return false;

		return true;
	}

	private void SetViewport()
	{
		var width = Math.Max(1, (int)ActualWidth);
		var height = Math.Max(1, (int)ActualHeight);
		GraphicsDevice.Viewport = new Viewport(0, 0, width, height);
	}

	private bool HandleDeviceReset()
	{
		if (GraphicsDevice == null)
			return false;

		var deviceNeedsReset = false;

		switch (GraphicsDevice.GraphicsDeviceStatus)
		{
			case GraphicsDeviceStatus.Lost:
				return false;
			case GraphicsDeviceStatus.NotReset:
				deviceNeedsReset = true;
				break;
		}

		if (deviceNeedsReset)
		{
			_graphicsDeviceService.ResetDevice((int)ActualWidth, (int)ActualHeight);
			return false;
		}

		return true;
	}
}
