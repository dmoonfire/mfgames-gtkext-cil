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

using Cairo;

using Gtk;

using MfGames.GtkExt;
using MfGames.GtkExt.TextEditor;
using MfGames.GtkExt.TextEditor.Editing;
using MfGames.GtkExt.TextEditor.Events;
using MfGames.GtkExt.TextEditor.Models;
using MfGames.GtkExt.TextEditor.Models.Buffers;
using MfGames.GtkExt.TextEditor.Models.Styles;
using MfGames.GtkExt.TextEditor.Renderers;
using MfGames.GtkExt.TextEditor.Renderers.Cache;

#endregion

namespace GtkExtDemo.TextEditor
{
	/// <summary>
	/// Contains the basic control for showing off the features of the line
	/// text editor.
	/// </summary>
	public class DemoTextEditor : DemoTab
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DemoTextEditor"/> class.
		/// </summary>
		public DemoTextEditor()
		{
			// Create the text editor with the resulting buffer.
			editorView = new EditorView();
			editorView.Controller.PopulateContextMenu += OnPopulateContextMenu;

			// Update the theme with some additional colors.
			Theme theme = editorView.Theme;

			theme.IndicatorStyles["Error"] = new IndicatorStyle(
				"Error", 100, new Color(1, 0, 0));
			theme.IndicatorStyles["Warning"] = new IndicatorStyle(
				"Warning", 10, new Color(1, 165 / 255.0, 0));
			theme.IndicatorRenderStyle = IndicatorRenderStyle.Ratio;
			theme.IndicatorPixelHeight = 2;
			theme.IndicatorRatioPixelGap = 1;

			// Wrap the text editor in a scrollbar.
			var scrolledWindow = new ScrolledWindow();
			scrolledWindow.VscrollbarPolicy = PolicyType.Always;
			scrolledWindow.Add(editorView);

			// Create the indicator bar that is 10 px wide.
			indicatorView = new IndicatorView(editorView);
			indicatorView.SetSizeRequest(20, 1);

			// Create the drop down list with the enumerations.
			var lineStyleCombo = new EnumComboBox(typeof(DemoLineStyleType));
			lineStyleCombo.Sensitive = false;

			// Add the editor and bar to the current tab.
			var editorBand = new HBox(false, 0);
			editorBand.PackStart(scrolledWindow, true, true, 0);
			editorBand.PackStart(indicatorView, false, false, 4);

			// Controls band
			var controlsBand = new HBox(false, 0);
			controlsBand.PackStart(lineStyleCombo, false, false, 0);
			controlsBand.PackStart(new Label(), true, true, 0);

			// Create a vbox and use it to add the combo boxes.
			var verticalLayout = new VBox(false, 4);
			verticalLayout.BorderWidth = 4;
			verticalLayout.PackStart(editorBand, true, true, 0);
			verticalLayout.PackStart(controlsBand, false, false, 4);

			// Add the editor and the controls into a vertical box.
			PackStart(verticalLayout, true, true, 2);
		}

		#endregion

		#region Buffers

		/// <summary>
		/// Creates the editable line buffer.
		/// </summary>
		/// <returns></returns>
		private LineBuffer CreateEditableLineBuffer()
		{
			// Create a patterned line buffer and make it read-write.
			var patternLineBuffer = new PatternLineBuffer(1024, 256, 4);
			var lineBuffer = new MemoryLineBuffer(patternLineBuffer);

			// Decorate the line buffer with something that will highlight the
			// error and warning keywords.
			var keywordBuffer = new KeywordLineBuffer(lineBuffer);

			// Return the resulting buffer.
			return keywordBuffer;
		}

		#endregion

		#region Widgets

		private readonly EditorView editorView;
		private readonly IndicatorView indicatorView;

		#region Events

		/// <summary>
		/// Called when the No Buffer button is clicked.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnClearBuffer(
			object sender,
			EventArgs e)
		{
			// Clear the buffer.
			editorView.SetRenderer(null);

			// Set the menu item toggle states.
			SetBufferMenuStates(false, false, true);
		}

		/// <summary>
		/// Called when the Editable Buffer button is clicked.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnEditableBufferActivated(
			object sender,
			EventArgs e)
		{
			// Create the buffer and set it.
			editorView.SetLineBuffer(CreateEditableLineBuffer());

			// Set the menu item toggle states.
			SetBufferMenuStates(true, false, false);
		}

		/// <summary>
		/// Called when the Read-Only Buffer button is clicked.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnReadOnlyBufferActivated(
			object sender,
			EventArgs e)
		{
			// Create the buffer and set it.
			editorView.SetLineBuffer(CreateEditableLineBuffer());

			// Set the menu item toggle states.
			SetBufferMenuStates(false, true, false);
		}

		#endregion

		#region Menus

		private void SetBufferMenuStates(
			bool checkEditable,
			bool checkReadOnly,
			bool checkClear)
		{
			// Remove the events from the items to avoid callbacks on the items.
			editableBufferMenuItem.Activated -= OnEditableBufferActivated;
			readOnlyBufferMenuItem.Activated -= OnReadOnlyBufferActivated;
			clearBufferMenuItem.Activated -= OnClearBuffer;

			// Check the boxes.
			editableBufferMenuItem.Active = checkEditable;
			readOnlyBufferMenuItem.Active = checkReadOnly;
			clearBufferMenuItem.Active = checkClear;

			// Add the events back in.
			editableBufferMenuItem.Activated += OnEditableBufferActivated;
			readOnlyBufferMenuItem.Activated += OnReadOnlyBufferActivated;
			clearBufferMenuItem.Activated += OnClearBuffer;
		}

		#endregion

		#region Setup

		private CheckMenuItem clearBufferMenuItem;
		private CheckMenuItem editableBufferMenuItem;
		private CheckMenuItem readOnlyBufferMenuItem;

		/// <summary>
		/// Configures the GUI and allows a demo to add menu and widgets.
		/// </summary>
		/// <param name="demo">The demo.</param>
		/// <param name="uiManager">The UI manager.</param>
		public override void ConfigureGui(
			Demo demo,
			UIManager uiManager)
		{
			// Get the menu and manually add the items.
			var menubar = (MenuBar) uiManager.GetWidget("/MenuBar");

			var menu = new Menu();

			var menuItem = new MenuItem("_Text Editor");
			menuItem.Submenu = menu;

			menubar.Append(menuItem);

			// Create the styled buffers.
			editableBufferMenuItem = new CheckMenuItem("_Editable Buffer");
			editableBufferMenuItem.DrawAsRadio = true;
			editableBufferMenuItem.Activated += OnEditableBufferActivated;
			menu.Append(editableBufferMenuItem);

			readOnlyBufferMenuItem = new CheckMenuItem("_Read-Only Buffer");
			readOnlyBufferMenuItem.DrawAsRadio = true;
			readOnlyBufferMenuItem.Activated += OnReadOnlyBufferActivated;
			menu.Append(readOnlyBufferMenuItem);

			clearBufferMenuItem = new CheckMenuItem("_Clear");
			clearBufferMenuItem.DrawAsRadio = true;
			clearBufferMenuItem.Activated += OnClearBuffer;
			menu.Append(clearBufferMenuItem);

			// Call the first callback.
			OnEditableBufferActivated(this, EventArgs.Empty);
		}

		#endregion

		#endregion

		#region Context Menu

		/// <summary>
		/// Called when the context menu is being populated.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The event arguments.</param>
		private void OnPopulateContextMenu(
			object sender,
			PopulateContextMenuArgs args)
		{
			// Add a separator and our custom "function".
			var menuItem = new MenuItem("Reverse Line");
			menuItem.Activated += OnReverseLine;

			args.Menu.Add(new SeparatorMenuItem());
			args.Menu.Add(menuItem);
		}

		/// <summary>
		/// Called when the line is requested to be reversed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="e">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnReverseLine(
			object sender,
			EventArgs e)
		{
			// Go through all the lines in the selection or if there is no
			// selection, then just the current line.
			Caret caret = editorView.Caret;
			var command = new Command(caret.Position);

			if (caret.Selection.IsEmpty || caret.Selection.IsSameLine)
			{
				ReverseLine(command, caret.Position.LineIndex);
			}
			else
			{
				for (int lineIndex = caret.Selection.StartPosition.LineIndex;
				     lineIndex <= caret.Selection.EndPosition.LineIndex;
				     lineIndex++)
				{
					ReverseLine(command, lineIndex);
				}
			}

			// Perform the command.
			editorView.Controller.Do(command);
		}

		/// <summary>
		/// Reverses the text of a line then adds it to the command.
		/// </summary>
		/// <param name="command">The command.</param>
		/// <param name="lineIndex">Index of the line.</param>
		private void ReverseLine(
			Command command,
			int lineIndex)
		{
			// Get the original line text.
			string lineText = editorView.LineBuffer.GetLineText(lineIndex);

			// Create a reverse of the text.
			var characters = new ArrayList<char>();

			characters.AddAll(lineText);
			characters.Reverse();

			var reverseText = new string(characters.ToArray());

			// Add the operations to the command.
			command.Operations.Add(new SetTextOperation(lineIndex, reverseText));

			command.UndoOperations.Add(new SetTextOperation(lineIndex, lineText));
		}

		#endregion
	}
}