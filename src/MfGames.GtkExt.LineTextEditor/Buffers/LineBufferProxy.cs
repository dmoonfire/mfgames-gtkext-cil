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

using MfGames.GtkExt.LineTextEditor.Interfaces;

#endregion

namespace MfGames.GtkExt.LineTextEditor.Buffers
{
	/// <summary>
	/// Implements a proxy which contains a line buffer and passes all calls
	/// into the underlying buffer.
	/// </summary>
	public class LineBufferProxy
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="LineBufferProxy"/> class.
		/// </summary>
		/// <param name="buffer">The buffer.</param>
		public LineBufferProxy(ILineBuffer buffer)
		{
			if (buffer == null)
			{
				throw new ArgumentNullException("buffer");
			}

			this.buffer = buffer;
		}

		#endregion

		#region Buffer

		private readonly ILineBuffer buffer;

		/// <summary>
		/// Gets the underlying buffer for this proxy.
		/// </summary>
		/// <value>The buffer.</value>
		protected ILineBuffer LineBuffer
		{
			get { return buffer; }
		}

		#endregion

		#region Buffer Viewing

		public virtual int LineCount
		{
			get { return buffer.LineCount; }
		}

		/// <summary>
		/// If set to true, the buffer is read-only and the editing commands
		/// should throw an InvalidOperationException.
		/// </summary>
		public virtual bool ReadOnly
		{
			get { return buffer.ReadOnly; }
		}

		/// <summary>
		/// Gets the length of the line.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <returns></returns>
		public virtual int GetLineLength(int line)
		{
			return buffer.GetLineLength(line);
		}

		/// <summary>
		/// Gets the line number.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <returns></returns>
		public virtual string GetLineNumber(int line)
		{
			return buffer.GetLineNumber(line);
		}

		/// <summary>
		/// Gets the line text.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <param name="startIndex">The start index.</param>
		/// <param name="endIndex">The end index.</param>
		/// <returns></returns>
		public virtual string GetLineText(
			int line,
			int startIndex,
			int endIndex)
		{
			return buffer.GetLineText(line, startIndex, endIndex);
		}

		#endregion

		#region Buffer Editing

		/// <summary>
		/// Deletes the lines.
		/// </summary>
		/// <param name="startLine">The start line.</param>
		/// <param name="endLine">The end line.</param>
		public virtual void DeleteLines(
			int startLine,
			int endLine)
		{
			buffer.DeleteLines(startLine, endLine);
		}

		/// <summary>
		/// Inserts the lines.
		/// </summary>
		/// <param name="afterLine">The after line.</param>
		/// <param name="count">The count.</param>
		public virtual void InsertLines(
			int afterLine,
			int count)
		{
			buffer.InsertLines(afterLine, count);
		}

		/// <summary>
		/// Sets the line text.
		/// </summary>
		/// <param name="line">The line.</param>
		/// <param name="startIndex">The start index.</param>
		/// <param name="endIndex">The end index.</param>
		/// <param name="text">The text.</param>
		public virtual void SetLineText(
			int line,
			int startIndex,
			int endIndex,
			string text)
		{
			buffer.SetLineText(line, startIndex, endIndex, text);
		}

		#endregion
	}
}