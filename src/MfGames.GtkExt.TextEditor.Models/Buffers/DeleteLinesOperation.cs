// Copyright 2011-2013 Moonfire Games
// Released under the MIT license
// http://mfgames.com/mfgames-gtkext-cil/license

namespace MfGames.GtkExt.TextEditor.Models.Buffers
{
	/// <summary>
	/// Indicates an operation that inserts lines into a line buffer.
	/// </summary>
	public class DeleteLinesOperation: ILineBufferOperation
	{
		#region Properties

		/// <summary>
		/// Gets the number of lines to delete.
		/// </summary>
		/// <value>The count.</value>
		public int Count { get; private set; }

		/// <summary>
		/// Gets the index of the first line to start deleting.
		/// </summary>
		/// <value>The index of the line.</value>
		public int LineIndex { get; private set; }

		/// <summary>
		/// Gets the type of the operation representing this object.
		/// </summary>
		/// <value>The type of the operation.</value>
		public LineBufferOperationType OperationType
		{
			get { return LineBufferOperationType.DeleteLines; }
		}

		#endregion

		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="DeleteLinesOperation"/> class.
		/// </summary>
		/// <param name="lineIndex">Index of the line.</param>
		/// <param name="count">The count.</param>
		public DeleteLinesOperation(
			int lineIndex,
			int count)
		{
			LineIndex = lineIndex;
			Count = count;
		}

		#endregion
	}
}
