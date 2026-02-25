using System;
using System.Globalization;
using System.Windows.Input;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using DevMap.Camera;
using DevMap.Globe;
using DevMap.MonoGameControls;
using DevMap.Tiles;
using DevMap.Util;

namespace DevMap;

public class GlobeViewModel : MonoGameViewModel
{
	private ArcballCamera _camera = null!;
	private MapCamera2D _mapCamera = null!;
	private GlobeRenderer _globeRenderer = null!;
	private FlatMapRenderer _flatMapRenderer = null!;
	private TileManager _tileManager = null!;
	private Viewport _lastViewport;

	// Mode switching
	private bool _isFlat;
	private const float FlatTransitionDistance = 1.8f; // Camera distance at which we switch to flat map

	// Smooth navigation animation state
	private bool _isAnimating;
	private float _animStartYaw, _animStartPitch;
	private float _animTargetYaw, _animTargetPitch;
	private double _animStartLat, _animStartLon;
	private double _animTargetLat, _animTargetLon;
	private float _animElapsed, _animDuration;

	// Cached raw values to avoid reformatting every frame
	private double _lastLat = double.NaN, _lastLon = double.NaN, _lastZoom = double.NaN;
	private double _lastClickLat = double.NaN, _lastClickLon = double.NaN;

	private string _clickCoordinates = "";
	public string ClickCoordinates
	{
		get => _clickCoordinates;
		private set { _clickCoordinates = value; OnPropertyChanged(); }
	}

	private string _navigateInput = "";
	public string NavigateInput
	{
		get => _navigateInput;
		set { _navigateInput = value; OnPropertyChanged(); }
	}

	private string _cameraPosition = "";
	public string CameraPosition
	{
		get => _cameraPosition;
		private set { _cameraPosition = value; OnPropertyChanged(); }
	}

	private string _zoomLevel = "";
	public string ZoomLevel
	{
		get => _zoomLevel;
		private set { _zoomLevel = value; OnPropertyChanged(); }
	}

	public ICommand NavigateCommand { get; }
	public ICommand GoToParisCommand { get; }
	public ICommand GoToNewYorkCommand { get; }
	public ICommand GoToSydneyCommand { get; }
	public ICommand ZoomInCommand { get; }
	public ICommand ZoomOutCommand { get; }
	public ICommand ZoomToCityCommand { get; }
	public ICommand ZoomToRegionCommand { get; }
	public ICommand ZoomToWorldCommand { get; }

	public GlobeViewModel()
	{
		NavigateCommand = new RelayCommand(NavigateToInput);
		GoToParisCommand = new RelayCommand(() => NavigateTo(48.8566, 2.3522));
		GoToNewYorkCommand = new RelayCommand(() => NavigateTo(40.7128, -74.0060));
		GoToSydneyCommand = new RelayCommand(() => NavigateTo(-33.8688, 151.2093));
		ZoomInCommand = new RelayCommand(() => SetZoom(GetCurrentZoom() + 1));
		ZoomOutCommand = new RelayCommand(() => SetZoom(GetCurrentZoom() - 1));
		ZoomToCityCommand = new RelayCommand(() => SetZoom(10));
		ZoomToRegionCommand = new RelayCommand(() => SetZoom(6));
		ZoomToWorldCommand = new RelayCommand(() => SetZoom(3));
	}

	public override void Initialize()
	{
		base.Initialize();

		_camera = new ArcballCamera
		{
			Distance = 3.0f,
			MinDistance = 1.2f,
			MaxDistance = 10.0f,
			FieldOfView = MathHelper.PiOver4,
		};

		_mapCamera = new MapCamera2D
		{
			MinZoom = 3.0,
			MaxZoom = 12.0,
		};
	}

	public override void LoadContent()
	{
		_tileManager = new TileManager(GraphicsDevice);
		_globeRenderer = new GlobeRenderer(GraphicsDevice);
		_flatMapRenderer = new FlatMapRenderer(GraphicsDevice);
	}

	public override void Update(GameTime gameTime)
	{
		// Animate navigation
		if (_isAnimating)
			UpdateAnimation(gameTime);

		var viewport = GraphicsDevice.Viewport;
		if (viewport.Width != _lastViewport.Width || viewport.Height != _lastViewport.Height)
		{
			_mapCamera.ViewportWidth = viewport.Width;
			_mapCamera.ViewportHeight = viewport.Height;
		}
		_lastViewport = viewport;

		UpdateCameraPosition();

		// In flat mode, keep arcball distance in sync with 2D zoom so transition check works
		if (_isFlat)
		{
			_camera.Distance = MercatorProjection.DistanceFromZoom(
				_mapCamera.Zoom, _camera.FieldOfView, _lastViewport.Height);
		}

		// Check for mode transition based on camera distance (screen-size independent)
		bool shouldBeFlat = _camera.Distance <= FlatTransitionDistance;
		if (shouldBeFlat != _isFlat)
		{
			if (shouldBeFlat)
				TransitionToFlat();
			else
				TransitionToGlobe();
		}

		// Update appropriate systems
		if (_isFlat)
		{
			_tileManager.UpdateFlat(_mapCamera);
		}

		if (!_isFlat)
		{
			_camera.UpdateMatrices(_lastViewport);
			_tileManager.Update(_camera, _lastViewport);
		}

		base.Update(gameTime);
	}

	private void TransitionToFlat()
	{
		// Convert arcball camera state to 2D map camera
		double latDeg = MathHelper.ToDegrees(_camera.Pitch);
		double lonDeg = MathHelper.ToDegrees(_camera.Yaw) + 90.0;
		if (lonDeg > 180)
			lonDeg -= 360;
		if (lonDeg < -180)
			lonDeg += 360;

		_mapCamera.CenterLat = latDeg;
		_mapCamera.CenterLon = lonDeg;
		// Use screen-aware zoom for tile resolution, but this only sets the initial
		// zoom when entering flat mode — afterwards MapCamera2D.Zoom is independent
		_mapCamera.Zoom = MercatorProjection.FractionalZoomFromDistance(
			_camera.Distance, _camera.FieldOfView, _lastViewport.Height);

		_isFlat = true;
	}

	private void TransitionToGlobe()
	{
		// Convert 2D map camera state back to arcball — set distance to just above
		// the transition threshold so we don't immediately re-enter flat mode
		_camera.Pitch = MathHelper.ToRadians((float)_mapCamera.CenterLat);
		_camera.Yaw = MathHelper.ToRadians((float)(_mapCamera.CenterLon - 90.0));
		_camera.Distance = FlatTransitionDistance + 0.01f;

		_isFlat = false;
	}

	public override void Draw(GameTime gameTime)
	{
		GraphicsDevice.Clear(Color.Black);

		if (_isFlat)
		{
			_flatMapRenderer.Draw(_mapCamera, _tileManager.VirtualAtlas);
		}
		else
		{
			_globeRenderer.Draw(_camera, _tileManager.AtlasTexture);
		}
	}

	public override void UnloadContent()
	{
		_globeRenderer?.Dispose();
		_flatMapRenderer?.Dispose();
		_tileManager?.Dispose();
	}

	public override void OnMouseDown(MouseStateArgs mouseState)
	{
		if (mouseState.LeftButton == ButtonState.Pressed)
		{
			_isAnimating = false;

			if (_isFlat)
			{
				_lastMousePosition = mouseState.Position;
				TryPickFlat(mouseState.Position);
			}
			else
			{
				_camera.HandleMouseDown(mouseState.Position);
				TryPickGlobe(mouseState.Position);
			}
		}
	}

	private Vector2 _lastMousePosition;

	private void TryPickFlat(Vector2 screenPos)
	{
		var (lat, lon) = _mapCamera.ScreenToGeo(screenPos);
		SetClickCoordinates(lat, lon);
	}

	private void TryPickGlobe(Vector2 screenPos)
	{
		var nearPoint = _lastViewport.Unproject(new Vector3(screenPos, 0f), _camera.Projection, _camera.View, Matrix.Identity);
		var farPoint = _lastViewport.Unproject(new Vector3(screenPos, 1f), _camera.Projection, _camera.View, Matrix.Identity);
		var direction = Vector3.Normalize(farPoint - nearPoint);

		float a = Vector3.Dot(direction, direction);
		float b = 2f * Vector3.Dot(nearPoint, direction);
		float c = Vector3.Dot(nearPoint, nearPoint) - 1f;
		float discriminant = b * b - 4f * a * c;

		if (discriminant < 0)
			return;

		float t = (-b - MathF.Sqrt(discriminant)) / (2f * a);
		var hit = nearPoint + direction * t;

		double latDeg = MathHelper.ToDegrees(MathF.Asin(hit.Y));
		double phi = MathHelper.ToDegrees(MathF.Atan2(hit.Z, hit.X));
		double lonDeg = 180.0 - phi;
		if (lonDeg > 180)
			lonDeg -= 360;
		if (lonDeg < -180)
			lonDeg += 360;

		SetClickCoordinates(latDeg, lonDeg);
	}

	private void SetClickCoordinates(double lat, double lon)
	{
		if (lat != _lastClickLat || lon != _lastClickLon)
		{
			_lastClickLat = lat;
			_lastClickLon = lon;
			ClickCoordinates = CoordinateFormat.ToDegreesString(lat, lon);
		}
	}

	public override void OnMouseMove(MouseStateArgs mouseState)
	{
		if (mouseState.LeftButton == ButtonState.Pressed)
		{
			if (_isFlat)
			{
				var delta = mouseState.Position - _lastMousePosition;
				_mapCamera.HandleMouseDrag(-delta);
				_lastMousePosition = mouseState.Position;
			}
			else
			{
				_camera.HandleMouseMove(mouseState.Position);
			}
		}
	}

	public override void OnMouseUp(MouseStateArgs mouseState)
	{
		if (!_isFlat)
			_camera.HandleMouseUp();
	}

	public override void OnMouseWheel(MouseStateArgs args, int delta)
	{
		if (_isFlat)
		{
			_mapCamera.HandleMouseWheel(delta);
		}
		else
		{
			_camera.HandleMouseWheel(delta);
		}
	}

	private double GetCurrentZoom()
	{
		if (_isFlat)
			return _mapCamera.Zoom;

		return MercatorProjection.FractionalZoomFromDistance(
			_camera.Distance, _camera.FieldOfView, _lastViewport.Height > 0 ? _lastViewport.Height : 720);
	}

	private void SetZoom(double zoom)
	{
		zoom = Math.Clamp(zoom, _mapCamera.MinZoom, _mapCamera.MaxZoom);
		int viewportHeight = _lastViewport.Height > 0 ? _lastViewport.Height : 720;
		float distance = MercatorProjection.DistanceFromZoom(zoom, _camera.FieldOfView, viewportHeight);

		// Transition to the appropriate mode if needed
		bool shouldBeFlat = distance <= FlatTransitionDistance;
		if (shouldBeFlat && !_isFlat)
			TransitionToFlat();
		else if (!shouldBeFlat && _isFlat)
			TransitionToGlobe();

		if (_isFlat)
		{
			_mapCamera.Zoom = zoom;
		}
		else
		{
			_camera.Distance = MathHelper.Clamp(distance, _camera.MinDistance, _camera.MaxDistance);
		}
	}

	private void UpdateCameraPosition()
	{
		double lat, lon;
		if (_isFlat)
		{
			lat = _mapCamera.CenterLat;
			lon = _mapCamera.CenterLon;
		}
		else
		{
			lat = MathHelper.ToDegrees(_camera.Pitch);
			lon = MathHelper.ToDegrees(_camera.Yaw) + 90.0;
			if (lon > 180)
				lon -= 360;
			if (lon < -180)
				lon += 360;
		}

		if (lat != _lastLat || lon != _lastLon)
		{
			_lastLat = lat;
			_lastLon = lon;
			CameraPosition = CoordinateFormat.ToDegreesString(lat, lon);
		}

		double currentZoom = GetCurrentZoom();
		if (currentZoom != _lastZoom)
		{
			_lastZoom = currentZoom;
			ZoomLevel = FormattableString.Invariant($"{currentZoom:F1}");
		}
	}

	private void UpdateAnimation(GameTime gameTime)
	{
		_animElapsed += (float)gameTime.ElapsedGameTime.TotalSeconds;
		float t = Math.Clamp(_animElapsed / _animDuration, 0f, 1f);
		float s = t * t * (3f - 2f * t); // Smooth-step

		if (_isFlat)
		{
			_mapCamera.CenterLat = _animStartLat + (_animTargetLat - _animStartLat) * s;
			_mapCamera.CenterLon = _animStartLon + (_animTargetLon - _animStartLon) * s;
		}
		else
		{
			_camera.Yaw = MathHelper.Lerp(_animStartYaw, _animTargetYaw, s);
			_camera.Pitch = MathHelper.Lerp(_animStartPitch, _animTargetPitch, s);
		}

		if (t >= 1f)
			_isAnimating = false;
	}

	private void NavigateToInput()
	{
		if (string.IsNullOrWhiteSpace(NavigateInput))
			return;

		var parts = NavigateInput.Split(',', StringSplitOptions.TrimEntries);
		if (parts.Length != 2)
			return;

		if (!TryParseCoord(parts[0], "NS", out double lat) ||
			!TryParseCoord(parts[1], "EW", out double lon))
			return;

		NavigateTo(lat, lon, smooth: true);
	}

	public void NavigateTo(double latDeg, double lonDeg, bool smooth = false, float durationSeconds = 1.5f)
	{
		NavigateInput = CoordinateFormat.ToDegreesString(latDeg, lonDeg);

		if (_isFlat)
		{
			if (smooth)
			{
				_animStartLat = _mapCamera.CenterLat;
				_animStartLon = _mapCamera.CenterLon;
				_animTargetLat = latDeg;
				_animTargetLon = lonDeg;
				_animElapsed = 0f;
				_animDuration = durationSeconds;
				_isAnimating = true;
			}
			else
			{
				_mapCamera.CenterLat = latDeg;
				_mapCamera.CenterLon = lonDeg;
				_isAnimating = false;
			}
		}
		else
		{
			float targetPitch = MathHelper.ToRadians((float)latDeg);
			float targetYaw = MathHelper.ToRadians((float)(lonDeg - 90.0));

			if (smooth)
			{
				_animStartYaw = _camera.Yaw;
				_animStartPitch = _camera.Pitch;

				float yawDiff = targetYaw - _animStartYaw;
				yawDiff = MathHelper.WrapAngle(yawDiff);
				_animTargetYaw = _animStartYaw + yawDiff;
				_animTargetPitch = targetPitch;

				_animElapsed = 0f;
				_animDuration = durationSeconds;
				_isAnimating = true;
			}
			else
			{
				_camera.Pitch = targetPitch;
				_camera.Yaw = targetYaw;
				_isAnimating = false;
			}
		}
	}

	private static bool TryParseCoord(string s, string dirChars, out double value)
	{
		value = 0;
		s = s.Trim();
		if (s.Length == 0)
			return false;

		double sign = 1;
		char last = char.ToUpperInvariant(s[^1]);
		if (last == dirChars[1])
		{
			sign = -1;
			s = s[..^1].Trim();
		}
		else if (last == dirChars[0])
		{
			s = s[..^1].Trim();
		}

		s = s.TrimEnd('\u00b0').Trim();

		if (!double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out double raw))
			return false;

		value = raw * sign;
		return true;
	}
}

public class RelayCommand(Action execute) : ICommand
{
	public event EventHandler? CanExecuteChanged;
	public bool CanExecute(object? parameter) => true;
	public void Execute(object? parameter) => execute();
}
