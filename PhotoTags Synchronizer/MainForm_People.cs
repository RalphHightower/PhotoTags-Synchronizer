﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using DataGridViewGeneric;
using MetadataLibrary;
using Thumbnails;
using Krypton.Toolkit;

namespace PhotoTagsSynchronizer
{

    public partial class MainForm : KryptonForm
    {
        #region CellMouseClick - Changes PushToUndoStack
        private void dataGridViewPeople_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;
            try
            {
                Rectangle cellRectangle = ((DataGridView)sender).GetCellDisplayRectangle(e.ColumnIndex, e.RowIndex, false);
                if (e.X >= cellRectangle.Width - tristateButtonWidth && e.Y <= tristateBittonHight) triStateButtomClick = true;
                else triStateButtomClick = false;
                if (!triStateButtomClick) return;

                DataGridView dataGridView = ((DataGridView)sender);
                if (!dataGridView.Enabled) return;

                if (dataGridView.SelectedCells.Count < 1) return;

                DataGridViewSelectedCellCollection dataGridViewSelectedCellCollection = dataGridView.SelectedCells;
                if (dataGridViewSelectedCellCollection.Count < 1) return;

                Dictionary<CellLocation, DataGridViewGenericCell> updatedCells = DataGridViewHandler.ToggleCells(dataGridView, DataGridViewHandlerPeople.headerPeople, NewState.Toggle, e.ColumnIndex, e.RowIndex);
                List<int> updatedColumns = new List<int>();

                if (updatedCells != null && updatedCells.Count > 0)
                {
                    ClipboardUtility.PushToUndoStack(dataGridView, updatedCells);
                    foreach (CellLocation cellLocation in updatedCells.Keys)
                    {
                        if (!updatedColumns.Contains(cellLocation.ColumnIndex)) updatedColumns.Add(cellLocation.ColumnIndex);
                        DataGridViewHandler.InvalidateCell(dataGridView, cellLocation.ColumnIndex, cellLocation.RowIndex);

                        if (cellLocation.ColumnIndex == -1) dataGridView.InvalidateCell(dataGridView.Columns[cellLocation.ColumnIndex].HeaderCell);
                        if (cellLocation.RowIndex != -1) dataGridView.InvalidateCell(dataGridView.Rows[cellLocation.RowIndex].HeaderCell);
                    }
                    DataGridView_UpdatedDirtyFlags(dataGridView);
                }

                dataGridView.InvalidateCell(dataGridView.Rows[0].HeaderCell);
                if (e.ColumnIndex != -1) dataGridView.InvalidateCell(dataGridView.Columns[e.ColumnIndex].HeaderCell);
                if (e.ColumnIndex != -1) DataGridViewHandler.InvalidateCell(dataGridView, e.ColumnIndex, 0);
                if (e.RowIndex != -1) dataGridView.InvalidateCell(dataGridView.Rows[e.RowIndex].HeaderCell);
                if (e.ColumnIndex == -1 && e.RowIndex == 0) dataGridView.Invalidate();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion 

        #region Cell TriState Click
        bool triStateButtomClick = false;
        int tristateButtonWidth = 32;
        int tristateBittonHight = 20;
        #endregion

        #region Cell Updated name
        private void dataGridViewPeople_CellParsing(object sender, DataGridViewCellParsingEventArgs e)
        {
            if (!(sender is DataGridView)) return;
            try
            {
                object val = DataGridViewHandler.GetCellValue((DataGridView)sender, e.ColumnIndex, e.RowIndex);
                if (!(val is RegionStructure)) return;
                RegionStructure region = (RegionStructure)val;

                if (region == null) return;
                region.Name = (string)e.Value;
                e.Value = region;
                e.ParsingApplied = true;
                //PeopleAddNewLastUseName(region.Name);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }            
        }
        #endregion

        #region Cell header - Face region
        private int peopleMouseDownX = -1;
        private int peopleMouseDownY = -1;
        private int peopleMouseMoveX = -1;
        private int peopleMouseMoveY = -1;
        private int peopleMouseDownColumn = int.MinValue;
        private bool drawingRegion = false;

        #region Cell header - Face region - CellMouseDown
        private void dataGridViewPeople_CellMouseDown(object sender, DataGridViewCellMouseEventArgs e)
        {
            drawingRegion = false;
            if (e.Button != MouseButtons.Left) return;

            DataGridView dataGridView = ((DataGridView)sender);
            if (!dataGridView.Enabled) return;

            try
            {
                if (e.RowIndex == -1 && e.ColumnIndex >= 0)
                {
                    if (!DataGridViewHandler.IsColumnSelected(dataGridView, e.ColumnIndex))
                    {
                        KryptonMessageBox.Show("You need to select a name cell for current media file.", "Missing selection on media file", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Information, showCtrlCopy: true);
                        return;
                    }

                    DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, e.ColumnIndex);
                    if (dataGridViewGenericColumn == null || dataGridViewGenericColumn.ReadWriteAccess != ReadWriteAccess.AllowCellReadAndWrite)
                    {
                        KryptonMessageBox.Show("You can only change region on current version on media file, not on historical or error log.", "Not correct column type", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Information, showCtrlCopy: true);
                        return;
                    }

                    List<int> selectedRows = DataGridViewHandler.GetRowSelected(dataGridView);
                    if (selectedRows.Count != 1)
                    {
                        KryptonMessageBox.Show("You can only create a region for one name cell at once.", "Wrong number of selection", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Information, showCtrlCopy: true);
                        return;
                    }
                    else
                    {
                        int selectedRow = selectedRows[0];
                        DataGridViewGenericRow dataGridViewGenericRow = DataGridViewHandler.GetRowDataGridViewGenericRow(dataGridView, selectedRow);

                        if (dataGridViewGenericRow == null || dataGridViewGenericRow.IsHeader)
                        {
                            KryptonMessageBox.Show("The selected cell can't be changed, need select another cell.", "Wrong cell selected", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Information, showCtrlCopy: true);
                            return;
                        }
                    }

                    Image image = dataGridViewGenericColumn.Thumbnail;
                    if (image == null)
                    {
                        KryptonMessageBox.Show("No media has been load, please wait or reload the media to fetch thumbnail image.", "Not media has been loaded", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Information, showCtrlCopy: true);
                        return;
                    }

                    Rectangle rectangleRoundedCellBounds = DataGridViewHandler.CalulateCellRoundedRectangleCellBounds(
                        new Rectangle(0, 0, dataGridView.Columns[e.ColumnIndex].Width, dataGridView.ColumnHeadersHeight));
                    Size thumbnailSize = DataGridViewHandler.CalulateCellImageSizeInRectagleWithUpScale(rectangleRoundedCellBounds, image.Size);
                    Rectangle rectangleCenterThumbnail = DataGridViewHandler.CalulateCellImageCenterInRectagle(rectangleRoundedCellBounds, thumbnailSize);

                    if (DataGridViewHandler.IsMouseWithinRectangle(e.X, e.Y, rectangleCenterThumbnail))
                    {
                        peopleMouseDownX = e.X;
                        peopleMouseDownY = e.Y;
                        peopleMouseDownColumn = e.ColumnIndex;

                        drawingRegion = true;
                    }

                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #region Cell header - Face region - CellMouseLeave
        private void dataGridViewPeople_CellMouseLeave(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                if (drawingRegion)
                {
                    drawingRegion = false;
                    peopleMouseDownColumn = int.MinValue;

                    DataGridView dataGridView = ((DataGridView)sender);
                    if (!dataGridView.Enabled) return;
                    DataGridViewHandler.Refresh(dataGridView);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #region Cell header - UpdateRegionThumbnail
        private void UpdateRegionThumbnail(DataGridView dataGridView)
        {
            try
            {
                foreach (DataGridViewCell dataGridViewCell in dataGridView.SelectedCells)
                {

                    DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, dataGridViewCell.ColumnIndex);
                    if (dataGridViewGenericColumn != null)
                    {
                        Image imageCoverArt = LoadMediaCoverArtPosterWithCache(dataGridViewGenericColumn.FileEntryAttribute.FileFullPath);
                        if (imageCoverArt != null)
                        {
                            DataGridViewGenericRow dataGridViewGenericRow = DataGridViewHandler.GetRowDataGridViewGenericRow(dataGridView, dataGridViewCell.RowIndex);
                            dataGridViewGenericRow.HeaderName = DataGridViewHandlerPeople.headerPeople;
                            DataGridViewHandler.SetRowHeaderNameAndFontStyle(dataGridView, dataGridViewCell.RowIndex, dataGridViewGenericRow);
                            DataGridViewHandler.SetCellRowHeight(dataGridView, dataGridViewCell.RowIndex, DataGridViewHandler.GetCellRowHeight(dataGridView));

                            DataGridViewGenericCellStatus dataGridViewGenericCellStatus = DataGridViewHandler.GetCellStatus(dataGridViewCell);
                            dataGridViewGenericCellStatus.CellReadOnly = false;
                            DataGridViewHandler.SetCellReadOnlyDependingOfStatus(dataGridView, dataGridViewCell.ColumnIndex, dataGridViewCell.RowIndex, dataGridViewGenericCellStatus);
                            //DataGridViewHandler.SetCellStatus(dataGridView, dataGridViewCell.ColumnIndex, dataGridViewCell.RowIndex, dataGridViewGenericCellStatus);
                            DataGridViewHandler.SetCellDefaultAfterUpdated(dataGridView, dataGridViewGenericCellStatus, dataGridViewCell.ColumnIndex, dataGridViewCell.RowIndex);
  
                            RegionStructure regionStructure = DataGridViewHandler.GetCellRegionStructure(dataGridView, dataGridViewCell.ColumnIndex, dataGridViewCell.RowIndex);
                            if (regionStructure != null)
                            {
                                if (imageCoverArt != null) regionStructure.Thumbnail = ThumbnailRegionHandler.CopyRegionFromImage(imageCoverArt, regionStructure);
                                else regionStructure.Thumbnail = (Image)Properties.Resources.RegionLoading;
                            }
                        } else
                        {
                            Logger.Error("Was not able to updated the region thumbnail. Poster was failed to load.");
                            KryptonMessageBox.Show("Was not able to updated the region thumbnail.\r\nPoster was failed to load.", "", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Warning, showCtrlCopy: true);
                        }
                        DataGridViewHandler.InvalidateCellColumnHeader(dataGridView, dataGridViewCell.ColumnIndex);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "UpdateRegionThumbnail");
                KryptonMessageBox.Show("Was not able to updated the region thumbnail.\r\n\r\n" + ex.Message, "Syntax error...", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Error, showCtrlCopy: true);
            }
        }
        #endregion 

        #region Cell header - Face region - CellMouseUp - PushToUndoStack
        private void dataGridViewPeople_CellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            drawingRegion = false;
            if (e.Button != MouseButtons.Left) return;

            DataGridView dataGridView = ((DataGridView)sender);
            if (!dataGridView.Enabled) return;
            try
            {
                if (e.RowIndex == -1 && e.ColumnIndex == peopleMouseDownColumn)
                {
                    DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, e.ColumnIndex);
                    if (dataGridViewGenericColumn == null) return;

                    lock (dataGridViewGenericColumn._ThumbnailLock)
                    {
                        Image image = dataGridViewGenericColumn.thumbnailUnlock;
                        Rectangle rectangleRoundedCellBounds = DataGridViewHandler.CalulateCellRoundedRectangleCellBounds(
                            new Rectangle(0, 0, dataGridView.Columns[e.ColumnIndex].Width, dataGridView.ColumnHeadersHeight));
                        Size thumbnailSize = DataGridViewHandler.CalulateCellImageSizeInRectagleWithUpScale(rectangleRoundedCellBounds, image.Size);
                        Rectangle rectangleCenterThumbnail = DataGridViewHandler.CalulateCellImageCenterInRectagle(rectangleRoundedCellBounds, thumbnailSize);

                        if (DataGridViewHandler.IsMouseWithinRectangle(e.X, e.Y, rectangleCenterThumbnail))
                        {
                            peopleMouseMoveX = e.X;
                            peopleMouseMoveY = e.Y;
                        }
                    }

                    dataGridView.InvalidateCell(e.ColumnIndex, e.RowIndex);

                    if (Math.Abs(peopleMouseDownX - peopleMouseMoveX) > 1 && Math.Abs(peopleMouseMoveY - peopleMouseDownY) > 1)
                    {
                        if (DataGridViewHandler.UpdateSelectedCellsWithNewMouseRegion(dataGridView, e.ColumnIndex, peopleMouseDownX, peopleMouseDownY, peopleMouseMoveX, peopleMouseMoveY))
                        {
                            DataGridViewHandler.SetColumnDirtyFlag(dataGridView, e.ColumnIndex, IsDataGridViewColumnDirty(dataGridView, e.ColumnIndex, out string diffrences), diffrences);
                            UpdateRegionThumbnail(dataGridView);
                        }
                    }
                    else
                    {
                        KryptonMessageBox.Show("Couldn't create a region. No region selection was made.", "No region selected", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Warning, showCtrlCopy: true);
                        peopleMouseDownColumn = int.MinValue;
                    }

                }

                peopleMouseDownColumn = int.MinValue;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #region Cell header - Face region - CellMouseMove
        private void dataGridViewPeople_CellMouseMove(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.Button != MouseButtons.Left) return;

            DataGridView dataGridView = ((DataGridView)sender);
            if (!dataGridView.Enabled) return;

            try
            {
                if (e.RowIndex == -1 && e.ColumnIndex == peopleMouseDownColumn)
                {
                    DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, e.ColumnIndex);
                    if (dataGridViewGenericColumn == null) return;

                    lock (dataGridViewGenericColumn._ThumbnailLock)
                    {
                        Image image = dataGridViewGenericColumn.thumbnailUnlock;
                        Rectangle rectangleRoundedCellBounds = DataGridViewHandler.CalulateCellRoundedRectangleCellBounds(
                            new Rectangle(0, 0, dataGridView.Columns[e.ColumnIndex].Width, dataGridView.ColumnHeadersHeight));
                        Size thumbnailSize = DataGridViewHandler.CalulateCellImageSizeInRectagleWithUpScale(rectangleRoundedCellBounds, image.Size);
                        Rectangle rectangleCenterThumbnail = DataGridViewHandler.CalulateCellImageCenterInRectagle(rectangleRoundedCellBounds, thumbnailSize);

                        if (DataGridViewHandler.IsMouseWithinRectangle(e.X, e.Y, rectangleCenterThumbnail))
                        {
                            peopleMouseMoveX = e.X;
                            peopleMouseMoveY = e.Y;

                            DataGridViewHandler.InvalidateCell(dataGridView, e.ColumnIndex, e.RowIndex);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #endregion

        #region AutoComplete

        #region AutoComplete - ClientListDropDown
        public AutoCompleteStringCollection ClientListDropDown()
        {
            AutoCompleteStringCollection autoCompleteStringCollection = new AutoCompleteStringCollection();
            try
            {
                List<string> regionNames = databaseAndCacheMetadataExiftool.ListAllPersonalRegionName();
                foreach (string regionName in regionNames)
                {
                    if (!string.IsNullOrWhiteSpace(regionName)) autoCompleteStringCollection.Add(regionName);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return autoCompleteStringCollection;

        }
        #endregion

        #region AutoComplete - EditingControlShowing
        private void dataGridViewPeople_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            try
            {
                DataGridView dataGridView = (DataGridView)sender;
                TextBox prodCode = e.Control as TextBox;
                if (prodCode != null)
                {
                    prodCode.AutoCompleteMode = AutoCompleteMode.SuggestAppend;
                    prodCode.AutoCompleteCustomSource = ClientListDropDown();
                    prodCode.AutoCompleteSource = AutoCompleteSource.CustomSource;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #endregion

        #region People name suggestion  

        private List<string> lastUsedNames = new List<string>();

        #region People name suggestion - PeopleAddNewLastUseName
        private void PeopleAddNewLastUseName(string name)
        {
            try
            {
                if (lastUsedNames.Contains(name)) lastUsedNames.Remove(name);
                lastUsedNames.Insert(0, name);
                while (lastUsedNames.Count > 5) lastUsedNames.RemoveAt(5);

                if (lastUsedNames.Count > 0)
                {
                    SetPeopleStripToolMenu(kryptonContextMenuItemGenericRegionRename1, 1, lastUsedNames[0]);
                    kryptonContextMenuItemGenericRegionRename1.Visible = true;
                }
                else kryptonContextMenuItemGenericRegionRename1.Visible = false;

                if (lastUsedNames.Count > 1)
                {
                    SetPeopleStripToolMenu(kryptonContextMenuItemGenericRegionRename2, 2, lastUsedNames[1]);
                    kryptonContextMenuItemGenericRegionRename2.Visible = true;
                }
                else kryptonContextMenuItemGenericRegionRename2.Visible = false;

                if (lastUsedNames.Count > 2)
                {
                    SetPeopleStripToolMenu(kryptonContextMenuItemGenericRegionRename3, 3, lastUsedNames[2]);
                    kryptonContextMenuItemGenericRegionRename3.Visible = true;
                }
                else kryptonContextMenuItemGenericRegionRename3.Visible = false;

                if (lastUsedNames.Count > 3)
                {
                    SetPeopleStripToolMenu(kryptonContextMenuItemGenericRegionRename4, 4, lastUsedNames[3]);
                    kryptonContextMenuItemGenericRegionRename4.Visible = true;
                }
                else kryptonContextMenuItemGenericRegionRename4.Visible = false;

                if (lastUsedNames.Count > 4)
                {
                    SetPeopleStripToolMenu(kryptonContextMenuItemGenericRegionRename5, 5, lastUsedNames[4]);
                    kryptonContextMenuItemGenericRegionRename5.Visible = true;
                }
                else kryptonContextMenuItemGenericRegionRename5.Visible = false;
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #region People name suggestion - SetPeopleStripToolMenu
        private void SetPeopleStripToolMenu(KryptonContextMenuItem toolStripMenuItem, int number, string name)
        {
            try
            {
                toolStripMenuItem.Tag = name;
                toolStripMenuItem.Text = "Rename #" + number;
                toolStripMenuItem.ExtraText = name;
                Properties.Settings.Default.PeopleRename = string.Join("\r\n", lastUsedNames.ToArray());
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion 

        private static HashSet<string> regionNamesRenameFromAllAdded = new HashSet<string>();
        private static HashSet<string> regionNamesRenameFromTopCoundAdded = new HashSet<string>();

        #region People name suggestion - PopulatePeopleToolStripMenuItems 

        #region FindFirstUnequal
        private int FindFirstUnequal(string text1, string text2)
        {
            int index = 0;
            while (true)
            {
                if (index >= text1.Length) return index; 
                if (index >= text2.Length) return index;
                if (text1[index] != text2[index]) return index;
                //     abc   index = 0    
                //
                //abc  abc
                //012  012   index = 3
                //
                //abc1 abc2
                //0123 0123  index = 3
                //               
                index++;
            }
        }
        #endregion

        #region GetSubStringIndex
        private string GetSubStringIndex(string text, int index)
        {
            return text.Substring(0, Math.Min(index, text.Length));
        }
        #endregion

        #region PopulatePeopleToolStripMenuItems
        public void PopulatePeopleToolStripMenuItems(List<DataGridViewGenericColumn> dataGridViewGenericColumns, 
            int suggestRegionNameNearbyDays, 
            int suggestRegionNameNearByContextMenuCount, int suggestRegionNameMostUsedContextMenuCount, int applicationSizeOfRegionNamesGroup,
            string allowedDateFormats)
        {
            try
            {
                List<string> regionNames = new List<string>();

                #region Most used
                kryptonContextMenuItemGenericRegionRenameMostUsedList.Items.Clear();
                regionNames = databaseAndCacheMetadataExiftool.ListAllPersonalRegionName(suggestRegionNameMostUsedContextMenuCount);
                regionNames.Sort();
                foreach (string name in regionNames)
                {
                    regionNamesRenameFromTopCoundAdded.Add(name);
                    KryptonContextMenuItem kryptonContextMenuItemGenericRegionRename = new KryptonContextMenuItem();
                    kryptonContextMenuItemGenericRegionRename.Tag = name;
                    kryptonContextMenuItemGenericRegionRename.Text = name;
                    kryptonContextMenuItemGenericRegionRename.Click += KryptonContextMenuItemRegionRenameGeneric_Click;
                    this.kryptonContextMenuItemGenericRegionRenameMostUsedList.Items.Add(kryptonContextMenuItemGenericRegionRename);
                }
                #endregion

                #region Near By
                kryptonContextMenuItemsGenericRegionRenameFromNearByList.Items.Clear();
                regionNames = new List<string>();
                if (dataGridViewGenericColumns != null)
                {
                    Dictionary<StringNullable, int> nameCount = new Dictionary<StringNullable, int>();
                    foreach (DataGridViewGenericColumn dataGridViewGenericColumn in dataGridViewGenericColumns)
                    {
                        if (dataGridViewGenericColumn.Metadata != null)
                        {
                            DateTime date = (DateTime)dataGridViewGenericColumn.Metadata.FileSmartDate(allowedDateFormats);
                            DateTime dateTimeFrom = date.AddDays(-suggestRegionNameNearbyDays);
                            DateTime dateTimeTo = date.AddDays(suggestRegionNameNearbyDays);
                            nameCount = databaseAndCacheMetadataExiftool.MergeRegionNameCount(nameCount, databaseAndCacheMetadataExiftool.ListAllRegionNameCountNearByCache(MetadataBrokerType.ExifTool, dateTimeFrom, dateTimeTo));
                        }
                    }
                    regionNames = databaseAndCacheMetadataExiftool.ConvertRegionNameCount(nameCount, suggestRegionNameNearByContextMenuCount);
                }
                regionNames.Sort();

                foreach (string name in regionNames)
                {
                    regionNamesRenameFromTopCoundAdded.Add(name);
                    KryptonContextMenuItem kryptonContextMenuItemGenericRegionRenameFromLastUsed = new KryptonContextMenuItem();
                    kryptonContextMenuItemGenericRegionRenameFromLastUsed.Tag = name;
                    kryptonContextMenuItemGenericRegionRenameFromLastUsed.Text = name;
                    kryptonContextMenuItemGenericRegionRenameFromLastUsed.Click += KryptonContextMenuItemRegionRenameGeneric_Click;
                    this.kryptonContextMenuItemsGenericRegionRenameFromNearByList.Items.Add(kryptonContextMenuItemGenericRegionRenameFromLastUsed);
                }
                #endregion

                #region All Region names
                kryptonContextMenuItemsGenericRegionRenameListAllList.Items.Clear();
                regionNames = databaseAndCacheMetadataExiftool.ListAllPersonalRegionName();
                regionNames.Sort();
                if (regionNames.Count <= applicationSizeOfRegionNamesGroup)
                {
                    foreach (string name in regionNames)
                    {
                        regionNamesRenameFromAllAdded.Add(name);
                        KryptonContextMenuItem kryptonContextMenuItemGenericRegionRenameFromListAll = new KryptonContextMenuItem();
                        kryptonContextMenuItemGenericRegionRenameFromListAll.Tag = name;
                        kryptonContextMenuItemGenericRegionRenameFromListAll.Text = name;
                        Image image = databaseAndCacheMetadataExiftool.ReadRandomThumbnailFromCacheOrDatabase(name);
                        if (image != null) kryptonContextMenuItemGenericRegionRenameFromListAll.Image = image;
                        kryptonContextMenuItemGenericRegionRenameFromListAll.Click += KryptonContextMenuItemRegionRenameGeneric_Click;
                        kryptonContextMenuItemsGenericRegionRenameListAllList.Items.Add(kryptonContextMenuItemGenericRegionRenameFromListAll);
                    }
                }
                else
                {
                    KryptonContextMenuItem kryptonContextMenuItemGenericGroupName = null;
                    KryptonContextMenuItem kryptonContextMenuItemGenericGroupNamePrevious = null;
                    KryptonContextMenuItems kryptonContextMenuItemGenericGroupList = null;

                    string firstNameInGroupCurrent = "";
                    string lastNameInGroupCurrent = "";
                    string lastNameInGroupPrevious = "";

                    string firstNameInGroubSubPrevious = "";
                    string lastNameInGroupSubPrevious = "";

                    string firstGroupNamePrevious = "";

                    int indexName = 0;
                    bool nameFixed = false;

                    foreach (string name in regionNames)
                    {
                        if (kryptonContextMenuItemGenericGroupName == null)
                        {
                            kryptonContextMenuItemGenericGroupName = new KryptonContextMenuItem();
                            kryptonContextMenuItemsGenericRegionRenameListAllList.Items.Add(kryptonContextMenuItemGenericGroupName);
                            kryptonContextMenuItemGenericGroupList = new KryptonContextMenuItems();
                            kryptonContextMenuItemGenericGroupName.Items.Add(kryptonContextMenuItemGenericGroupList);
                            nameFixed = false;
                        }

                        regionNamesRenameFromAllAdded.Add(name);
                        KryptonContextMenuItem kryptonContextMenuItemGenericRegionRenameFromListAll = new KryptonContextMenuItem();
                        kryptonContextMenuItemGenericRegionRenameFromListAll.Tag = name;
                        kryptonContextMenuItemGenericRegionRenameFromListAll.Text = name;
                        Image image = databaseAndCacheMetadataExiftool.ReadRandomThumbnailFromCacheOrDatabase(name);
                        if (image != null) kryptonContextMenuItemGenericRegionRenameFromListAll.Image = image;
                        kryptonContextMenuItemGenericRegionRenameFromListAll.Click += KryptonContextMenuItemRegionRenameGeneric_Click;
                        kryptonContextMenuItemGenericGroupList.Items.Add(kryptonContextMenuItemGenericRegionRenameFromListAll);

                        if (indexName == 0) firstNameInGroupCurrent = name;
                        if (indexName >= applicationSizeOfRegionNamesGroup - 1)
                        {
                            lastNameInGroupCurrent = name;

                            int indexNotEqualPrevious = FindFirstUnequal(firstGroupNamePrevious, lastNameInGroupPrevious) + 1;
                            int indexNotEqualPreviousAndCurrent = FindFirstUnequal(lastNameInGroupPrevious, firstNameInGroupCurrent) + 1;
                            int indexNotEqualCurrent = FindFirstUnequal(firstNameInGroupCurrent, lastNameInGroupCurrent) + 1;

                            if (kryptonContextMenuItemGenericGroupNamePrevious != null)
                            {
                                lastNameInGroupSubPrevious = GetSubStringIndex(lastNameInGroupPrevious, Math.Max(indexNotEqualPrevious, indexNotEqualPreviousAndCurrent));
                                kryptonContextMenuItemGenericGroupNamePrevious.Text = firstNameInGroubSubPrevious + " - " + lastNameInGroupSubPrevious;
                            }

                            string currentFirstSubName = GetSubStringIndex(firstNameInGroupCurrent, Math.Max(indexNotEqualCurrent, indexNotEqualPreviousAndCurrent));
                            string currentLastSubName = GetSubStringIndex(lastNameInGroupCurrent, indexNotEqualCurrent);
                            kryptonContextMenuItemGenericGroupName.Text = currentFirstSubName + " - " + currentLastSubName;


                            firstGroupNamePrevious = firstNameInGroupCurrent;
                            lastNameInGroupPrevious = lastNameInGroupCurrent;

                            firstNameInGroubSubPrevious = currentFirstSubName;
                            lastNameInGroupSubPrevious = currentLastSubName;

                            //Get ready for new group
                            indexName = 0;
                            kryptonContextMenuItemGenericGroupNamePrevious = kryptonContextMenuItemGenericGroupName;
                            kryptonContextMenuItemGenericGroupName = null;
                            kryptonContextMenuItemGenericGroupList = null;
                            nameFixed = true;
                        }
                        else indexName++;
                    }

                    if (!nameFixed)
                    {
                        int indexNotEqual = FindFirstUnequal(firstNameInGroupCurrent, lastNameInGroupCurrent) + 1;
                        kryptonContextMenuItemGenericGroupName.Text = GetSubStringIndex(firstNameInGroupCurrent, indexNotEqual) + " - " + GetSubStringIndex(lastNameInGroupCurrent, indexNotEqual);
                    }
                }
                string[] renameNames = Properties.Settings.Default.PeopleRename.Replace("\r", "").Split('\n');

                for (int i = renameNames.Length - 1; i >= 0; i--)
                {
                    PeopleAddNewLastUseName(renameNames[i]);
                }
                #endregion
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #endregion

        #region People rename - PeopleRenameCell
        private bool PeopleRenameCell(DataGridView dataGridView, DataGridViewCell cell, string nameSelected, Dictionary<CellLocation, DataGridViewGenericCell> updatedCells)
        {
            bool cellUpdated = false;
            try
            {
                DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, cell.ColumnIndex);
                DataGridViewGenericRow dataGridViewGenericRow = DataGridViewHandler.GetRowDataGridViewGenericRow(dataGridView, cell.RowIndex);

                if (dataGridViewGenericColumn != null && dataGridViewGenericRow != null &&
                    dataGridViewGenericColumn.ReadWriteAccess == ReadWriteAccess.AllowCellReadAndWrite &&
                    dataGridViewGenericRow.ReadWriteAccess == ReadWriteAccess.AllowCellReadAndWrite &&
                    !dataGridViewGenericRow.IsHeader)
                {
                    DataGridViewGenericCell dataGridViewGenericCellOriginal = DataGridViewHandler.GetCellDataGridViewGenericCellCopy(dataGridView, cell.ColumnIndex, cell.RowIndex);
                    if (!dataGridViewGenericCellOriginal.CellStatus.CellReadOnly)
                    {
                        CellLocation cellLocation = new CellLocation(cell.ColumnIndex, cell.RowIndex);
                        
                        cellUpdated = true;

                        if (dataGridViewGenericCellOriginal.Value is RegionStructure)
                        {
                            RegionStructure region = (RegionStructure)dataGridViewGenericCellOriginal.Value;
                            if (region != null)
                            {
                                if (region.Name != nameSelected) 
                                {
                                    if (!updatedCells.ContainsKey(cellLocation)) updatedCells.Add(cellLocation, new DataGridViewGenericCell(dataGridViewGenericCellOriginal));
                                    region.Name = nameSelected;
                                    PeopleAddNewLastUseName(nameSelected);
                                    DataGridViewHandler.SetCellValue(dataGridView, cell.ColumnIndex, cell.RowIndex, region, true);
                                }
                            }
                        }

                        DataGridViewHandlerPeople.SetCellDefault(dataGridView, MetadataBrokerType.Empty, cell.ColumnIndex, cell.RowIndex); //No DirtyFlagSet
                        //SetValue will do the trick DataGridViewHandler.SetColumnDirtyFlag(dataGridView, cell.ColumnIndex, IsDataGridViewColumnDirty(dataGridView, cell.ColumnIndex));
                    }

                }
                else if (dataGridViewGenericRow == null) //new row
                {
                    DataGridViewHandlerPeople.AddRowPeople(dataGridView, nameSelected);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
            return cellUpdated;
        }
        #endregion

        #region People rename - PeopleRenameSelected
        private void PeopleRenameSelected(DataGridView dataGridView, string nameSelected)
        {
            try
            {
                Dictionary<CellLocation, DataGridViewGenericCell> updatedCells = new Dictionary<CellLocation, DataGridViewGenericCell>();

                foreach (DataGridViewCell cell in dataGridView.SelectedCells)
                {
                    PeopleRenameCell(dataGridView, cell, nameSelected, updatedCells);
                }

                if (updatedCells != null && updatedCells.Count > 0) ClipboardUtility.PushToUndoStack(dataGridView, updatedCells);
                DataGridView_UpdatedDirtyFlags(dataGridView);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #region People rename - PeopleRenameSelected_Click
        private void KryptonContextMenuItemRegionRenameGeneric_Click(object sender, EventArgs e)
        {
            try
            {
                DataGridView dataGridView = dataGridViewPeople;
                if (!dataGridView.Enabled) return;
                PeopleRenameSelected(dataGridView, (string)((KryptonContextMenuItem)sender).Tag);
                DataGridViewHandler.Refresh(dataGridView);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }

        #endregion

        #region People rename - ActionRegionRename
        private void ActionRegionRename(string name)
        {
            try
            {
                DataGridView dataGridView = dataGridViewPeople;
                if (!dataGridView.Enabled) return;
                PeopleRenameSelected(dataGridView, name);
                DataGridViewHandler.Refresh(dataGridView);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #region People rename - RegionRename 1, 2, 3, 4, 5 - Click Events Sources
        private void KryptonContextMenuItemGenericRegionRenameGeneric_Click(object sender, EventArgs e)
        {
            try
            {
                ActionRegionRename((string)((KryptonContextMenuItem)sender).Tag);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #endregion 

        #region CheckRowAndSetDefaults
        private void CheckRowAndSetDefaults(DataGridView dataGridView, int columnIndex, int rowIndex)
        {
            try
            {
                for (int columnIndexCheck = 0; columnIndexCheck < DataGridViewHandler.GetColumnCount(dataGridView); columnIndexCheck++)
                {
                    DataGridViewGenericCellStatus dataGridViewGenericCellStatus = DataGridViewHandler.GetCellStatus(dataGridView, columnIndexCheck, rowIndex);
                    if (dataGridViewGenericCellStatus == null) dataGridViewGenericCellStatus = new DataGridViewGenericCellStatus(MetadataBrokerType.Empty, SwitchStates.Disabled, true);

                    DataGridViewHandler.SetCellDefaultAfterUpdated(dataGridView, dataGridViewGenericCellStatus, columnIndexCheck, rowIndex);
                }

                #region Set Row defaults
                DataGridViewGenericRow dataGridViewGenericRow = DataGridViewHandler.GetRowDataGridViewGenericRow(dataGridView, rowIndex);
                string dataGridViewGenericRowHeaderName = (dataGridViewGenericRow != null ? dataGridViewGenericRow.HeaderName : DataGridViewHandlerPeople.headerPeopleAdded);
                DataGridViewHandler.SetRowHeaderNameAndFontStyle(dataGridView, rowIndex,
                    new DataGridViewGenericRow(dataGridViewGenericRowHeaderName,
                    dataGridView.Rows[rowIndex].Cells[columnIndex].Value == null ? "" : dataGridView.Rows[rowIndex].Cells[columnIndex].Value.ToString(), ReadWriteAccess.AllowCellReadAndWrite));
                #endregion
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion 

        #region FormRegionSelect
        FormRegionSelect formRegionSelect = new FormRegionSelect();

        #region FormRegionSelect - OpenRegionSelector
        private void OpenRegionSelector()
        {
            try
            {
                if (formRegionSelect == null || formRegionSelect.IsDisposed) formRegionSelect = new FormRegionSelect();
                formRegionSelect.OnRegionSelected -= FormRegionSelect_OnRegionSelected;
                formRegionSelect.OnRegionSelected += FormRegionSelect_OnRegionSelected;
                formRegionSelect.Owner = this;
                if (formRegionSelect.WindowState == FormWindowState.Minimized) formRegionSelect.WindowState = FormWindowState.Normal;
                formRegionSelect.BringToFront();
                formRegionSelect.Show();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion 

        #region FormRegionSelect - RegionSelectorLoadAndSelect
        private void RegionSelectorLoadAndSelect(DataGridView dataGridView, int rowSelected = -1, int columnSelected = -1)
        {
            if (dataGridView == null) return;
            if (formRegionSelect == null) return;
            //UpdateRegionThumbnail(dataGridView);
            if (formRegionSelect.Visible == false) return;

            try 
            { 
                Cyotek.Windows.Forms.ImageBoxSelectionMode imageBoxSelectionMode = Cyotek.Windows.Forms.ImageBoxSelectionMode.Zoom;
            
                formRegionSelect.SetSelectionNone();
                if (!dataGridView.Enabled) { formRegionSelect.SetImageNone("No valid media file is selected, and no data loaded."); return; }

                string dataGridViewName = DataGridViewHandler.GetDataGridViewName(dataGridView);
                if (dataGridViewName == LinkTabAndDataGridViewNameRename || dataGridViewName == LinkTabAndDataGridViewNameConvertAndMerge)
                {
                    string errorMessag = "No valid media file is selected.\r\nSelect a row or a cell within a row to present a poster of media file.";


                    //Only one row can be selected, only one file can be shown
                    if (rowSelected == -1)
                    {
                        List<int> selectedRows = DataGridViewHandler.GetColumnSelected(dataGridView);
                        if (selectedRows.Count != 1) { formRegionSelect.SetImageNone(errorMessag); return; } //Can only show one poster
                        rowSelected = selectedRows[0];
                    }

                    DataGridViewGenericRow dataGridViewGenericRow = DataGridViewHandler.GetRowDataGridViewGenericRow(dataGridView, rowSelected);
                    if (dataGridViewGenericRow == null) { formRegionSelect.SetImageNone(errorMessag); return; }
                    if (dataGridViewGenericRow.IsHeader) { formRegionSelect.SetImageNone(errorMessag); return; }

                    formRegionSelect.SetImageText(Path.Combine(dataGridViewGenericRow.HeaderName, dataGridViewGenericRow.RowName));
                    Image image = LoadMediaCoverArtPosterWithCache(Path.Combine(dataGridViewGenericRow.HeaderName, dataGridViewGenericRow.RowName));
                    formRegionSelect.SetImage(image, "Showing: " + dataGridViewGenericRow.RowName, imageBoxSelectionMode);
                }
                else
                {
                    string errorMessag;
                    if (dataGridViewName != LinkTabAndDataGridViewNamePeople) errorMessag = "No valid media file is selected.\r\nSelect a column or a cell within a column to present a poster of media file.";
                    else
                    {
                        errorMessag = "No valid media file is selected.\r\n" +
                          "You need to select a 'region name cell'.\r\n" +
                          "Then you can drag and drop to create a region square for select cell.\r\n" +
                          "The region will be added to selected cell and will become named.";
                        imageBoxSelectionMode = Cyotek.Windows.Forms.ImageBoxSelectionMode.Rectangle;
                    }
                    //Only one column can be selected, only one file can be shown
                    if (columnSelected == -1)
                    {
                        List<int> selectedColumns = DataGridViewHandler.GetColumnSelected(dataGridView);
                        if (selectedColumns.Count != 1) { formRegionSelect.SetImageNone(errorMessag); return; } //Can only show one poster
                        columnSelected = selectedColumns[0];
                    }

                    DataGridViewSelectedCellCollection cellSelected = DataGridViewHandler.GetCellSelected(dataGridView);

                    if (DataGridViewHandler.GetCellSelectedCount(dataGridView) != 1 || dataGridViewName != LinkTabAndDataGridViewNamePeople)
                    {
                        DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, columnSelected);
                        formRegionSelect.SetImageText(dataGridViewGenericColumn.FileEntryAttribute.FileName);
                        Image image = LoadMediaCoverArtPosterWithCache(dataGridViewGenericColumn.FileEntryAttribute.FileFullPath);
                        formRegionSelect.SetImage(image, "Showing: " + dataGridViewGenericColumn.FileEntryAttribute.FileName, imageBoxSelectionMode);
                    }
                    else
                    {
                        
                        int rowIndex = cellSelected[0].RowIndex;
                        int columnIndex = cellSelected[0].ColumnIndex;
                        if (rowIndex < 0 || columnIndex < 0) { formRegionSelect.SetImageNone(errorMessag); return; }

                        DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, columnIndex);
                        if (dataGridViewGenericColumn == null || dataGridViewGenericColumn.ReadWriteAccess != ReadWriteAccess.AllowCellReadAndWrite) return;
                        //MessageBox.Show("You can only change region on current version on media file, not on historical or error log.", "Not correct column type", (KryptonMessageBoxButtons)MessageBoxButtons.OK);

                        List<int> selectedRows = DataGridViewHandler.GetRowSelected(dataGridView);
                        

                        int selectedRow = selectedRows[0];
                        DataGridViewGenericRow dataGridViewGenericRow = DataGridViewHandler.GetRowDataGridViewGenericRow(dataGridView, selectedRow);


                        if (selectedRows.Count != 1) { formRegionSelect.SetImageNone(errorMessag); return; }
                        if (dataGridViewGenericRow == null) { formRegionSelect.SetImageNone(errorMessag); return; }
                        if (dataGridViewGenericRow.IsHeader) { formRegionSelect.SetImageNone(errorMessag); return; }
                        if (dataGridViewGenericColumn.Metadata == null) { formRegionSelect.SetImageNone(errorMessag); return; }
                         

                        formRegionSelect.SetImageText(dataGridViewGenericColumn.Metadata.FileFullPath);
                        Image image = LoadMediaCoverArtPosterWithCache(dataGridViewGenericColumn.Metadata.FileFullPath);
                        if (image != null)
                        {
                            RegionStructure region = DataGridViewHandler.GetCellRegionStructure(dataGridView, columnIndex, rowIndex);
                            if (region != null)
                            {
                                formRegionSelect.SetImage(image, "Select region: " + region.Name, imageBoxSelectionMode, columnIndex, rowIndex);

                                Rectangle rectangleInImage = region.GetImageRegionPixelRectangle(image.Size);
                                RectangleF rectangleFInImage = new RectangleF((float)rectangleInImage.X, (float)rectangleInImage.Y, (float)rectangleInImage.Width, (float)rectangleInImage.Height);
                                formRegionSelect.SetSelection(rectangleFInImage);
                            } else
                            {
                                formRegionSelect.SetImage(image, "Select region: create a new region", imageBoxSelectionMode, columnIndex, rowIndex);
                            }
                        }
                        else
                        {
                            Logger.Warn("Region selector was not able to load poster.");
                            KryptonMessageBox.Show("Region selector was not able to load poster.", "Not able to load poster...", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Error, showCtrlCopy: true);
                        }
                        
                    }
                    
                }

            }
            catch (Exception ex)
            {
                Logger.Error(ex, "RegionSelectorLoadAndSelect");
                KryptonMessageBox.Show("Region selector was not able to start.\r\n\r\n" + ex.Message, "Syntax error...", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Error, showCtrlCopy: true);
            }
        }
        #endregion 

        #region FormRegionSelect - FormRegionSelect_OnRegionSelected
        private void FormRegionSelect_OnRegionSelected(object sender, RegionSelectedEventArgs e)
        {
            try
            {
                DataGridView dataGridView = GetActiveTabDataGridView();
                if (!dataGridView.Enabled) { formRegionSelect.SetImageNone("No valid media file is selected, and no data loaded."); return; }


                RectangleF region = RegionStructure.CalculateImageRegionAbstarctRectangle(e.ImageSize,
                    new Rectangle((int)e.Selection.X, (int)e.Selection.Y, (int)e.Selection.Width, (int)e.Selection.Height),
                    RegionStructureTypes.WindowsLivePhotoGallery);
                if (DataGridViewHandler.UpdateSelectedCellsWithNewRegion(dataGridView, e.ColumnIndex, region))
                {
                    UpdateRegionThumbnail(dataGridView);
                    DataGridViewHandler.InvalidateCellColumnHeader(dataGridView, e.ColumnIndex);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #region FormRegionSelect - CellEnter
        private void dataGridViewPeople_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            try
            {
                DataGridView dataGridView = dataGridViewPeople;
                RegionSelectorLoadAndSelect(dataGridView, e.RowIndex, e.ColumnIndex);
                DataGridViewHandler.InvalidateCellColumnHeader(dataGridView, e.ColumnIndex);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion

        #endregion

        #region ValitedatePastePeople
        private void ValitedatePastePeople(DataGridView dataGridView, string header)
        {
            try
            {
                Dictionary<CellLocation, DataGridViewGenericCell> updatedCells = new Dictionary<CellLocation, DataGridViewGenericCell>();
                DataGridViewSelectedCellCollection dataGridViewSelectedCellCollection = DataGridViewHandler.GetCellSelected(dataGridView);
                foreach (DataGridViewCell dataGridViewCell in dataGridViewSelectedCellCollection)
                {
                    DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, dataGridViewCell.ColumnIndex);
                    DataGridViewGenericRow dataGridViewGenericRow = DataGridViewHandler.GetRowDataGridViewGenericRow(dataGridView, dataGridViewCell.RowIndex);

                    if (dataGridViewGenericColumn == null)
                    {
                        //CheckRowAndSetDefaults(dataGridView, dataGridViewCell.ColumnIndex, dataGridViewCell.RowIndex);
                    }
                    else if (dataGridViewGenericRow == null)
                    {
                        CheckRowAndSetDefaults(dataGridView, dataGridViewCell.ColumnIndex, dataGridViewCell.RowIndex);
                    }
                    else
                    {
                        if (dataGridViewCell.Value != null) PeopleRenameCell(dataGridView, dataGridViewCell, dataGridViewCell.Value.ToString(), updatedCells);
                    }
                }

                if (updatedCells != null && updatedCells.Count > 0) ClipboardUtility.PushToUndoStack(dataGridView, updatedCells);

                DataGridView_UpdatedDirtyFlags(dataGridView);
                DataGridViewHandler.Refresh(dataGridView);
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
            }
        }
        #endregion
    }
}
