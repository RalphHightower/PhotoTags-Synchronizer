﻿#region BSD License
/*
 * 
 * Original BSD 3-Clause License (https://github.com/ComponentFactory/Krypton/blob/master/LICENSE)
 *  © Component Factory Pty Ltd, 2006 - 2016, All rights reserved.
 * 
 *  New BSD 3-Clause License (https://github.com/Krypton-Suite/Standard-Toolkit/blob/master/LICENSE)
 *  Modifications by Peter Wagner(aka Wagnerp) & Simon Coghlan(aka Smurf-IV), et al. 2017 - 2021. All rights reserved. 
 *  
 *  Modified: Monday 12th April, 2021 @ 18:00 GMT
 *
 */
#endregion

using System;

namespace Krypton.Docking
{
    /// <summary>
    /// Event arguments for a DockableWorkspaceRemoved event.
    /// </summary>
    public class DockableWorkspaceEventArgs : EventArgs
    {
        #region Instance Fields

        #endregion

        #region Identity
        /// <summary>
        /// Initialize a new instance of the DockableWorkspaceEventArgs class.
        /// </summary>
        /// <param name="workspace">Reference to dockable workspace control instance.</param>
        /// <param name="element">Reference to docking workspace element that is managing the dockable workspace control.</param>
        public DockableWorkspaceEventArgs(KryptonDockableWorkspace workspace,
                                          KryptonDockingWorkspace element)
        {
            DockableWorkspaceControl = workspace;
            DockingWorkspaceElement = element;
        }
        #endregion

        #region Public
        /// <summary>
        /// Gets a reference to the KryptonDockableWorkspace control.
        /// </summary>
        public KryptonDockableWorkspace DockableWorkspaceControl { get; }

        /// <summary>
        /// Gets a reference to the KryptonDockingWorkspace that is managing the dockable workspace control.
        /// </summary>
        public KryptonDockingWorkspace DockingWorkspaceElement { get; }

        #endregion
    }
}