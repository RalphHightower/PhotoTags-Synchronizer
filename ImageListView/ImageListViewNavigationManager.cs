﻿// ImageListView - A listview control for image files
// Copyright (C) 2009 Ozgur Ozcitak
//
// Licensed under the Apache License, Version 2.0 (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
//     http://www.apache.org/licenses/LICENSE-2.0
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
//
// Ozgur Ozcitak (ozcitak@yahoo.com)

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Collections.Specialized;
using System.ServiceModel;

namespace Manina.Windows.Forms
{
    public partial class ImageListView
    {
        /// <summary>
        /// Represents details of keyboard and mouse navigation events.
        /// </summary>
        internal class ImageListViewNavigationManager : IDisposable
        {
            #region Constants
            /// <summary>
            /// Selection tolerance in pixels.
            /// </summary>
            private const int SelectionTolerance = 5;
            #endregion

            #region Member Variables
            private ImageListView mImageListView;

            private bool inItemArea;
            private bool inHeaderArea;
            private bool inPaneArea;

            private Point lastViewOffset;
            private Point lastSeparatorDragLocation;
            private Point lastPaneResizeLocation;
            private Point lastMouseDownLocation;
            private Point lastMouseMoveLocation;
            private Dictionary<ImageListViewItem, bool> highlightedItems;
            private bool suppressClick;

            private bool lastMouseDownInItemArea;
            private bool lastMouseDownInColumnHeaderArea;
            private bool lastMouseDownInPaneArea;

            private bool lastMouseDownOverItem;
            private bool lastMouseDownOverColumn;
            private bool lastMouseDownOverSeparator;
            private bool lastMouseDownOverPaneBorder;

            private bool selfDragging;

            private System.Windows.Forms.Timer scrollTimer;
            #endregion

            #region Properties
            /// <summary>
            /// Gets whether the left mouse button is down.
            /// </summary>
            public bool LeftButton { get; private set; }
            /// <summary>
            /// Gets whether the right mouse button is down.
            /// </summary>
            public bool RightButton { get; private set; }
            /// <summary>
            /// Gets whether the shift key is down.
            /// </summary>
            public bool ShiftKey { get; private set; }
            /// <summary>
            /// Gets whether the control key is down.
            /// </summary>
            public bool ControlKey { get; private set; }

            /// <summary>
            /// Gets the item under the mouse.
            /// </summary>
            public ImageListViewItem HoveredItem { get; private set; }
            /// <summary>
            /// Gets the column under the mouse.
            /// </summary>
            public ImageListView.ImageListViewColumnHeader HoveredColumn { get; private set; }
            /// <summary>
            /// Gets the column whose separator is under the mouse.
            /// </summary>
            public ImageListView.ImageListViewColumnHeader HoveredSeparator { get; private set; }
            /// <summary>
            /// Gets the column whose separator is being dragged.
            /// </summary>
            public ImageListView.ImageListViewColumnHeader SelectedSeperator { get; private set; }
            /// <summary>
            /// Gets whether the mouse is over the pane border.
            /// </summary>
            public bool HoveredPaneBorder { get; private set; }

            /// <summary>
            /// Gets whether a mouse selection is in progress.
            /// </summary>
            public bool MouseSelecting { get; private set; }
            /// <summary>
            /// Gets whether a separator is being dragged with the mouse.
            /// </summary>
            public bool DraggingSeperator { get; private set; }
            /// <summary>
            /// Gets whether the left-pane is being resized with the mouse.
            /// </summary>
            public bool ResizingPane { get; private set; }

            /// <summary>
            /// Gets the target item for a drop operation.
            /// </summary>
            public ImageListViewItem DropTarget { get; private set; }
            /// <summary>
            /// Gets whether drop target is to the right of the item.
            /// </summary>
            public bool DropToRight { get; private set; }

            /// <summary>
            /// Gets the selection rectangle.
            /// </summary>
            public Rectangle SelectionRectangle { get; private set; }
            #endregion

            #region Constructor
            /// <summary>
            /// Initializes a new instance of the ImageListViewNavigationManager class.
            /// </summary>
            /// <param name="owner">The owner control.</param>
            public ImageListViewNavigationManager(ImageListView owner)
            {
                mImageListView = owner;

                DraggingSeperator = false;
                ResizingPane = false;

                LeftButton = false;
                RightButton = false;
                ShiftKey = false;
                ControlKey = false;

                HoveredItem = null;
                HoveredColumn = null;
                HoveredSeparator = null;
                SelectedSeperator = null;
                HoveredPaneBorder = false;

                MouseSelecting = false;

                DropTarget = null;
                DropToRight = false;
                selfDragging = false;

                highlightedItems = new Dictionary<ImageListViewItem, bool>();

                scrollTimer = new System.Windows.Forms.Timer();
                scrollTimer.Interval = 100;
                scrollTimer.Enabled = false;
                scrollTimer.Tick += new EventHandler(scrollTimer_Tick);

                suppressClick = false;
            }
            #endregion

            #region Instance Methods
            /// <summary>
            /// Determines whether the item is highlighted.
            /// </summary>
            public ItemHighlightState HighlightState(ImageListViewItem item)
            {
                bool highlighted = false;
                if (highlightedItems.TryGetValue(item, out highlighted))
                {
                    if (highlighted)
                        return ItemHighlightState.HighlightedAndSelected;
                    else
                        return ItemHighlightState.HighlightedAndUnSelected;
                }
                return ItemHighlightState.NotHighlighted;
            }
            /// <summary>
            /// Performs application-defined tasks associated with freeing, 
            /// releasing, or resetting unmanaged resources.
            /// </summary>
            public void Dispose()
            {
                scrollTimer.Dispose();
            }
            #endregion

            #region Mouse Event Handlers
            /// <summary>
            /// Handles control's MouseDown event.
            /// </summary>
            public void MouseDown(MouseEventArgs e)
            {
                if ((e.Button & MouseButtons.Left) != MouseButtons.None)
                    LeftButton = true;
                if ((e.Button & MouseButtons.Right) != MouseButtons.None)
                    RightButton = true;

                DoHitTest(e.Location);
                lastMouseDownInItemArea = inItemArea;
                lastMouseDownInColumnHeaderArea = inHeaderArea;
                lastMouseDownInPaneArea = inPaneArea;
                lastMouseDownOverItem = (HoveredItem != null);
                lastMouseDownOverColumn = (HoveredColumn != null);
                lastMouseDownOverSeparator = (HoveredSeparator != null);
                lastMouseDownOverPaneBorder = HoveredPaneBorder;

                lastViewOffset = mImageListView.ViewOffset;
                lastMouseDownLocation = e.Location;
            }
            /// <summary>
            /// Handles control's MouseMove event.
            /// </summary>
            public void MouseMove(MouseEventArgs e)
            {
                ImageListViewItem oldHoveredItem = HoveredItem;
                ImageListView.ImageListViewColumnHeader oldHoveredColumn = HoveredColumn;
                ImageListView.ImageListViewColumnHeader oldHoveredSeparator = HoveredSeparator;

                DoHitTest(e.Location);

                mImageListView.mRenderer.SuspendPaint();

                // Do we need to scroll the view?
                if (MouseSelecting && mImageListView.ScrollOrientation == ScrollOrientation.VerticalScroll && !scrollTimer.Enabled)
                {
                    if (e.Y > mImageListView.ClientRectangle.Bottom)
                    {
                        scrollTimer.Tag = -120;
                        scrollTimer.Enabled = true;
                    }
                    else if (e.Y < mImageListView.ClientRectangle.Top)
                    {
                        scrollTimer.Tag = 120;
                        scrollTimer.Enabled = true;
                    }
                }
                else if (MouseSelecting && mImageListView.ScrollOrientation == ScrollOrientation.HorizontalScroll && !scrollTimer.Enabled)
                {
                    if (e.X > mImageListView.ClientRectangle.Right)
                    {
                        scrollTimer.Tag = -120;
                        scrollTimer.Enabled = true;
                    }
                    else if (e.X < mImageListView.ClientRectangle.Left)
                    {
                        scrollTimer.Tag = 120;
                        scrollTimer.Enabled = true;
                    }
                }
                else if (scrollTimer.Enabled && mImageListView.ClientRectangle.Contains(e.Location))
                {
                    scrollTimer.Enabled = false;
                }

                if (DraggingSeperator)
                {
                    int delta = e.Location.X - lastSeparatorDragLocation.X;
                    int colwidth = SelectedSeperator.Width + delta;
                    if (colwidth > 16)
                        lastSeparatorDragLocation = e.Location;
                    else
                    {
                        lastSeparatorDragLocation = new Point(e.Location.X - colwidth + 16, e.Location.Y);
                        colwidth = 16;
                    }
                    SelectedSeperator.Width = colwidth;

                    HoveredItem = null;
                    HoveredColumn = SelectedSeperator;
                    HoveredSeparator = SelectedSeperator;
                    mImageListView.Refresh();
                }
                else if (ResizingPane)
                {
                    int delta = e.Location.X - lastPaneResizeLocation.X;
                    int width = mImageListView.mPaneWidth + delta;
                    if (width > 2)
                        lastPaneResizeLocation = e.Location;
                    else
                    {
                        lastPaneResizeLocation = new Point(e.Location.X - width + 2, e.Location.Y);
                        width = 2;
                    }
                    mImageListView.mPaneWidth = width;

                    HoveredItem = null;
                    HoveredColumn = null;
                    HoveredSeparator = null;
                    mImageListView.Refresh();
                }
                else if (MouseSelecting)
                {
                    if (!ShiftKey && !ControlKey)
                        mImageListView.SelectedItems.Clear(false);

                    // Create the selection rectangle
                    Point viewOffset = mImageListView.ViewOffset;
                    Point pt1 = new Point(lastMouseDownLocation.X - (viewOffset.X - lastViewOffset.X),
                        lastMouseDownLocation.Y - (viewOffset.Y - lastViewOffset.Y));
                    Point pt2 = new Point(e.Location.X, e.Location.Y);
                    SelectionRectangle = new Rectangle(Math.Min(pt1.X, pt2.X), Math.Min(pt1.Y, pt2.Y), Math.Abs(pt1.X - pt2.X), Math.Abs(pt1.Y - pt2.Y));

                    // Normalize to item area coordinates
                    pt1 = new Point(SelectionRectangle.Left, SelectionRectangle.Top);
                    pt2 = new Point(SelectionRectangle.Right, SelectionRectangle.Bottom);
                    Point itemAreaOffset = new Point(-mImageListView.layoutManager.ItemAreaBounds.Left,
                        -mImageListView.layoutManager.ItemAreaBounds.Top);
                    pt1.Offset(itemAreaOffset);
                    pt2.Offset(itemAreaOffset);

                    // Determine which items are highlighted
                    highlightedItems.Clear();
                    int startRow = (int)Math.Floor((float)(Math.Min(pt1.Y, pt2.Y) + viewOffset.Y) /
                        (float)mImageListView.layoutManager.ItemSizeWithMargin.Height);
                    int endRow = (int)Math.Floor((float)(Math.Max(pt1.Y, pt2.Y) + viewOffset.Y) /
                        (float)mImageListView.layoutManager.ItemSizeWithMargin.Height);
                    int startCol = (int)Math.Floor((float)(Math.Min(pt1.X, pt2.X) + viewOffset.X) /
                        (float)mImageListView.layoutManager.ItemSizeWithMargin.Width);
                    int endCol = (int)Math.Floor((float)(Math.Max(pt1.X, pt2.X) + viewOffset.X) /
                        (float)mImageListView.layoutManager.ItemSizeWithMargin.Width);
                    if (mImageListView.ScrollOrientation == ScrollOrientation.HorizontalScroll &&
                        (startRow >= 0 || endRow >= 0))
                    {
                        for (int i = startCol; i <= endCol; i++)
                        {
                            for (int col = startCol; col <= endCol; col++)
                            {
                                if (i >= 0 && i <= mImageListView.Items.Count - 1 &&
                                    !highlightedItems.ContainsKey(mImageListView.Items[i]))
                                    highlightedItems.Add(mImageListView.Items[i],
                                        (ControlKey ? !mImageListView.Items[i].Selected : true));
                            }
                        }
                    }
                    else if (mImageListView.ScrollOrientation == ScrollOrientation.VerticalScroll &&
                        (startCol >= 0 || endCol >= 0) && (startRow >= 0 || endRow >= 0) &&
                        (startCol <= mImageListView.layoutManager.Cols - 1 || endCol <= mImageListView.layoutManager.Cols - 1))
                    {
                        startCol = Math.Min(mImageListView.layoutManager.Cols - 1, Math.Max(0, startCol));
                        endCol = Math.Min(mImageListView.layoutManager.Cols - 1, Math.Max(0, endCol));
                        for (int row = startRow; row <= endRow; row++)
                        {
                            for (int col = startCol; col <= endCol; col++)
                            {
                                int i = row * mImageListView.layoutManager.Cols + col;
                                if (i >= 0 && i <= mImageListView.Items.Count - 1 &&
                                    !highlightedItems.ContainsKey(mImageListView.Items[i]))
                                    highlightedItems.Add(mImageListView.Items[i],
                                        (ControlKey ? !mImageListView.Items[i].Selected : true));
                            }
                        }
                    }

                    HoveredColumn = null;
                    HoveredSeparator = null;
                    SelectedSeperator = null;

                    mImageListView.Refresh();
                }
                else if (!MouseSelecting && !DraggingSeperator && !ResizingPane &&
                    inItemArea && lastMouseDownInItemArea &&
                    (LeftButton || RightButton) &&
                    ((Math.Abs(e.Location.X - lastMouseDownLocation.X) > SelectionTolerance ||
                    Math.Abs(e.Location.Y - lastMouseDownLocation.Y) > SelectionTolerance)))
                {
                    if (!lastMouseDownOverItem && HoveredItem == null)
                    {
                        // Start mouse selection
                        MouseSelecting = true;
                        SelectionRectangle = new Rectangle(lastMouseDownLocation, new Size(0, 0));
                        mImageListView.Refresh();
                    }
                    else if (lastMouseDownOverItem && HoveredItem != null && mImageListView.AllowDrag)
                    {
                        // Start drag&drop
                        if (!HoveredItem.Selected)
                        {
                            mImageListView.SelectedItems.Clear(false);
                            HoveredItem.mSelected = true;
                            mImageListView.OnSelectionChangedInternal();
                            DropTarget = null;
                            mImageListView.Refresh(true);
                        }

                        // Set drag data
                        List<string> filenames = new List<string>();
                        foreach (ImageListViewItem item in mImageListView.SelectedItems)
                        {
                            if (item.isVirtualItem)
                            {
                                // Get the virtual item source image
                                VirtualItemImageEventArgs ve = new VirtualItemImageEventArgs(item.mVirtualItemKey);
                                mImageListView.RetrieveVirtualItemImageInternal(ve);
                                if (!string.IsNullOrWhiteSpace(ve.FileName))
                                    filenames.Add(ve.FileName);
                            }
                            else
                            {
                                filenames.Add(item.FileFullPath);
                            }
                        }

                        string[] fileList = filenames.ToArray();
                        DataObject fileDragData = new DataObject(DataFormats.FileDrop, fileList);

                        DropTarget = null;
                        selfDragging = true;
                        mImageListView.DoDragDrop(fileDragData,  DragDropEffects.Copy | DragDropEffects.Move); // Allowed effects
                        selfDragging = false;

                        // Since the MouseUp event will be eaten by DoDragDrop we will not receive
                        // the MouseUp event. We need to manually update mouse button flags after
                        // the drop.
                        if ((Control.MouseButtons & MouseButtons.Left) == MouseButtons.None)
                            LeftButton = false;
                        if ((Control.MouseButtons & MouseButtons.Right) == MouseButtons.None)
                            RightButton = false;

                        //ADD Added by JTN, remove item that is moved
                        List<ImageListViewItem> itemsMoved = new List<ImageListViewItem>();
                        foreach (string fullFilename in fileList)
                        {                          
                            foreach (ImageListViewItem item in mImageListView.SelectedItems)
                            {
                                if (String.Compare(item.FileFullPath, fullFilename, comparisonType: StringComparison.OrdinalIgnoreCase) == 0)
                                {
                                    if (!File.Exists(fullFilename))
                                    {
                                        itemsMoved.Add(item);
                                        break;
                                    }
                                }
                            }                            
                        }
                        if (itemsMoved.Count > 0)
                        {
                            mImageListView.SelectedItems.Clear(); //Remove selection, files are moved.
                            foreach (ImageListViewItem itemFound in itemsMoved) mImageListView.Items.RemoveItem(itemFound);
                        }
                    }
                }
                else if (!MouseSelecting && !DraggingSeperator && !ResizingPane &&
                    inHeaderArea && lastMouseDownInColumnHeaderArea && lastMouseDownOverSeparator && LeftButton &&
                    mImageListView.AllowColumnResize && HoveredSeparator != null)
                {
                    // Start dragging a separator
                    DraggingSeperator = true;
                    SelectedSeperator = HoveredSeparator;
                    lastSeparatorDragLocation = e.Location;
                }
                else if (!MouseSelecting && !DraggingSeperator && !ResizingPane &&
                    inPaneArea && lastMouseDownInPaneArea && lastMouseDownOverPaneBorder && LeftButton &&
                    mImageListView.AllowPaneResize && HoveredPaneBorder != false)
                {
                    // Start dragging the pane
                    ResizingPane = true;
                    lastPaneResizeLocation = e.Location;
                }
                else if (!ReferenceEquals(HoveredItem, oldHoveredItem) ||
                    !ReferenceEquals(HoveredColumn, oldHoveredColumn) ||
                    !ReferenceEquals(HoveredSeparator, oldHoveredSeparator))
                {
                    // Hovered item changed
                    if (!ReferenceEquals(HoveredItem, oldHoveredItem))
                        mImageListView.OnItemHover(new ItemHoverEventArgs(HoveredItem, oldHoveredItem));

                    if (!ReferenceEquals(HoveredColumn, oldHoveredColumn))
                        mImageListView.OnColumnHover(new ColumnHoverEventArgs(HoveredColumn, oldHoveredColumn));

                    mImageListView.Refresh();
                }

                mImageListView.mRenderer.ResumePaint();

                // Change to size cursor if mouse is over a column separator or pane border
                if (mImageListView.Cursor != Cursors.VSplit && mImageListView.Focused && !MouseSelecting)
                {
                    if ((mImageListView.AllowColumnResize && HoveredSeparator != null) ||
                        (mImageListView.AllowPaneResize && HoveredPaneBorder != false))
                        mImageListView.Cursor = Cursors.VSplit;
                }
                else if (mImageListView.Cursor == Cursors.VSplit)
                {
                    if (!((inHeaderArea && (DraggingSeperator || HoveredSeparator != null)) ||
                        (inPaneArea && (ResizingPane || HoveredPaneBorder != false))))
                        mImageListView.Cursor = Cursors.Default;
                }

                lastMouseMoveLocation = e.Location;
            }
            /// <summary>
            /// Handles control's MouseUp event.
            /// </summary>
            public void MouseUp(MouseEventArgs e)
            {
                DoHitTest(e.Location);

                mImageListView.mRenderer.SuspendPaint();

                // Stop if we are scrolling
                if (scrollTimer.Enabled)
                    scrollTimer.Enabled = false;

                if (DraggingSeperator)
                {
                    mImageListView.OnColumnWidthChanged(new ColumnEventArgs(SelectedSeperator));
                    SelectedSeperator = null;
                    DraggingSeperator = false;
                }
                else if (ResizingPane)
                {
                    ResizingPane = false;
                }
                else if (MouseSelecting)
                {
                    // Apply highlighted items
                    if (highlightedItems.Count != 0)
                    {
                        foreach (KeyValuePair<ImageListViewItem, bool> pair in highlightedItems)
                            pair.Key.mSelected = pair.Value;
                        highlightedItems.Clear();
                    }

                    mImageListView.OnSelectionChangedInternal();

                    MouseSelecting = false;

                    mImageListView.Refresh();
                }
                else if (lastMouseDownInItemArea && lastMouseDownOverItem && HoveredItem != null && LeftButton)
                {
                    // Select the item under the cursor
                    if (ControlKey)
                    {
                        HoveredItem.mSelected = !HoveredItem.mSelected;
                    }
                    else if (ShiftKey)
                    {
                        int startIndex = 0;
                        if (mImageListView.SelectedItems.Count != 0)
                        {
                            startIndex = mImageListView.SelectedItems[0].Index;
                            mImageListView.SelectedItems.Clear(false);
                        }

                        int endIndex = HoveredItem.Index;
                        if (mImageListView.ScrollOrientation == ScrollOrientation.VerticalScroll)
                        {
                            int startRow = Math.Min(startIndex, endIndex) / mImageListView.layoutManager.Cols;
                            int endRow = Math.Max(startIndex, endIndex) / mImageListView.layoutManager.Cols;
                            int startCol = Math.Min(startIndex, endIndex) % mImageListView.layoutManager.Cols;
                            int endCol = Math.Max(startIndex, endIndex) % mImageListView.layoutManager.Cols;

                            for (int row = startRow; row <= endRow; row++)
                            {
                                for (int col = startCol; col <= endCol; col++)
                                {
                                    int index = row * mImageListView.layoutManager.Cols + col;
                                    mImageListView.Items[index].mSelected = true;
                                }
                            }
                        }
                        else
                        {
                            for (int i = Math.Min(startIndex, endIndex); i <= Math.Max(startIndex, endIndex); i++)
                                mImageListView.Items[i].mSelected = true;
                        }
                    }
                    else
                    {
                        mImageListView.SelectedItems.Clear(false);
                        HoveredItem.mSelected = true;
                    }

                    // Raise the selection change event
                    mImageListView.OnSelectionChangedInternal();
                    mImageListView.OnItemClick(new ItemClickEventArgs(HoveredItem, e.Location, e.Button));

                    // Set the item as the focused item
                    mImageListView.Items.FocusedItem = HoveredItem;

                    mImageListView.Refresh();
                }
                else if (lastMouseDownInItemArea && lastMouseDownOverItem && HoveredItem != null && RightButton)
                {
                    if (!ControlKey && !HoveredItem.Selected)
                    {
                        // Clear the selection if Control key is not pressed
                        mImageListView.SelectedItems.Clear(false);
                        HoveredItem.mSelected = true;
                        mImageListView.OnSelectionChangedInternal();
                    }

                    mImageListView.OnItemClick(new ItemClickEventArgs(HoveredItem, e.Location, e.Button));
                    mImageListView.Items.FocusedItem = HoveredItem;
                }
                else if (lastMouseDownInItemArea && inItemArea && HoveredItem == null && (LeftButton || RightButton))
                {
                    // Clear selection if clicked in empty space
                    mImageListView.SelectedItems.Clear();
                    mImageListView.Refresh();
                }
                else if (lastMouseDownInColumnHeaderArea && lastMouseDownOverColumn &&
                    mImageListView.AllowColumnClick && HoveredColumn != null && HoveredSeparator == null)
                {
                    if (!suppressClick)
                    {
                        // Change the sort column
                        if (mImageListView.SortColumn == HoveredColumn.Type)
                        {
                            if (mImageListView.SortOrder == SortOrder.Descending)
                                mImageListView.SortOrder = SortOrder.Ascending;
                            else
                                mImageListView.SortOrder = SortOrder.Descending;
                        }
                        else
                        {
                            mImageListView.mSortColumn = HoveredColumn.Type;
                            mImageListView.mSortOrder = SortOrder.Ascending;
                            mImageListView.Sort();
                        }
                        mImageListView.OnColumnClick(new ColumnClickEventArgs(HoveredColumn, e.Location, e.Button));
                    }
                    else
                        suppressClick = false;
                }

                if ((e.Button & MouseButtons.Left) != MouseButtons.None)
                    LeftButton = false;
                if ((e.Button & MouseButtons.Right) != MouseButtons.None)
                    RightButton = false;

                mImageListView.mRenderer.ResumePaint();
            }
            /// <summary>
            /// Handles control's MouseDoubleClick event.
            /// </summary>
            public void MouseDoubleClick(MouseEventArgs e)
            {
                if (lastMouseDownInItemArea && lastMouseDownOverItem && HoveredItem != null)
                {
                    mImageListView.OnItemDoubleClick(new ItemClickEventArgs(HoveredItem, e.Location, e.Button));
                }
                else if (lastMouseDownInColumnHeaderArea && lastMouseDownOverSeparator &&
                    mImageListView.AllowColumnClick && HoveredSeparator != null)
                {
                    HoveredSeparator.AutoFit();
                    mImageListView.Refresh();
                    suppressClick = true;
                }
            }
            /// <summary>
            /// Handles control's MouseLeave event.
            /// </summary>
            public void MouseLeave()
            {
                if (HoveredItem != null || HoveredColumn != null || HoveredSeparator != null || HoveredPaneBorder != false)
                {
                    if (HoveredItem != null)
                        mImageListView.OnItemHover(new ItemHoverEventArgs(null, HoveredItem));
                    if (HoveredColumn != null)
                        mImageListView.OnColumnHover(new ColumnHoverEventArgs(null, HoveredColumn));

                    HoveredItem = null;
                    HoveredColumn = null;
                    HoveredSeparator = null;
                    HoveredPaneBorder = false;
                    mImageListView.Refresh();
                }
            }
            #endregion

            #region Key Event Handlers
            /// <summary>
            /// Handles control's KeyDown event.
            /// </summary>
            public void KeyDown(KeyEventArgs e)
            {
                ShiftKey = (e.Modifiers & Keys.Shift) == Keys.Shift;
                ControlKey = (e.Modifiers & Keys.Control) == Keys.Control;

                mImageListView.mRenderer.SuspendPaint();

                // If the shift key or the control key is pressed and there is no focused item
                // set the first item as the focused item.
                if ((ShiftKey || ControlKey) && mImageListView.Items.Count != 0 &&
                    mImageListView.Items.FocusedItem == null)
                {
                    mImageListView.Items.FocusedItem = mImageListView.Items[0];
                    mImageListView.Refresh();
                }

                if (mImageListView.Items.Count != 0)
                {
                    int index = 0;
                    if (mImageListView.Items.FocusedItem != null)
                        index = mImageListView.Items.FocusedItem.Index;

                    int newindex = ApplyNavKey(index, e.KeyCode);
                    if (index != newindex)
                    {
                        if (ControlKey)
                        {
                            // Just move the focus
                        }
                        else if (ShiftKey)
                        {
                            int startIndex = 0;
                            int endIndex = 0;
                            if (mImageListView.SelectedItems.Count != 0)
                            {
                                startIndex = mImageListView.SelectedItems[0].Index;
                                endIndex = mImageListView.SelectedItems[mImageListView.SelectedItems.Count - 1].Index;
                                mImageListView.SelectedItems.Clear(false);
                            }
                            if (index == startIndex)
                                startIndex = newindex;
                            else if (index == endIndex)
                                endIndex = newindex;
                            for (int i = Math.Min(startIndex, endIndex); i <= Math.Max(startIndex, endIndex); i++)
                            {
                                mImageListView.Items[i].mSelected = true;
                            }
                            mImageListView.OnSelectionChangedInternal();
                        }
                        else
                        {
                            mImageListView.SelectedItems.Clear(false);
                            mImageListView.Items[newindex].mSelected = true;
                            mImageListView.OnSelectionChangedInternal();
                        }
                        mImageListView.Items.FocusedItem = mImageListView.Items[newindex];
                        mImageListView.EnsureVisible(newindex);
                    }
                }

                mImageListView.mRenderer.ResumePaint();
            }
            /// <summary>
            /// Handles control's KeyUp event.
            /// </summary>
            public void KeyUp(KeyEventArgs e)
            {
                ShiftKey = (e.Modifiers & Keys.Shift) == Keys.Shift;
                ControlKey = (e.Modifiers & Keys.Control) == Keys.Control;
            }
            #endregion

            #region Drag and Drop Event Handlers
            /// <summary>
            /// Handles control's DragDrop event.
            /// </summary>
            public void DragDrop(DragEventArgs e)
            {
                mImageListView.mRenderer.SuspendPaint();

                if (selfDragging)
                {
                    // Reorder items
                    List<ImageListViewItem> draggedItems = new List<ImageListViewItem>();
                    int i = -1;
                    if (DropTarget != null) i = DropTarget.Index;
                    foreach (ImageListViewItem item in mImageListView.SelectedItems)
                    {
                        if (item.Index <= i) i--;
                        draggedItems.Add(item);
                        mImageListView.Items.RemoveInternal(item, false);
                    }
                    if (i < 0) i = 0;
                    if (i > mImageListView.Items.Count - 1) i = mImageListView.Items.Count - 1;
                    if (DropToRight) i++;
                    foreach (ImageListViewItem item in draggedItems)
                    {
                        mImageListView.Items.InsertInternal(i, item);
                        i++;
                    }
                    mImageListView.OnSelectionChangedInternal();
                }
                else
                {
                    int index = mImageListView.Items.Count;
                    if (DropTarget != null) index = DropTarget.Index;
                    if (DropToRight) index++;
                    if (index > mImageListView.Items.Count)
                        index = mImageListView.Items.Count;

                    string[] fileAndFolderNames = (string[])e.Data.GetData(DataFormats.FileDrop);
                    if (fileAndFolderNames != null)
                    {
                        List<string> fileCollection = new List<string>();
                        foreach (string fileOrFolder in fileAndFolderNames)
                        {
                            if (File.Exists(fileOrFolder)) fileCollection.Add(fileOrFolder);
                            else if (Directory.Exists(fileOrFolder))
                            {
                                string[] files = Directory.GetFiles(fileOrFolder, "*", SearchOption.AllDirectories);
                                fileCollection.AddRange(files);
                            }
                            
                        }
                        mImageListView.OnDropFiles(new DropFileEventArgs(index, fileCollection.ToArray()));
                    }
                    
                }

                DropTarget = null;
                selfDragging = false;

                mImageListView.Refresh();
                mImageListView.mRenderer.ResumePaint();
            }
            /// <summary>
            /// Handles control's DragEnter event.
            /// </summary>
            public void DragEnter(DragEventArgs e)
            {
                if (!selfDragging && e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Move;
                else
                    e.Effect = DragDropEffects.Copy;
            }
            /// <summary>
            /// Handles control's DragOver event.
            /// </summary>
            public void DragOver(DragEventArgs e)
            {
                if (mImageListView.AllowDrop || (mImageListView.AllowDrag && selfDragging))
                {
                    if (mImageListView.Items.Count == 0)
                    {
                        if (selfDragging)
                            e.Effect = DragDropEffects.None;
                        else
                            e.Effect = DragDropEffects.Link; //Drag and Drop from External source as File Explorer
                    }
                    else
                    {
                        // Calculate the location of the insertion cursor
                        Point pt = new Point(e.X, e.Y);
                        pt = mImageListView.PointToClient(pt);

                        // Do we need to scroll the view?
                        if (mImageListView.ScrollOrientation == ScrollOrientation.VerticalScroll &&
                            pt.Y > mImageListView.ClientRectangle.Bottom - 20)
                        {
                            scrollTimer.Tag = -120;
                            scrollTimer.Enabled = true;
                        }
                        else if (mImageListView.ScrollOrientation == ScrollOrientation.VerticalScroll &&
                            pt.Y < mImageListView.ClientRectangle.Top + 20)
                        {
                            scrollTimer.Tag = 120;
                            scrollTimer.Enabled = true;
                        }
                        else if (mImageListView.ScrollOrientation == ScrollOrientation.HorizontalScroll &&
                            pt.X > mImageListView.ClientRectangle.Right - 20)
                        {
                            scrollTimer.Tag = -120;
                            scrollTimer.Enabled = true;
                        }
                        else if (mImageListView.ScrollOrientation == ScrollOrientation.HorizontalScroll &&
                            pt.X < mImageListView.ClientRectangle.Left + 20)
                        {
                            scrollTimer.Tag = 120;
                            scrollTimer.Enabled = true;
                        }
                        else
                            scrollTimer.Enabled = false;

                        // Normalize to item area coordinates
                        pt.X -= mImageListView.layoutManager.ItemAreaBounds.Left;
                        pt.Y -= mImageListView.layoutManager.ItemAreaBounds.Top;

                        // Row and column mouse is over
                        bool dragCaretOnRight = false;
                        int index = 0;

                        if (mImageListView.ScrollOrientation == ScrollOrientation.HorizontalScroll)
                        {
                            index = (pt.X + mImageListView.ViewOffset.X) / mImageListView.layoutManager.ItemSizeWithMargin.Width;
                        }
                        else
                        {
                            int col = pt.X / mImageListView.layoutManager.ItemSizeWithMargin.Width;
                            int row = (pt.Y + mImageListView.ViewOffset.Y) / mImageListView.layoutManager.ItemSizeWithMargin.Height;
                            if (col > mImageListView.layoutManager.Cols - 1)
                            {
                                col = mImageListView.layoutManager.Cols - 1;
                                dragCaretOnRight = true;
                            }
                            index = row * mImageListView.layoutManager.Cols + col;
                        }

                        if (index < 0) index = 0;
                        if (index > mImageListView.Items.Count - 1)
                        {
                            index = mImageListView.Items.Count - 1;
                            dragCaretOnRight = true;
                        }

                        ImageListViewItem dragDropTarget = mImageListView.Items[index];

                        if (selfDragging && (dragDropTarget.Selected ||
                            (!dragCaretOnRight && index > 0 && mImageListView.Items[index - 1].Selected) ||
                            (dragCaretOnRight && index < mImageListView.Items.Count - 1 && mImageListView.Items[index + 1].Selected)))
                        {
                            e.Effect = DragDropEffects.None; //WHen moved selected files

                            dragDropTarget = null;
                        }
                        else if (selfDragging)
                            e.Effect = DragDropEffects.Move; //When move files inside ImageListView
                        else
                            e.Effect = DragDropEffects.Link; //When files dropped from File Explorer

                        if (!ReferenceEquals(dragDropTarget, DropTarget) || dragCaretOnRight != DropToRight)
                        {
                            DropTarget = dragDropTarget;
                            DropToRight = dragCaretOnRight;
                            mImageListView.Refresh(true);
                        }
                    }
                }
                else
                    e.Effect = DragDropEffects.None; //When Allow Drag&Drop turn off
            }
            /// <summary>
            /// Handles control's DragLeave event.
            /// </summary>
            public void DragLeave()
            {
                if (mImageListView.AllowDrag && selfDragging)
                {
                    DropTarget = null;
                    mImageListView.Refresh(true);
                }

                if (scrollTimer.Enabled)
                    scrollTimer.Enabled = false;
            }
            #endregion

            #region Helper Methods
            /// <summary>
            /// Performs a hit test.
            /// </summary>
            private void DoHitTest(Point pt)
            {
                ImageListView.HitInfo h;
                mImageListView.HitTest(pt, out h);

                if (h.ItemHit)
                    HoveredItem = mImageListView.Items[h.ItemIndex];
                else
                    HoveredItem = null;

                if (h.ColumnHit)
                    HoveredColumn = mImageListView.Columns[h.ColumnIndex];
                else
                    HoveredColumn = null;

                if (h.ColumnSeparatorHit)
                    HoveredSeparator = mImageListView.Columns[h.ColumnSeparator];
                else
                    HoveredSeparator = null;

                if (h.PaneBorder)
                    HoveredPaneBorder = true;
                else
                    HoveredPaneBorder = false;

                inItemArea = h.InItemArea;
                inHeaderArea = h.InHeaderArea;
                inPaneArea = h.InPaneArea;
            }
            /// <summary>
            /// Returns the item index after applying the given navigation key.
            /// </summary>
            private int ApplyNavKey(int index, Keys key)
            {
                if (mImageListView.ScrollOrientation == ScrollOrientation.VerticalScroll)
                {
                    if (key == Keys.Up && index >= mImageListView.layoutManager.Cols)
                        index -= mImageListView.layoutManager.Cols;
                    else if (key == Keys.Down && index < mImageListView.Items.Count - mImageListView.layoutManager.Cols)
                        index += mImageListView.layoutManager.Cols;
                    else if (key == Keys.Left && index > 0)
                        index--;
                    else if (key == Keys.Right && index < mImageListView.Items.Count - 1)
                        index++;
                    else if (key == Keys.PageUp && index >= mImageListView.layoutManager.Cols * (mImageListView.layoutManager.Rows - 1))
                        index -= mImageListView.layoutManager.Cols * (mImageListView.layoutManager.Rows - 1);
                    else if (key == Keys.PageDown && index < mImageListView.Items.Count - mImageListView.layoutManager.Cols * (mImageListView.layoutManager.Rows - 1))
                        index += mImageListView.layoutManager.Cols * (mImageListView.layoutManager.Rows - 1);
                    else if (key == Keys.Home)
                        index = 0;
                    else if (key == Keys.End)
                        index = mImageListView.Items.Count - 1;
                }
                else
                {
                    if (key == Keys.Left && index > 0)
                        index--;
                    else if (key == Keys.Right && index < mImageListView.Items.Count - 1)
                        index++;
                    else if (key == Keys.PageUp && index >= mImageListView.layoutManager.Cols)
                        index -= mImageListView.layoutManager.Cols;
                    else if (key == Keys.PageDown && index < mImageListView.Items.Count - mImageListView.layoutManager.Cols)
                        index += mImageListView.layoutManager.Cols;
                    else if (key == Keys.Home)
                        index = 0;
                    else if (key == Keys.End)
                        index = mImageListView.Items.Count - 1;
                }

                if (index < 0)
                    index = 0;
                else if (index > mImageListView.Items.Count - 1)
                    index = mImageListView.Items.Count - 1;

                return index;
            }
            #endregion

            #region Scroll Timer
            /// <summary>
            /// Handles the Tick event of the scrollTimer control.
            /// </summary>
            private void scrollTimer_Tick(object sender, EventArgs e)
            {
                int delta = (int)scrollTimer.Tag;
                if (MouseSelecting)
                {
                    Point location = mImageListView.PointToClient(Control.MousePosition);
                    mImageListView.OnMouseMove(new MouseEventArgs(Control.MouseButtons, 0, location.X, location.Y, 0));
                }
                mImageListView.OnMouseWheel(new MouseEventArgs(MouseButtons.None, 0, 0, 0, delta));
            }
            #endregion
        }
    }
}