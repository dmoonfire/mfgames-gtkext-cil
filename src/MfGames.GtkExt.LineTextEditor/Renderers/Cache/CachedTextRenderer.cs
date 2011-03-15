#region Copyright and License

// Copyright (c) 2009-2011, Moonfire Games
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

#endregion

#region Namespaces

using System;

using C5;

using MfGames.GtkExt.LineTextEditor.Buffers;
using MfGames.GtkExt.LineTextEditor.Events;
using MfGames.GtkExt.LineTextEditor.Interfaces;
using MfGames.GtkExt.LineTextEditor.Visuals;

using Pango;

using Rectangle=Cairo.Rectangle;

#endregion

namespace MfGames.GtkExt.LineTextEditor.Renderers.Cache
{
	/// <summary>
	/// Implements a dynamic cache around a <see cref="TextRenderer"/> to reduce
	/// processing overhead at the expense of larger memory usage.
	/// </summary>
	public class CachedTextRenderer : TextRendererDecorator
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedTextRenderer"/> class.
		/// </summary>
		/// <param name="displayContext">The display context.</param>
		/// <param name="lineBuffer">The line buffer.</param>
		public CachedTextRenderer(
			IDisplayContext displayContext,
			LineBuffer lineBuffer)
			: this(displayContext, new LineBufferTextRenderer(displayContext, lineBuffer)
				)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedTextRenderer"/> class.
		/// </summary>
		/// <param name="displayContext">The display context.</param>
		/// <param name="textRenderer">The text renderer.</param>
		public CachedTextRenderer(
			IDisplayContext displayContext,
			TextRenderer textRenderer)
			: this(displayContext, textRenderer, 8, 16)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="CachedTextRenderer"/> class.
		/// </summary>
		/// <param name="displayContext">The display context.</param>
		/// <param name="textRenderer">The text renderer.</param>
		/// <param name="maximumLoadedWindows">The maximum loaded windows.</param>
		/// <param name="windowSize">Size of the window.</param>
		public CachedTextRenderer(
			IDisplayContext displayContext,
			TextRenderer textRenderer,
			int maximumLoadedWindows,
			int windowSize)
			: base(displayContext, textRenderer)
		{
			// Set the cache window properties.
			this.windowSize = windowSize;

			// Create the collection of windows.
			windows = new ArrayList<CachedWindow>();

			// Pre-create the window arrays.
			allocatedLines = new ArrayList<CachedLine[]>();

			for (int index = 0; index < maximumLoadedWindows; index++)
			{
				// Create the array and add it to the allocated list.
				var lines = new CachedLine[windowSize];

				allocatedLines.Add(lines);

				for (int line = 0; line < windowSize; line++)
				{
					lines[line] = new CachedLine();
				}
			}
		}

		#endregion

		#region Caches

		/// <summary>
		/// Contains a list of allocated lines that were created but are currently
		/// not in use. This is to avoid memory pressure by allocating them once
		/// and not freeing the memory until the class is freed.
		/// </summary>
		private readonly ArrayList<CachedLine[]> allocatedLines;

		/// <summary>
		/// Contains all the windows in this cache.
		/// </summary>
		private readonly ArrayList<CachedWindow> windows;

		/// <summary>
		/// Contains the size of the individual windows.
		/// </summary>
		private readonly int windowSize;

		/// <summary>
		/// Gets the size of a window cache.
		/// </summary>
		/// <value>The size of the window.</value>
		public int WindowSize
		{
			get { return windowSize; }
		}

		/// <summary>
		/// Goes through and makes sure all the windows are allocated for the
		/// underlying buffer.
		/// </summary>
		private void AllocateWindows()
		{
			// If we have no lines, then we don't need any buffers.
			int lineCount = LineBuffer.LineCount;

			if (lineCount == 0)
			{
				Clear();
			}

			// We need a window for every windowSize lines.
			int windowsNeeded = lineCount / windowSize;

			if (lineCount % windowSize > 0)
			{
				windowsNeeded++;
			}

			// If we don't have enough, allocate more.
			while (windows.Count < windowsNeeded)
			{
				windows.Add(new CachedWindow(this, windows.Count));
			}

			// If we have too many, then free them.
			while (windows.Count > windowsNeeded)
			{
				CachedWindow window = windows.RemoveLast();
				Clear(window);
			}
		}

		/// <summary>
		/// Clears out all the windows and returns the larger arrays back into
		/// the allocation list.
		/// </summary>
		private void Clear()
		{
			// Go through all the windows and returned the allocated lines back
			// to the list.
			foreach (CachedWindow window in windows)
			{
				Clear(window);
			}

			// Clear out the array.
			windows.Clear();
		}

		/// <summary>
		/// Clears the specified window of allocations.
		/// </summary>
		/// <param name="window">The window.</param>
		private void Clear(CachedWindow window)
		{
			if (window.Lines != null)
			{
				// Clear out the lines to make sure the garbage collection can
				// release them as needed.
				foreach (CachedLine line in window.Lines)
				{
					line.Reset();
				}

				// Move the lines list back into the allocation list.
				allocatedLines.Add(window.Lines);
				window.Lines = null;
			}
		}

		/// <summary>
		/// Clears the least recently used window that has lines.
		/// </summary>
		private void ClearLeastRecentlyUsedWindow()
		{
			// Go through the windows and find the cache window that has the
			// oldest data.
			int lruWindowIndex = -1;
			DateTime lruWindowAccessed = DateTime.MaxValue;

			for (int index = 0; index < windows.Count; index++)
			{
				// If the window doesn't have lines, we don't use it.
				CachedWindow window = windows[index];

				if (window.Lines == null)
				{
					continue;
				}

				// Check to see if this window is older than the current.
				if (window.LastAccessed < lruWindowAccessed)
				{
					lruWindowAccessed = window.LastAccessed;
					lruWindowIndex = index;
				}
			}

			// The index will contains the last index.
			if (lruWindowIndex == -1)
			{
				throw new Exception("Cannot find LRU cache window");
			}

			// Clear this window.
			Clear(windows[lruWindowIndex]);
		}

		/// <summary>
		/// Gets a set of allocated cached lines, releasing any as needed.
		/// </summary>
		/// <returns></returns>
		internal CachedLine[] GetAllocatedCachedLines()
		{
			// Get an array of lines from the list.
			if (allocatedLines.Count == 0)
			{
				// We don't have any allocated lines, so free the last.
				ClearLeastRecentlyUsedWindow();
			}

			// Return the first set of allocated lines in the array.
			return allocatedLines.Pop();
		}

		/// <summary>
		/// Gets the index of the window for a given line.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <returns></returns>
		private int GetWindowIndex(int line)
		{
			// Figure out the window based on the windowSize.
			int window = line / windowSize;
			return window;
		}

		/// <summary>
		/// Marks the individual windows as needing to recalculate their heights.
		/// </summary>
		public override void Reset()
		{
			// Reset the line heights.
			lineHeight = null;

			// Reset each of the windows.
			foreach (CachedWindow window in windows)
			{
				window.Reset();
				Clear(window);
			}
		}

		#endregion

		#region Line Layout

		private int? lineHeight;

		/// <summary>
		/// Sets the width on the underlying buffer and resets the cache windows
		/// if the width changes.
		/// </summary>
		/// <value>The width.</value>
		public override int Width
		{
			set
			{
				// Check to see if we have a chance. If we don't, then we don't
				// have to do anything.
				if (base.Width == value)
				{
					return;
				}

				// Reset the cache windows since the line layout didn't change,
				// but we have to recalculate all the heights again.
				Reset();

				// Set the new width in the underlying buffer.
				base.Width = value;
			}
		}

		/// <summary>
		/// Gets the line layout for a given line.
		/// </summary>
		/// <param name="lineIndex">The line.</param>
		/// <returns></returns>
		public override Layout GetLineLayout(int lineIndex)
		{
			// Make sure we have all the windows allocated.
			AllocateWindows();

			// Go through the windows and find the starting one.
			lineIndex = LineBuffer.NormalizeLineIndex(lineIndex);
			int windowIndex = GetWindowIndex(lineIndex);
			CachedWindow window = windows[windowIndex];

			// Get the layout from the window.
			Layout layout = window.GetLineLayout(DisplayContext, lineIndex);
			return layout;
		}

		/// <summary>
		/// Uses the cache to retrieve the heights of the individual lines.
		/// </summary>
		/// <param name="startLineIndex">The start line.</param>
		/// <param name="endLineIndex">The end line.</param>
		/// <returns></returns>
		public override int GetLineLayoutHeight(
			int startLineIndex,
			int endLineIndex)
		{
			// Make sure we have all the windows allocated.
			AllocateWindows();

			// Normalize the end line so we don't go over our bounds.
			endLineIndex = LineBuffer.NormalizeLineIndex(endLineIndex);

			// Go through the windows and find the starting one.
			int startingWindowIndex = GetWindowIndex(startLineIndex);
			int endingWindowIndex = GetWindowIndex(endLineIndex);

			CachedWindow startingWindow = windows[startingWindowIndex];
			CachedWindow endingWindow = windows[endingWindowIndex];

			// Make sure that both the starting and ending windows are populated.
			// This handles if the windows are the same since Populate() checks
			// the loaded status.
			startingWindow.Populate(DisplayContext);
			endingWindow.Populate(DisplayContext);

			// Get the height of the lines inside the starting window.
			int height = startingWindow.GetLineLayoutHeight(
				DisplayContext, startLineIndex, endLineIndex);

			// If the end window is different, get those line heights also.
			if (startingWindowIndex != endingWindowIndex)
			{
				height += endingWindow.GetLineLayoutHeight(
					DisplayContext, startLineIndex, endLineIndex);
			}

			// Retrieve all the cache windows between the two ranges.
			for (int windowIndex = startingWindowIndex + 1;
			     windowIndex < endingWindowIndex;
			     windowIndex++)
			{
				CachedWindow window = windows[windowIndex];
				height += window.GetLineLayoutHeight(DisplayContext);
			}

			// Return the resulting height of the region.
			return height;
		}

		/// <summary>
		/// Gets the height of a single line of "normal" text.
		/// </summary>
		/// <returns></returns>
		public override int GetLineLayoutHeight()
		{
			if (!lineHeight.HasValue)
			{
				lineHeight = base.GetLineLayoutHeight();
			}

			return lineHeight.Value;
		}

		/// <summary>
		/// Gets the lines that are visible in the given view area.
		/// </summary>
		/// <param name="viewArea">The view area.</param>
		/// <param name="startLine">The start line.</param>
		/// <param name="endLine">The end line.</param>
		public override void GetLineLayoutRange(
			Rectangle viewArea,
			out int startLine,
			out int endLine)
		{
			// Go through and find the windows that have the starting and ending
			// area.
			int height = 0;
			int startWindowIndex = -1;
			int endWindowIndex = -1;
			int startWindowHeight = 0;
			int endWindowHeight = 0;
			double bottom = viewArea.Y + viewArea.Height;

			foreach (CachedWindow window in windows)
			{
				// Get the window height.
				int windowHeight = window.GetLineLayoutHeight(DisplayContext);

				// Check for starting line.
				if (viewArea.Y >= height && viewArea.Y <= height + windowHeight)
				{
					// The starting line is somewhere in this window.
					startWindowIndex = window.WindowIndex;
					startWindowHeight = height;
				}

				// Check for ending line.
				if (bottom >= height && bottom <= height + windowHeight)
				{
					// The starting line is somewhere in this window.
					endWindowIndex = window.WindowIndex;
					endWindowHeight = height;
				}

				// If we have both a start and end window, we're done with this
				// loop and can process it.
				if (startWindowIndex >= 0 && endWindowIndex >= 0)
				{
					break;
				}

				// Add to the current height.
				height += windowHeight;
			}

			// Make sure we have a starting and ending line index. If we don't have
			// a starting line, then just show the last one.
			if (startWindowIndex == -1)
			{
				startLine = endLine = LineBuffer.LineCount - 1;
				return;
			}

			// Determine what the start line is inside the starting cache.
			var startWindowOffset = (int) (viewArea.Y - startWindowHeight);
			startLine = windows[startWindowIndex].GetLineLayoutContaining(
				DisplayContext, startWindowOffset);

			// Get the ending line from the ending cache.
			if (endWindowIndex == -1)
			{
				endLine = LineBuffer.LineCount - 1;
				return;
			}

			var endWindowOffset = (int) (viewArea.Y + viewArea.Height - endWindowHeight);
			endLine = windows[endWindowIndex].GetLineLayoutContaining(
				DisplayContext, endWindowOffset);
		}

		/// <summary>
		/// Gets the line style for a given line.
		/// </summary>
		/// <param name="lineIndex">The line number.</param>
		/// <returns></returns>
		public override LineStyle GetLineStyle(int lineIndex)
		{
			// Make sure we have all the windows allocated.
			AllocateWindows();

			// Go through the windows and find the starting one.
			lineIndex = LineBuffer.NormalizeLineIndex(lineIndex);
			int windowIndex = GetWindowIndex(lineIndex);
			CachedWindow window = windows[windowIndex];

			// Get the layout from the window.
			LineStyle style = window.GetLineStyle(DisplayContext, lineIndex);
			return style;
		}

		#endregion

		#region Buffer Editing

		/// <summary>
		/// Called when a line is changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The args.</param>
		protected override void OnLineChanged(
			object sender,
			LineChangedArgs args)
		{
			// Get the window for the line change and reset that line.
			int cachedWindowIndex = GetWindowIndex(args.LineIndex);
			CachedWindow cachedWindow = windows[cachedWindowIndex];

			cachedWindow.Reset(args.LineIndex - cachedWindow.WindowStartLine);
			//Clear(cachedWindow);

			// Call the base implementation to cascade the events up.
			base.OnLineChanged(sender, args);
		}

		/// <summary>
		/// Called when the inner buffer deletes lines.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The args.</param>
		protected override void OnLinesDeleted(
			object sender,
			LineRangeEventArgs args)
		{
			Clear();
			AllocateWindows();
			base.OnLinesDeleted(sender, args);
		}

		/// <summary>
		/// Called when the inner buffer inserts lines.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The args.</param>
		protected override void OnLinesInserted(
			object sender,
			LineRangeEventArgs args)
		{
			Clear();
			AllocateWindows();
			base.OnLinesInserted(sender, args);
		}

		#endregion

		#region Selection

		/// <summary>
		/// Updates the caret/selection on screen.
		/// </summary>
		/// <param name="displayContext">The display context.</param>
		/// <param name="previousSelection">The previous selection.</param>
		public override void UpdateSelection(
			IDisplayContext displayContext,
			BufferSegment previousSelection)
		{
			// Only update the caches if one of the current or previous
			// selections was actually selecting something.
			BufferSegment currentSelection = displayContext.Caret.Selection;

			if (!previousSelection.IsEmpty || !currentSelection.IsEmpty)
			{
				// Clear out the cache for all the lines in the new and old selections.
				int endLineIndex =
					displayContext.LineBuffer.NormalizeLineIndex(
						currentSelection.EndPosition.LineIndex);

				for (int lineIndex = currentSelection.StartPosition.LineIndex;
				     lineIndex <= endLineIndex;
				     lineIndex++)
				{
					// Get the window for the line change and reset that window.
					int cachedWindowIndex = GetWindowIndex(lineIndex);
					CachedWindow cachedWindow = windows[cachedWindowIndex];

					cachedWindow.Reset(lineIndex - cachedWindow.WindowStartLine);
				}

				for (int lineIndex = previousSelection.StartPosition.LineIndex;
				     lineIndex <= endLineIndex;
				     lineIndex++)
				{
					// Get the window for the line change and reset that window.
					int cachedWindowIndex = GetWindowIndex(lineIndex);
					CachedWindow cachedWindow = windows[cachedWindowIndex];

					cachedWindow.Reset(lineIndex - cachedWindow.WindowStartLine);
				}
			}

			// Call the base implementation.
			// TODO base.UpdateSelection(displayContext, previousSelection);
		}

		#endregion
	}
}