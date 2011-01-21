using System;
using System.Collections.Generic;

using C5;

using MfGames.GtkExt.LineTextEditor.Interfaces;

namespace MfGames.GtkExt.LineTextEditor.Actions
{
    /// <summary>
    /// Contains a collection of action states that can be conditionally
    /// removed with various states removed.
    /// </summary>
    public class ActionStateCollection : ArrayList<IActionState>
    {
        /// <summary>
        /// Gets an action state inside the collection or null.
        /// </summary>
        /// <typeparam name="TActionState">The type of the action state.</typeparam>
        /// <returns></returns>
        public TActionState Get<TActionState>()
            where TActionState: class, IActionState
        {
            foreach (IActionState state in this)
            {
                if (state is TActionState)
                {
                    return (TActionState) state;
                }
            }

            return null;
        }

		/// <summary>
		/// Removes all the elements inside the array except for those in the
		/// list.
		/// </summary>
		/// <param name="excludeTypes">The types not to remove.</param>
    	public void RemoveAllExcluding(IEnumerable<Type> excludeTypes)
    	{
			// Go through a list and build up a list of things to remove.
			ArrayList<IActionState> removeStates = new ArrayList<IActionState>();

			foreach (IActionState state in this)
			{
				// First check to see if this state is on the exclude list.
				bool found = false;

				foreach (Type type in excludeTypes)
				{
					if (type.IsAssignableFrom(state.GetType()))
					{
						found = true;
						break;
					}
				}

				if (found)
				{
					continue;
				}

				// Check to see if this action is willing to let itself be
				// removed.
				if (state.CanRemove())
				{
					removeStates.Add(state);
				}
			}

			// Go through the remove list and remove them.
			foreach (IActionState state in removeStates)
			{
				Remove(state);
			}
    	}
    }
}