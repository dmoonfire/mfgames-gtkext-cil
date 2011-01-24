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

using Pango;

#endregion

namespace MfGames.GtkExt.Extensions.Pango
{
	/// <summary>
	/// Defines extensions to the Pango.LayoutLine class.
	/// </summary>
	public static class PangoLayoutLineExtensions
	{
		/// <summary>
		/// Determines whether this layout line is the last line in the layout.
		/// </summary>
		/// <param name="layoutLine">The layout line.</param>
		/// <returns>
		/// 	<c>true</c> if [is last line] [the specified layout line]; otherwise, <c>false</c>.
		/// </returns>
		public static bool IsLastLineInLayout(this LayoutLine layoutLine)
		{
			Layout layout = layoutLine.Layout;
			return layout.Lines[layout.LineCount - 1].StartIndex == layoutLine.StartIndex;
		}
	}
}