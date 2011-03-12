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
using System.Collections.Generic;

using C5;

using MfGames.GtkExt.LineTextEditor.Buffers;
using MfGames.GtkExt.LineTextEditor.Interfaces;

#endregion

namespace GtkExtDemo.LineTextEditor
{
	/// <summary>
	/// Wraps around a line buffer and marks up anything with a number of keywords
	/// with Pango markup.
	/// </summary>
	public class KeywordLineBuffer : LineBufferDecorator
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="KeywordLineBuffer"/> class.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		public KeywordLineBuffer(LineBuffer buffer)
			: base(buffer)
		{
		}

		#endregion

		#region Markup

		/// <summary>
		/// Gets the Pango markup for a given line.
		/// </summary>
		/// <param name="lineIndex">The line.</param>
		/// <returns></returns>
		public override string GetLineMarkup(int lineIndex)
		{
			// Get the escaped line markup.
			string markup = base.GetLineMarkup(lineIndex);

			// Parse through the markup and get a list of entries. We go through
			// the list in reverse so we can use the character entries without
			// adjusting for the text we're adding.
			ArrayList<KeywordMarkupEntry> entries = KeywordMarkupEntry.ParseText(markup);

			entries.Reverse();

			foreach (var entry in entries)
			{
				// Insert the final span at the end.
				markup = markup.Insert(entry.EndCharacterIndex, "</span>");

				// Figure out the attributes.
				string attributes = string.Empty;

				switch (entry.Markup)
				{
					case KeywordMarkupType.Error:
						attributes = "underline='error' underline_color='red' color='red'";
						break;

					case KeywordMarkupType.Warning:
						attributes = "underline='error' underline_color='orange' color='orange'";
						break;
				}

				// Add in the attributes for the start index.
				markup = markup.Insert(
					entry.StartCharacterIndex, "<span " + attributes + ">");
			}

			// Return the resulting markup.
			return markup;
		}

		#endregion

		#region Line Indicators

		/// <summary>
		/// Gets the line indicators for a given character range.
		/// </summary>
		/// <param name="lineIndex">Index of the line.</param>
		/// <param name="startCharacterIndex">Start character in the line text.</param>
		/// <param name="endCharacterIndex">End character in the line text.</param>
		/// <returns>
		/// An enumerable with the indicators or <see langword="null"/> for
		/// none.
		/// </returns>
		public override IEnumerable<ILineIndicator> GetLineIndicators(
			int lineIndex,
			int startCharacterIndex,
			int endCharacterIndex)
		{
			// Get the escaped line markup.
			string text = GetLineText(lineIndex);

			endCharacterIndex = Math.Min(endCharacterIndex, text.Length);

			string partialText = text.Substring(
				startCharacterIndex, endCharacterIndex - startCharacterIndex);

			// Parse through the markup and get a list of entries. If we don't
			// get any out of this, return null.
			ArrayList<KeywordMarkupEntry> entries =
				KeywordMarkupEntry.ParseText(partialText);

			if (entries.Count == 0)
			{
				return null;
			}

			// Return the list of keyword entries which are also indicators.
			var indicators = new ArrayList<ILineIndicator>(entries.Count);

			indicators.AddAll(entries);

			return indicators;
		}

		#endregion
	}
}