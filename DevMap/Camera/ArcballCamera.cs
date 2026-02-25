using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace DevMap.Camera;

public class ArcballCamera
{
	public float Yaw { get; set; }
	public float Pitch { get; set; }
	public float Distance { get; set; } = 3.0f;
	public float MinDistance { get; set; } = 1.2f;
	public float MaxDistance { get; set; } = 10.0f;
	public float FieldOfView { get; set; } = MathHelper.PiOver4;
	public float NearPlane { get; set; } = 0.01f;
	public float FarPlane { get; set; } = 100f;

	public Matrix View { get; private set; }
	public Matrix Projection { get; private set; }
	public Vector3 Position { get; private set; }

	private Vector2 _lastMousePosition;
	private bool _isDragging;

	private const float DragSensitivity = 0.005f;
	private const float ZoomFactor = 1.1f;

	public void HandleMouseDown(Vector2 position)
	{
		_isDragging = true;
		_lastMousePosition = position;
	}

	public void HandleMouseMove(Vector2 position)
	{
		if (!_isDragging)
			return;

		float dx = position.X - _lastMousePosition.X;
		float dy = position.Y - _lastMousePosition.Y;

		float scaledSensitivity = DragSensitivity * (Distance / MaxDistance);
		Yaw -= dx * scaledSensitivity;
		Pitch += dy * scaledSensitivity;

		Pitch = MathHelper.Clamp(Pitch, -MathHelper.ToRadians(89f), MathHelper.ToRadians(89f));

		_lastMousePosition = position;
	}

	public void HandleMouseUp()
	{
		_isDragging = false;
	}

	public void HandleMouseWheel(int delta)
	{
		int notches = delta / 120;
		float factor = notches > 0 ? 1f / ZoomFactor : ZoomFactor;
		for (int i = 0; i < System.Math.Abs(notches); i++)
			Distance *= factor;

		Distance = MathHelper.Clamp(Distance, MinDistance, MaxDistance);
	}

	public void UpdateMatrices(Viewport viewport)
	{
		float cosPitch = (float)System.Math.Cos(Pitch);
		float sinPitch = (float)System.Math.Sin(Pitch);
		float cosYaw = (float)System.Math.Cos(Yaw);
		float sinYaw = (float)System.Math.Sin(Yaw);

		Position = new Vector3(
			Distance * cosPitch * sinYaw,
			Distance * sinPitch,
			Distance * cosPitch * cosYaw
		);

		View = Matrix.CreateLookAt(Position, Vector3.Zero, Vector3.Up);
		Projection = Matrix.CreatePerspectiveFieldOfView(
			FieldOfView,
			viewport.AspectRatio,
			NearPlane,
			FarPlane
		);
	}
}
