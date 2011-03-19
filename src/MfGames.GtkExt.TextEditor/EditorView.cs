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
using System.Diagnostics;

using Cairo;

using Gdk;

using Gtk;

using MfGames.GtkExt.Extensions.Cairo;
using MfGames.GtkExt.Extensions.Pango;
using MfGames.GtkExt.TextEditor.Buffers;
using MfGames.GtkExt.TextEditor.Editing;
using MfGames.GtkExt.TextEditor.Interfaces;
using MfGames.GtkExt.TextEditor.Margins;
using MfGames.GtkExt.TextEditor.Models;
using MfGames.GtkExt.TextEditor.Models.Buffers;
using MfGames.GtkExt.TextEditor.Models.Styles;
using MfGames.GtkExt.TextEditor.Renderers;
using MfGames.GtkExt.TextEditor.Renderers.Cache;
using MfGames.GtkExt.TextEditor.Visuals;

using Pango;

using CairoHelper=Gdk.CairoHelper;
using Color=Cairo.Color;
using Context=Cairo.Context;
using Key=Gdk.Key;
using Layout=Pango.Layout;
using Rectangle=Cairo.Rectangle;
using Style=Gtk.Style;
using Window=Gdk.Window;
using WindowType=Gdk.WindowType;

#endregion

namespace MfGames.GtkExt.TextEditor
{
	/// <summary>
	/// The primary editor control for the virtualized line text editor.
	/// </summary>
	public class EditorView : Widget, IDisplayContext
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="EditorView"/> class.
		/// </summary>
		public EditorView()
		{
			// Set up the basic characteristics of the widget.
			Events = EventMask.PointerMotionMask | EventMask.ButtonPressMask |
			         EventMask.PointerMotionHintMask | EventMask.ButtonReleaseMask |
			         EventMask.EnterNotifyMask | EventMask.LeaveNotifyMask |
			         EventMask.VisibilityNotifyMask | EventMask.FocusChangeMask |
			         EventMask.ScrollMask | EventMask.KeyPressMask |
			         EventMask.KeyReleaseMask;
			DoubleBuffered = true;
			CanFocus = true;
			WidgetFlags |= WidgetFlags.NoWindow;

			// Set up the rest of the screen elements.
			margins = new MarginRendererCollection();
			margins.Add(new LineNumberMarginRenderer());
			theme = new Theme();
			editorViewSettings = new EditorViewSettings();

			// Set up the caret, this must be done after the buffer is set.
			caret = new Caret(this);

			// Set up the text editor controller.
			controller = new TextController(this);
			wordSplitter = new EnglishWordSplitter();
			Clipboard = Clipboard.Get(Atom.Intern("CLIPBOARD", true));

			controller.BeginAction += OnBeginAction;
			controller.EndAction += OnEndAction;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="EditorView"/> class.
		/// </summary>
		/// <param name="raw">The raw.</param>
		protected EditorView(IntPtr raw)
			: base(raw)
		{
		}

		#endregion

		#region Properties

		private readonly DateTime createdTimestamp = DateTime.Now;

		private EditorViewSettings editorViewSettings;

		/// <summary>
		/// Gets or sets the settings for the text editor.
		/// </summary>
		/// <value>
		/// The text editor settings.
		/// </value>
		private EditorViewSettings EditorViewSettings
		{
			get { return editorViewSettings; }
			set { editorViewSettings = value ?? new EditorViewSettings(); }
		}

		#endregion

		#region Line Buffer

		/// <summary>
		/// Gets the line buffer associated with the editor.
		/// </summary>
		/// <value>The line buffer.</value>
		public LineBuffer LineBuffer
		{
			[DebuggerStepThrough]
			get { return Renderer == null ? null : Renderer.LineBuffer; }
		}

		/// <summary>
		/// Gets or sets the line layout buffer.
		/// </summary>
		/// <value>The line layout buffer.</value>
		public EditorViewRenderer Renderer { get; private set; }

		/// <summary>
		/// Clears the line buffer from the editor.
		/// </summary>
		public void ClearLineBuffer()
		{
			SetLineBuffer(null);
		}

		/// <summary>
		/// Clears the renderer from the editor.
		/// </summary>
		public void ClearRenderer()
		{
			SetRenderer(null);
		}

		/// <summary>
		/// Handles the various events and processing needed to update the view.
		/// </summary>
		private void HandleChangedLineBuffer()
		{
			if (LineBuffer != null)
			{
				// Reset the margins and force them to resize themselves.
				margins.Resize(this);

				// Readjust the scroll bars and scroll to the top.
				SetAdjustments();
				Caret.Position = new BufferPosition(0, 0);
				ScrollToCaret();

				// Reset the controller of any input states.
				controller.Reset();
			}
			else
			{
				// Just scroll to the bottom.
				if (verticalAdjustment != null)
				{
					verticalAdjustment.SetBounds(0, 0, 0, 0, 0);
				}
			}

			// Raise an event to indicate we changed our renderer.
			RaiseLineBufferChanged();

			// Queue a redraw of the entire text editor.
			RequestRedraw();
		}

		/// <summary>
		/// Occurs when the line buffer is cleared or replaced.
		/// </summary>
		public event EventHandler LineBufferChanged;

		/// <summary>
		/// Raises the text renderer changed event.
		/// </summary>
		protected virtual void RaiseLineBufferChanged()
		{
			var listeners = LineBufferChanged;

			if (listeners != null)
			{
				listeners(this, EventArgs.Empty);
			}
		}

		/// <summary>
		/// Sets the line buffer in the editor view.
		/// </summary>
		/// <param name="value">The value.</param>
		public void SetLineBuffer(LineBuffer value)
		{
			// If it is null, then clear out the buffer but don't create a
			// text renderer if we don't have to.
			if (value == null)
			{
				if (Renderer != null)
				{
					Renderer.ClearLineBuffer();
				}

				// Handle the update to the view.
				HandleChangedLineBuffer();

				return;
			}

			// If we don't have a text renderer, use a sane default which works
			// in most cases.
			if (Renderer == null)
			{
				// We don't have a renderer, so create one.
				var lineBufferRenderer = new LineBufferRenderer(this);
				Renderer = new CachedTextRenderer(this, lineBufferRenderer);
			}

			// Now set the line buffer and perform the preparation operations.
			Renderer.SetLineBuffer(value);
			HandleChangedLineBuffer();
		}

		/// <summary>
		/// Sets the text renderer.
		/// </summary>
		/// <param name="value">The new <see cref="EditorView"/>, which can be 
		/// <see langword="null"/>.
		/// <param>
		public void SetRenderer(EditorViewRenderer value)
		{
			// Detach events if we previously had a renderer.
			if (Renderer != null)
			{
				// Disconnect from the events.
				Renderer.LineChanged -= OnLineChanged;

				// Remove the line buffer first.
				ClearLineBuffer();
			}

			// Set the new buffer.
			Renderer = value;

			if (LineBuffer != null)
			{
				// Hook up to the events.
				Renderer.LineChanged += OnLineChanged;
			}

			// Set up the editor view for the changes made.
			HandleChangedLineBuffer();
		}

		#endregion

		#region Layout

		/// <summary>
		/// Sets the layout using the given block style.
		/// </summary>
		/// <param name="layout">The layout.</param>
		/// <param name="style">The style.</param>
		/// <param name="width">The width.</param>
		public void SetLayout(
			Layout layout,
			LineBlockStyle style,
			int width)
		{
			// Set the style elements.
			layout.Wrap = style.GetWrap();
			layout.Alignment = style.GetAlignment();
			layout.FontDescription = style.GetFontDescription();

			// Check to see if we are doing line wrapping and set the width,
			// minus the padding, margins, and borders.
			layout.Width = Units.FromPixels((int) Math.Ceiling(width - style.Width));
		}

		#endregion

		#region Display

		private readonly MarginRendererCollection margins;
		private Theme theme;
		private IWordSplitter wordSplitter;

		/// <summary>
		/// Gets the GTK style associated with this context.
		/// </summary>
		/// <value>The GTK style.</value>
		public Style GtkStyle
		{
			get { return Style; }
		}

		/// <summary>
		/// Gets or sets the theme.
		/// </summary>
		/// <value>The theme.</value>
		public Theme Theme
		{
			get { return theme; }
			set { theme = value ?? new Theme(); }
		}

		/// <summary>
		/// Gets or sets the word splitter.
		/// </summary>
		/// <value>The word splitter.</value>
		public IWordSplitter WordSplitter
		{
			get { return wordSplitter; }
			set { wordSplitter = value ?? new OffsetWordSplitter(); }
		}

		#endregion

		#region Editing

		private readonly Caret caret;
		private readonly TextController controller;

		/// <summary>
		/// Gets the caret used to indicate where the user is editing.
		/// </summary>
		/// <value>The caret.</value>
		public Caret Caret
		{
			get { return caret; }
		}

		/// <summary>
		/// Gets the clipboard associated with this editor.
		/// </summary>
		/// <value>The clipboard.</value>
		public Clipboard Clipboard { get; private set; }

		/// <summary>
		/// Gets the controller associated with this editor.
		/// </summary>
		/// <value>The controller.</value>
		public TextController Controller
		{
			get { return controller; }
		}

		/// <summary>
		/// Called when an action begins.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnBeginAction(
			object sender,
			EventArgs args)
		{
			requestedRedraw = false;
		}

		/// <summary>
		/// Called when the user presses a button.
		/// </summary>
		/// <param name="buttonEvent">The event.</param>
		/// <returns></returns>
		protected override bool OnButtonPressEvent(EventButton buttonEvent)
		{
			// Wrap the event in various objects and pass it into the controller.
			var point = new PointD(buttonEvent.X, buttonEvent.Y);

			return controller.HandleMousePress(
				point, buttonEvent.Button, buttonEvent.State);
		}

		/// <summary>
		/// Called when the user releases the button.
		/// </summary>
		/// <param name="buttonEvent">The @event.</param>
		/// <returns></returns>
		protected override bool OnButtonReleaseEvent(EventButton buttonEvent)
		{
			// Wrap the event in various objects and pass it into the controller.
			var point = new PointD(buttonEvent.X, buttonEvent.Y);

			return controller.HandleMouseRelease(
				point, buttonEvent.Button, buttonEvent.State);
		}

		/// <summary>
		/// Called when an action ends.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnEndAction(
			object sender,
			EventArgs args)
		{
			if (requestedRedraw)
			{
				QueueDraw();
			}
		}

		/// <summary>
		/// Called when a key is pressed.
		/// </summary>
		/// <param name="eventKey">The event key.</param>
		/// <returns></returns>
		protected override bool OnKeyPressEvent(EventKey eventKey)
		{
			// If we don't have a line buffer, don't do anything.
			if (LineBuffer == null)
			{
				return false;
			}

			// Decompose the key into its components.
			ModifierType modifier;
			Key key;
			GdkUtility.DecomposeKeys(eventKey, out key, out modifier);

			// Get the unicode character for this key.
			uint unicodeChar = Keyval.ToUnicode(eventKey.KeyValue);

			// Pass it on to the controller.
			return controller.HandleKeyPress(key, modifier, unicodeChar);
		}

		/// <summary>
		/// Called when the line changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The args.</param>
		private void OnLineChanged(
			object sender,
			LineChangedArgs args)
		{
			RequestRedraw();
		}

		/// <summary>
		/// Called when the mouse moves.
		/// </summary>
		/// <param name="motionEvent">The motion event.</param>
		/// <returns></returns>
		protected override bool OnMotionNotifyEvent(EventMotion motionEvent)
		{
			// Wrap the event in various objects and pass it into the controller.
			var point = new PointD(motionEvent.X, motionEvent.Y);

			return controller.HandleMouseMotion(point, motionEvent.State);
		}

		#endregion

		#region Rendering Events

		private bool requestedRedraw;

		/// <summary>
		/// Gets the width of the area that can be used for rendering text.
		/// </summary>
		/// <value>The width of the text.</value>
		public int TextWidth
		{
			get { return Allocation.Width - margins.Width; }
		}

		/// <summary>
		/// Gets the text X coordinate.
		/// </summary>
		/// <value>The text X.</value>
		public int TextX
		{
			get { return margins.Width; }
		}

		/// <summary>
		/// Called when the widget is exposed or drawn.
		/// </summary>
		/// <param name="e">The e.</param>
		/// <returns></returns>
		protected override bool OnExposeEvent(EventExpose e)
		{
			// Figure out the area we are rendering into.
			Gdk.Rectangle area = e.Region.Clipbox;
			var cairoArea = new Rectangle(area.X, area.Y, area.Width, area.Height);

			using (Context cairoContext = CairoHelper.Create(e.Window))
			{
				// Create a render context.
				var renderContext = new RenderContext(cairoContext);
				renderContext.RenderRegion = cairoArea;

				// If we don't have a buffer at this point, render the entire
				// area with the disabled background color and stop.
				if (Renderer == null)
				{
					// Paint the background color of the window.
					cairoContext.Color = theme.DisabledBackgroundColor;
					cairoContext.Rectangle(cairoArea);
					cairoContext.Fill();

					// We are done processing.
					return true;
				}

				// Paint the background color of the window.
				cairoContext.Color = theme.BackgroundColor;
				cairoContext.Rectangle(cairoArea);
				cairoContext.Fill();

				// Reset the layout and its properties.
				Renderer.Width = area.Width - margins.Width;

				// Figure out the viewport area we'll be drawing.
				int offsetY = 0;

				if (verticalAdjustment != null)
				{
					offsetY += (int) verticalAdjustment.Value;
				}

				var viewArea = new Rectangle(
					area.X, area.Y + offsetY, area.Width, area.Height);

				// Determine the line range visible in the given area.
				int startLine, endLine;
				Renderer.GetLineLayoutRange(viewArea, out startLine, out endLine);

				// Determine where the first line actually starts.
				int startLineY = 0;

				if (startLine > 0)
				{
					startLineY = Renderer.GetLineLayoutHeight(0, startLine - 1);
				}

				// Go through the lines and draw each one in the correct position.
				double currentY = startLineY - offsetY;

				for (int lineIndex = startLine; lineIndex <= endLine; lineIndex++)
				{
					// Pull out the layout and style since we'll use it.
					Layout layout = Renderer.GetLineLayout(lineIndex);
					LineBlockStyle style = Renderer.GetLineStyle(lineIndex);

					// Get the extents for that line.
					int layoutWidth, layoutHeight;
					layout.GetPixelSize(out layoutWidth, out layoutHeight);

					// Figure out the height of the line including padding.
					double height = layoutHeight + style.Height;

					// If this is the current line, then we draw an additional
					// background color if the theme requests it.
					if (lineIndex == caret.Position.LineIndex)
					{
						// If we have a full-line background color, display it.
						if (theme.CurrentLineBackgroundColor.HasValue)
						{
							var lineArea = new Rectangle(TextX, currentY, TextWidth, height);

							cairoContext.Color = theme.CurrentLineBackgroundColor.Value;
							cairoContext.Rectangle(lineArea);
							cairoContext.Fill();
						}

						// If we have a wrapped line background color, draw it.
						if (theme.CurrentWrappedLineBackgroundColor.HasValue)
						{
							// Get the wrapped line for the caret's position.
							LayoutLine wrappedLine = caret.Position.GetWrappedLine(this);
							Pango.Rectangle wrappedLineExtents;

							wrappedLine.GetPixelExtents(out wrappedLineExtents);

							//// Figure out where in the screen we'll be drawing this layout.
							//double wrappedY = currentY + logicalRegion.Y + style.Top;

							//Console.WriteLine(
							//    string.Format(
							//        "wy {0} cy {1} ly {2} iy {4} st {3}",
							//        wrappedY,
							//        currentY,
							//        logicalRegion.Y,
							//        style.Top,
							//        inkRegion.Y));

							// Draw the current wrapped line index.
							var wrappedLineArea = new Rectangle(
								TextX,
								currentY + wrappedLineExtents.Y + style.Top,
								TextWidth,
								wrappedLineExtents.Height);

							cairoContext.Color = theme.CurrentWrappedLineBackgroundColor.Value;
							cairoContext.Rectangle(wrappedLineArea);
							cairoContext.Fill();
						}
					}

					// Draw the current line along with wrapping and padding.
					DrawingUtility.DrawLayout(
						this,
						renderContext,
						new Rectangle(TextX, currentY, TextWidth, height),
						layout,
						style);

					// Render out the margin renderers.
					margins.Draw(
						this, renderContext, lineIndex, new PointD(0, currentY), height);

					// Move down a line.
					currentY += height;
				}

				// Draw the caret on the screen, but only if we have focus.
				if (IsFocus)
				{
					caret.Draw(renderContext);
				}

				// Show the scroll region, if requested.
				if (editorViewSettings.ShowScrollPadding)
				{
					cairoContext.Color = new Color(1, 0.5, 0.5);
					cairoContext.Rectangle(scrollPaddingRegion);
					cairoContext.Stroke();
				}
			}

			return true;
		}

		/// <summary>
		/// Requests a redraw of a specific area on the screen.
		/// </summary>
		public void RequestRedraw()
		{
			if (controller.InAction)
			{
				requestedRedraw = true;
			}
			else
			{
				QueueDraw();
			}
		}

		/// <summary>
		/// Requests a redraw of a specific area on the screen.
		/// </summary>
		/// <param name="region">The region.</param>
		public void RequestRedraw(Rectangle region)
		{
			RequestRedraw();
		}

		#endregion

		#region Scrollbars

		private Rectangle scrollPaddingRegion;
		private Adjustment verticalAdjustment;

		/// <summary>
		/// Gets or sets the vertical adjustment or offset into the viewing area.
		/// </summary>
		/// <value>The vertical adjustment.</value>
		public double BufferOffsetY
		{
			get { return verticalAdjustment.Value; }
		}

		/// <summary>
		/// Gets the vertical adjustment.
		/// </summary>
		/// <value>The vertical adjustment.</value>
		public Adjustment VerticalAdjustment
		{
			get { return verticalAdjustment; }
		}

		/// <summary>
		/// Called when the scroll adjustements are requested.
		/// </summary>
		/// <param name="hadj">The hadj.</param>
		/// <param name="vadj">The vadj.</param>
		protected override void OnSetScrollAdjustments(
			Adjustment hadj,
			Adjustment vadj)
		{
			// Determine if we need to remove ourselves from the previous adjustment
			// events.
			if (verticalAdjustment != null)
			{
				verticalAdjustment.ValueChanged -= OnVerticalAdjustment;
			}

			// And set the bounds based on our size.
			verticalAdjustment = vadj;

			SetAdjustments();

			// Add the events, if we have an adjustment.
			if (verticalAdjustment != null)
			{
				verticalAdjustment.ValueChanged += OnVerticalAdjustment;
			}
		}

		/// <summary>
		/// Called when the vertical adjustment is changed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnVerticalAdjustment(
			object sender,
			EventArgs args)
		{
			// Redraw the entire window.
			QueueDraw();
		}

		/// <summary>
		/// Scrolls the view to ensure the caret is visible.
		/// </summary>
		public void ScrollToCaret()
		{
			// If we don't have adjustments, don't do anything.
			if (verticalAdjustment == null)
			{
				return;
			}

			// Figure out if the caret is already in the visible area.
			Rectangle caretRegion = caret.GetDrawRegion();

			if (scrollPaddingRegion.Contains(caretRegion))
			{
				// We are already visible, so do nothing.
				return;
			}

			// Figure out what direction we have to scroll.
			if (caretRegion.Y < scrollPaddingRegion.Y)
			{
				// We have to scroll down. Start by figuring out the distance from the top
				// of the caret to the top of the scroll region.
				double difference = scrollPaddingRegion.Y - caretRegion.Y;

				verticalAdjustment.Value -= difference;
			}
			else
			{
				// We have to scroll up. Figure out the bottom of the caret in relation to
				// the bottom of the scrolling region.
				double caretBottom = caretRegion.Y + caretRegion.Height;
				double bottom = scrollPaddingRegion.Y + scrollPaddingRegion.Height;
				double difference = caretBottom - bottom;

				verticalAdjustment.Value += difference;
			}

			// Redraw the screen.
			RequestRedraw();
		}

		/// <summary>
		/// Used to set the adjustments on the scrollbars for the text editor
		/// </summary>
		private void SetAdjustments()
		{
			// We have to have a size and an adjustment.
			if (verticalAdjustment == null || Renderer == null)
			{
				return;
			}

			// If the text width is negative, then we can't format.
			if (TextWidth < 0)
			{
				return;
			}

			// Set the line buffer's width and then request the height for all
			// the lines in the buffer.
			Renderer.Width = TextWidth;
			int height = Renderer.GetLineLayoutHeight(0, Int32.MaxValue);

			// Set the adjustments based on those values.
			int lineHeight = Renderer.GetLineLayoutHeight();

			verticalAdjustment.SetBounds(
				0.0, height, lineHeight, (int) (Allocation.Height / 2.0), Allocation.Height);

			// Figure out the scroll padding.
			int scrollPaddingHeight = lineHeight * editorViewSettings.CaretScrollPad;

			scrollPaddingRegion = new Rectangle(
				0,
				scrollPaddingHeight,
				Allocation.Width,
				Allocation.Height - scrollPaddingHeight * 2);
		}

		#endregion

		#region Window Events

		/// <summary>
		/// Called when the window is realized (shown).
		/// </summary>
		protected override void OnRealized()
		{
			WidgetFlags |= WidgetFlags.Realized;
			var attributes = new WindowAttr();
			attributes.WindowType = WindowType.Child;
			attributes.X = Allocation.X;
			attributes.Y = Allocation.Y;
			attributes.Width = Allocation.Width;
			attributes.Height = Allocation.Height;
			attributes.Wclass = WindowClass.InputOutput;
			attributes.Visual = Visual;
			attributes.Colormap = Colormap;
			attributes.EventMask = (int) (Events | EventMask.ExposureMask);
			attributes.Mask = Events | EventMask.ExposureMask;

			const WindowAttributesType mask =
				WindowAttributesType.X | WindowAttributesType.Y |
				WindowAttributesType.Colormap | WindowAttributesType.Visual;
			GdkWindow = new Window(ParentWindow, attributes, mask);
			GdkWindow.UserData = Raw;
			Style = Style.Attach(GdkWindow);
			WidgetFlags &= ~WidgetFlags.NoWindow;
		}

		/// <summary>
		/// Called when the widget is resized.
		/// </summary>
		/// <param name="allocation">The allocation.</param>
		protected override void OnSizeAllocated(Gdk.Rectangle allocation)
		{
			// Call the base implementation.
			base.OnSizeAllocated(allocation);

			// If we have a GdkWindow, move it.
			if (GdkWindow != null)
			{
				GdkWindow.MoveResize(allocation);
			}

			if (Renderer != null)
			{
				// We need to reset the buffer so it can recalculate all the widths
				// and clear any caches.
				Renderer.Reset();

				// Change the adjustments (scrollbars).
				SetAdjustments();

				// Get rid of any input states.
				controller.Reset();
			}

			// Force the entire widget to draw.
			QueueDraw();
		}

		/// <summary>
		/// Called when the widget is unrealized (hidden).
		/// </summary>
		protected override void OnUnrealized()
		{
			if (GdkWindow != null)
			{
				GdkWindow.UserData = IntPtr.Zero;
				GdkWindow.Destroy();
				WidgetFlags |= WidgetFlags.NoWindow;
			}

			base.OnUnrealized();
		}

		#endregion
	}
}