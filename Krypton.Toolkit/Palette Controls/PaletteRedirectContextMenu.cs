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

using System.Drawing;
using System.Diagnostics;

namespace Krypton.Toolkit
{
    /// <summary>
    /// Redirects requests for context menu images from the ContextMenuImages instance.
    /// </summary>
    public class PaletteRedirectContextMenu : PaletteRedirect
    {
        #region Instance Fields
        private readonly ContextMenuImages _images;
        #endregion

        #region Identity
        /// <summary>
        /// Initialize a new instance of the PaletteRedirectContextMenu class.
        /// </summary>
        /// <param name="target">Initial palette target for redirection.</param>
        /// <param name="images">Reference to source of context menu images.</param>
        public PaletteRedirectContextMenu(IPalette target,
                                          ContextMenuImages images)
            : base(target)
        {
            Debug.Assert(images != null);

            // Remember incoming target
            _images = images;
        }
        #endregion

        #region Images
        /// <summary>
        /// Gets a checked image appropriate for a context menu item.
        /// </summary>
        /// <returns>Appropriate image for drawing; otherwise null.</returns>
        public override Image GetContextMenuCheckedImage()
        {
            Image retImage = _images.Checked ?? Target.GetContextMenuCheckedImage();

            // Not found, then inherit from target

            return retImage;
        }

        /// <summary>
        /// Gets a indeterminate image appropriate for a context menu item.
        /// </summary>
        /// <returns>Appropriate image for drawing; otherwise null.</returns>
        public override Image GetContextMenuIndeterminateImage()
        {
            Image retImage = _images.Indeterminate ?? Target.GetContextMenuIndeterminateImage();

            // Not found, then inherit from target

            return retImage;
        }

        /// <summary>
        /// Gets an image indicating a sub-menu on a context menu item.
        /// </summary>
        /// <returns>Appropriate image for drawing; otherwise null.</returns>
        public override Image GetContextMenuSubMenuImage()
        {
            Image retImage = _images.SubMenu ?? Target.GetContextMenuSubMenuImage();

            // Not found, then inherit from target

            return retImage;
        }
        #endregion
    }
}