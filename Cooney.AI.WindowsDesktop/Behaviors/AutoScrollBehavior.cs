using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Cooney.AI.WindowsDesktop.Behaviors;

/// <summary>
/// Attached behavior that provides automatic scrolling for ScrollViewer controls.
/// Automatically scrolls to bottom when items are added or updated, but respects
/// manual user scrolling (resumes auto-scroll when user scrolls back to bottom).
/// </summary>
public static class AutoScrollBehavior
{
	#region Attached Properties

	/// <summary>
	/// Gets or sets the collection to monitor for changes.
	/// When items are added or item properties change, the ScrollViewer will auto-scroll.
	/// </summary>
	public static readonly DependencyProperty ItemsSourceProperty =
		DependencyProperty.RegisterAttached(
			"ItemsSource",
			typeof(INotifyCollectionChanged),
			typeof(AutoScrollBehavior),
			new PropertyMetadata(null, OnItemsSourceChanged));

	public static INotifyCollectionChanged? GetItemsSource(DependencyObject obj) =>
		(INotifyCollectionChanged?)obj.GetValue(ItemsSourceProperty);

	public static void SetItemsSource(DependencyObject obj, INotifyCollectionChanged? value) =>
		obj.SetValue(ItemsSourceProperty, value);

	/// <summary>
	/// Gets or sets whether auto-scroll is enabled (can be used to disable temporarily).
	/// </summary>
	public static readonly DependencyProperty IsEnabledProperty =
		DependencyProperty.RegisterAttached(
			"IsEnabled",
			typeof(bool),
			typeof(AutoScrollBehavior),
			new PropertyMetadata(true));

	public static bool GetIsEnabled(DependencyObject obj) =>
		(bool)obj.GetValue(IsEnabledProperty);

	public static void SetIsEnabled(DependencyObject obj, bool value) =>
		obj.SetValue(IsEnabledProperty, value);

	#endregion

	#region State Storage

	private static readonly DependencyProperty ScrollStateProperty =
		DependencyProperty.RegisterAttached(
			"ScrollState",
			typeof(ScrollState),
			typeof(AutoScrollBehavior),
			new PropertyMetadata(null));

	private class ScrollState
	{
		public bool IsUserScrolling { get; set; }
		public bool AutoScrollEnabled { get; set; } = true;
		public List<INotifyPropertyChanged> TrackedItems { get; } = [];
	}

	#endregion

	#region Event Handlers

	private static void OnItemsSourceChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
	{
		if (d is not ScrollViewer scrollViewer)
			return;

		// Clean up old subscription
		if (e.OldValue is INotifyCollectionChanged oldCollection)
		{
			oldCollection.CollectionChanged -= GetCollectionChangedHandler(scrollViewer);
			scrollViewer.ScrollChanged -= OnScrollChanged;

			var oldState = (ScrollState?)scrollViewer.GetValue(ScrollStateProperty);
			if (oldState != null)
			{
				foreach (var item in oldState.TrackedItems)
				{
					item.PropertyChanged -= GetItemPropertyChangedHandler(scrollViewer);
				}
				oldState.TrackedItems.Clear();
			}
		}

		// Set up new subscription
		if (e.NewValue is INotifyCollectionChanged newCollection)
		{
			var state = new ScrollState();
			scrollViewer.SetValue(ScrollStateProperty, state);

			newCollection.CollectionChanged += GetCollectionChangedHandler(scrollViewer);
			scrollViewer.ScrollChanged += OnScrollChanged;

			// Track existing items
			if (newCollection is IEnumerable enumerable)
			{
				foreach (var item in enumerable)
				{
					if (item is INotifyPropertyChanged notifyItem)
					{
						notifyItem.PropertyChanged += GetItemPropertyChangedHandler(scrollViewer);
						state.TrackedItems.Add(notifyItem);
					}
				}
			}
		}
	}

	private static NotifyCollectionChangedEventHandler GetCollectionChangedHandler(ScrollViewer scrollViewer)
	{
		return (sender, e) => OnCollectionChanged(scrollViewer, e);
	}

	private static void OnCollectionChanged(ScrollViewer scrollViewer, NotifyCollectionChangedEventArgs e)
	{
		var state = (ScrollState?)scrollViewer.GetValue(ScrollStateProperty);
		if (state == null)
			return;

		// Track new items for property changes
		if (e.NewItems != null)
		{
			foreach (var item in e.NewItems)
			{
				if (item is INotifyPropertyChanged notifyItem)
				{
					notifyItem.PropertyChanged += GetItemPropertyChangedHandler(scrollViewer);
					state.TrackedItems.Add(notifyItem);
				}
			}
		}

		// Untrack removed items
		if (e.OldItems != null)
		{
			foreach (var item in e.OldItems)
			{
				if (item is INotifyPropertyChanged notifyItem)
				{
					notifyItem.PropertyChanged -= GetItemPropertyChangedHandler(scrollViewer);
					state.TrackedItems.Remove(notifyItem);
				}
			}
		}

		// Handle reset (clear all)
		if (e.Action == NotifyCollectionChangedAction.Reset)
		{
			foreach (var item in state.TrackedItems)
			{
				item.PropertyChanged -= GetItemPropertyChangedHandler(scrollViewer);
			}
			state.TrackedItems.Clear();
		}

		// Auto-scroll on add
		if (state.AutoScrollEnabled && GetIsEnabled(scrollViewer) && e.Action == NotifyCollectionChangedAction.Add)
		{
			ScrollToBottom(scrollViewer, state);
		}
	}

	private static PropertyChangedEventHandler GetItemPropertyChangedHandler(ScrollViewer scrollViewer)
	{
		return (sender, e) => OnItemPropertyChanged(scrollViewer, e);
	}

	private static void OnItemPropertyChanged(ScrollViewer scrollViewer, PropertyChangedEventArgs e)
	{
		var state = (ScrollState?)scrollViewer.GetValue(ScrollStateProperty);
		if (state == null)
			return;

		// Scroll when text properties change (for streaming updates)
		if (state.AutoScrollEnabled && GetIsEnabled(scrollViewer) &&
			(e.PropertyName == "RenderedText" || e.PropertyName == "PendingText"))
		{
			ScrollToBottom(scrollViewer, state);
		}
	}

	private static void OnScrollChanged(object sender, ScrollChangedEventArgs e)
	{
		if (sender is not ScrollViewer scrollViewer)
			return;

		var state = (ScrollState?)scrollViewer.GetValue(ScrollStateProperty);
		if (state == null)
			return;

		// Detect user-initiated scrolling (content height didn't change)
		if (e.ExtentHeightChange == 0)
		{
			state.IsUserScrolling = true;
		}

		// Check if scrolled to bottom
		var isAtBottom = Math.Abs(scrollViewer.VerticalOffset + scrollViewer.ViewportHeight - scrollViewer.ExtentHeight) < 1.0;

		if (state.IsUserScrolling)
		{
			// Resume auto-scroll when user scrolls back to bottom
			state.AutoScrollEnabled = isAtBottom;
		}
	}

	private static void ScrollToBottom(ScrollViewer scrollViewer, ScrollState state)
	{
		scrollViewer.Dispatcher.BeginInvoke(() =>
		{
			scrollViewer.ScrollToBottom();
			state.IsUserScrolling = false;
		}, DispatcherPriority.Loaded);
	}

	#endregion
}
