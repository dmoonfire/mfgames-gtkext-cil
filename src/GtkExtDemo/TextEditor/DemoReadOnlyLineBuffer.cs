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
using System.Text;

using MfGames.GtkExt.TextEditor.Models;
using MfGames.GtkExt.TextEditor.Models.Buffers;

#endregion

namespace GtkExtDemo.TextEditor
{
	/// <summary>
	/// Implements a read only buffer that populates data with text similiar
	/// to the "ipso lorem" text.
	/// </summary>
	public class DemoReadOnlyLineBuffer : LineBuffer
	{
		#region Constuctors

		static DemoReadOnlyLineBuffer()
		{
			// Build up the string so it fits the formatting of the source file.
			var b = new StringBuilder();

			b.Append("lorem ipsum dolor sit amet consetetur sadipscing elitr ");
			b.Append("sed diam nonumy eirmod tempor invidunt ut labore et dolore ");
			b.Append("magna aliquyam erat sed diam voluptua at vero eos et accusam ");
			b.Append("et justo duo dolores et ea rebum stet clita kasd gubergren no ");
			b.Append("sea takimata sanctus est lorem ipsum dolor sit amet lorem ");
			b.Append("ipsum dolor sit amet consetetur sadipscing elitr sed diam ");
			b.Append("nonumy eirmod tempor invidunt ut labore et dolore magna aliquyam ");
			b.Append("erat sed diam voluptua at vero eos et accusam et justo duo ");
			b.Append("dolores et ea rebum stet clita kasd gubergren no sea takimata ");
			b.Append("sanctus est lorem ipsum dolor sit amet lorem ipsum dolor sit ");
			b.Append("amet consetetur sadipscing elitr sed diam nonumy eirmod tempor ");
			b.Append(
				"invidunt ut labore et dolore magna aliquyam erat sed diam voluptua ");
			b.Append("at vero eos et accusam et justo duo dolores et ea rebum stet ");
			b.Append("clita kasd gubergren no sea takimata sanctus est lorem ipsum ");
			b.Append("dolor sit amet duis autem vel eum iriure dolor in hendrerit in ");
			b.Append("vulputate velit esse molestie consequat vel illum dolore eu ");
			b.Append("feugiat nulla facilisis at vero eros et accumsan et iusto ");
			b.Append("odio dignissim qui blandit praesent luptatum zzril delenit augue ");
			b.Append("duis dolore te feugait nulla facilisi lorem ipsum dolor sit ");
			b.Append("amet consectetuer adipiscing elit sed diam nonummy nibh euismod ");
			b.Append("tincidunt ut laoreet dolore magna aliquam erat volutpat ut wisi ");
			b.Append("enim ad minim veniam quis nostrud exerci tation ullamcorper ");
			b.Append("suscipit lobortis nisl ut aliquip ex ea commodo consequat duis ");
			b.Append("autem vel eum iriure dolor in hendrerit in vulputate velit esse ");
			b.Append(
				"molestie consequat vel illum dolore eu feugiat nulla facilisis at ");
			b.Append(
				"vero eros et accumsan et iusto odio dignissim qui blandit praesent ");
			b.Append(
				"luptatum zzril delenit augue duis dolore te feugait nulla facilisi ");
			b.Append(
				"nam liber tempor cum soluta nobis eleifend option congue nihil imperdiet ");
			b.Append(
				"doming id quod mazim placerat facer possim assum lorem ipsum dolor ");
			b.Append("sit amet consectetuer adipiscing elit sed diam nonummy nibh ");
			b.Append(
				"euismod tincidunt ut laoreet dolore magna aliquam erat volutpat ut ");
			b.Append("wisi enim ad minim veniam quis nostrud exerci tation ullamcorper");

			// Split on the space and keep it in an array.
			words = b.ToString().Split(' ');
		}

		#endregion

		/// <summary>
		/// Gets the number of lines in the buffer.
		/// </summary>
		/// <value>The line count.</value>
		public override int LineCount
		{
			get { return 1000; }
		}

		/// <summary>
		/// If set to <see langword="true"/>, the buffer is read-only and the editing
		/// commands should throw an <see cref="InvalidOperationException"/>.
		/// </summary>
		public override bool ReadOnly
		{
			get { return true; }
		}

		/// <summary>
		/// Performs the given operation, raising any events for changing.
		/// </summary>
		/// <param name="operation">The operation.</param>
		public override void Do(ILineBufferOperation operation)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Gets the length of the line.
		/// </summary>
		/// <param name="lineIndex">The line index in the buffer.</param>
		/// <returns>The length of the line.</returns>
		public override int GetLineLength(
			int lineIndex,
			LineContexts lineContexts)
		{
			return GenerateText(lineIndex).Length;
		}

		/// <summary>
		/// Gets the formatted line number for a given line.
		/// </summary>
		/// <param name="lineIndex">The line index in the buffer.</param>
		/// <returns>A formatted line number.</returns>
		public override string GetLineNumber(int lineIndex)
		{
			return (lineIndex + 1).ToString();
		}

		/// <summary>
		/// Gets the text of a given line in the buffer.
		/// </summary>
		/// <param name="lineIndex">The line index in the buffer. If the index is beyond the end of the buffer, the last line is used.</param>
		/// <param name="characters">The character range to pull the text.</param>
		/// <returns></returns>
		public override string GetLineText(
			int lineIndex,
			CharacterRange characters,
			LineContexts lineContexts)
		{
			return characters.Substring(GenerateText(lineIndex));
		}

		#region Generation

		private static readonly string[] words;

		/// <summary>
		/// Generates random text using the line index as a seed.
		/// </summary>
		/// <param name="lineIndex">Index of the line.</param>
		/// <returns></returns>
		private static string GenerateText(int lineIndex)
		{
			// Get the random and determine how many words we'll be selecting.
			var random = new Random(lineIndex + 1);

			int start = random.Next(words.Length);
			int wordCount = random.Next(10, 50);

			// Create the text by selecting the given number of words and
			// appending them to a buffer.
			var buffer = new StringBuilder();

			for (int i = 0; i < wordCount; i++)
			{
				if (i > 0)
				{
					buffer.Append(" ");
				}

				buffer.Append(words[(i + start) % words.Length]);
			}

			// Return the resulting string.
			return buffer.ToString();
		}

		#endregion
	}
}