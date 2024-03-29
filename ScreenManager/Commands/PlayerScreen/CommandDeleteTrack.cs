/*
Copyright � Joan Charmant 2008.
joan.charmant@gmail.com 
 
This file is part of Kinovea.

Kinovea is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License version 2 
as published by the Free Software Foundation.

Kinovea is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with Kinovea. If not, see http://www.gnu.org/licenses/.

*/

using Kinovea.ScreenManager.Languages;
using System;
using System.Reflection;
using System.Resources;
using System.Threading;
using Kinovea.Services;

namespace Kinovea.ScreenManager
{
    public class CommandDeleteTrack : IUndoableCommand
    {

        public string FriendlyName
        {
        	get { return ScreenManagerLang.mnuDeleteTrajectory; }
        }

        private PlayerScreenUserInterface m_psui;
        private Metadata m_Metadata;
        private int m_iTotalTracks;
        private Track m_Track;
        private int m_iTrackIndex;

        #region constructor
        public CommandDeleteTrack(PlayerScreenUserInterface _psui, Metadata _Metadata)
        {
            m_psui = _psui;
            m_Metadata = _Metadata;
            m_iTrackIndex = m_Metadata.SelectedTrack;
            m_iTotalTracks = m_Metadata.Tracks.Count;
            m_Track = m_Metadata.Tracks[m_iTrackIndex];
        }
        #endregion

        /// <summary>
        /// Execution de la commande
        /// </summary>
        public void Execute()
        {
            // It should work because all add/delete actions modify the undo stack.
            // When we come back here for a redo, we should be in the exact same state
            // as the first time.
            // Even if drawings were added in between, we can't come back here
            // before all those new drawings have been unstacked from the m_CommandStack stack.

            m_Metadata.Tracks.RemoveAt(m_iTrackIndex);
            m_Metadata.SelectedTrack = -1;
            m_psui.pbSurfaceScreen.Invalidate();
        }
        public void Unexecute()
        {
            // Recreate the drawing.

            // 1. Look for the keyframe
            // We must insert exactly where we deleted, otherwise the drawing table gets messed up.
            // We must still be able to undo any Add action that where performed before.
            m_Metadata.Tracks.Insert(m_iTrackIndex, m_Track);
            m_psui.pbSurfaceScreen.Invalidate();
        }
    }
}


