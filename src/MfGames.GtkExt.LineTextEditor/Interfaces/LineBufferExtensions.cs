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

namespace MfGames.GtkExt.LineTextEditor.Interfaces
{
	/// <summary>
	/// Contains extensions used with the ILineBuffer.
	/// </summary>
	public static class LineBufferExtensions
	{
		/// <summary>
		/// Gets the text for a specific line.
		/// </summary>
		/// <param name="lineBuffer">The line buffer.</param>
		/// <param name="lineIndex">Index of the line.</param>
		/// <returns></returns>
		public static string GetLineText(
			this ILineBuffer lineBuffer,
			int lineIndex)
		{
			return lineBuffer.GetLineText(lineIndex, 0, -1);
		}
	}
}