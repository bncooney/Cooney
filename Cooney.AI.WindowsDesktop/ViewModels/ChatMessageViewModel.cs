using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using Cooney.AI.WindowsDesktop.Models;

namespace Cooney.AI.WindowsDesktop.ViewModels;

/// <summary>
/// ViewModel wrapper for ChatMessage that handles presentation concerns
/// like debounced markdown rendering for streaming responses.
/// </summary>
public partial class ChatMessageViewModel : ObservableObject
{
	private readonly ChatMessage _message;
	private readonly System.Timers.Timer? _debounceTimer;
	private DateTime _lastRenderTime = DateTime.MinValue;

	/// <summary>
	/// The sender of the message (exposed from underlying model).
	/// </summary>
	public Sender From => _message.From;

	/// <summary>
	/// The timestamp of the message (exposed from underlying model).
	/// </summary>
	public DateTime Timestamp => _message.Timestamp;

	/// <summary>
	/// The rendered markdown text (updated periodically during streaming).
	/// </summary>
	[ObservableProperty]
	private string _renderedText = string.Empty;

	/// <summary>
	/// Raw text buffer that hasn't been rendered yet (updated immediately during streaming).
	/// </summary>
	[ObservableProperty]
	private string _pendingText = string.Empty;

	/// <summary>
	/// Creates a new ChatMessageViewModel.
	/// </summary>
	/// <param name="message">The underlying chat message model.</param>
	/// <param name="enableDebouncing">Enable debounced rendering for streaming responses.</param>
	public ChatMessageViewModel(ChatMessage message, bool enableDebouncing = false)
	{
		_message = message;
		_renderedText = message.Text;

		if (enableDebouncing)
		{
			_debounceTimer = new System.Timers.Timer(1000) // 1 second between markdown renders
			{
				AutoReset = false
			};
			_debounceTimer.Elapsed += (s, e) =>
			{
				Application.Current?.Dispatcher.Invoke(DrainPendingText);
			};
		}
	}

	/// <summary>
	/// Appends text to the message with optional debounced rendering.
	/// </summary>
	public void AppendText(string textChunk)
	{
		_message.AppendText(textChunk);

		if (_debounceTimer != null)
		{
			PendingText += textChunk;

			var timeSinceLastRender = DateTime.UtcNow - _lastRenderTime;
			if (timeSinceLastRender.TotalMilliseconds >= 1000)
			{
				DrainPendingText();
			}
			else
			{
				_debounceTimer.Stop();
				_debounceTimer.Start();
			}
		}
		else
		{
			RenderedText = _message.Text;
		}
	}

	/// <summary>
	/// Forces immediate rendering of all pending text.
	/// </summary>
	public void Flush()
	{
		_debounceTimer?.Stop();
		if (!string.IsNullOrEmpty(PendingText))
		{
			DrainPendingText();
		}
	}

	private void DrainPendingText()
	{
		_lastRenderTime = DateTime.UtcNow;
		RenderedText = _message.Text;
		PendingText = string.Empty;
	}
}
