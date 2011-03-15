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

using MfGames.GtkExt.LineTextEditor.Buffers;
using MfGames.GtkExt.LineTextEditor.Interfaces;

#endregion

namespace MfGames.GtkExt.LineTextEditor.Renderers
{
	/// <summary>
	/// Implements a <see cref="TextRenderer"/> wrapped around a 
	/// <see cref="LineBuffer"/>.
	/// </summary>
	public class LineBufferTextRenderer : TextRenderer
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="TextRenderer"/> class.
		/// </summary>
		/// <param name="displayContext">The display context.</param>
		/// <param name="lineBuffer">The line buffer.</param>
		public LineBufferTextRenderer(
			IDisplayContext displayContext,
			LineBuffer lineBuffer)
			: base(displayContext)
		{
			// Perform sanity checking on parameters.
			if (lineBuffer == null)
			{
				throw new ArgumentNullException("lineBuffer");
			}

			// Save the buffer in a property.
			this.lineBuffer = lineBuffer;

			// Hook up the events for the buffer.
			lineBuffer.LineChanged += OnLineChanged;
			lineBuffer.LinesInserted += OnLinesInserted;
			lineBuffer.LinesDeleted += OnLinesDeleted;
		}

		#endregion

		#region Buffer

		private readonly LineBuffer lineBuffer;

		/// <summary>
		/// Gets the line buffer associated with this renderer.
		/// </summary>
		/// <value>The line buffer.</value>
		public override LineBuffer LineBuffer
		{
			[DebuggerStepThrough]
			get { return lineBuffer; }
		}

		#endregion
	}
}