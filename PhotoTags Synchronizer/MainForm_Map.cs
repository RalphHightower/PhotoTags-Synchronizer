﻿using System;
using System.Windows.Forms;
using CefSharp;
using System.Globalization;
using CameraOwners;
using LocationNames;
using DataGridViewGeneric;
using System.Collections.Generic;
using Krypton.Toolkit;

namespace PhotoTagsSynchronizer
{

    public partial class MainForm : KryptonForm
    {        
        #region LocationCoordinate ParseCoordinateFromURL(string text)
        private LocationCoordinate ParseCoordinateFromURL(string text)
        {
            /*
            Normal:                                                     #map=15/59.9415/10.6785&layers=N
            Note:       note/new?   lat=59.9419&lon=10.6814             #map=15/59.9415/10.6861&layers=N
            Center:     query?      lat=59.9420&lon=10.6847             #map=15/59.9421/10.6805&layers=N
            Address:    search?whereami=1&  query=59.9417%2C10.6740     #map=15/59.9417/10.6740&layers=N
            Search:     search?             query=59.9417%2C10.6740     #map=15/59.9417/10.6740&layers=N
            */
            LocationCoordinate locationCoordinate = null;

            if (text.ToLower().StartsWith("https://www.openstreetmap.org/"))
            {
                string url = text;
                string string_lon;
                string string_lat;
                int index_sperator;
                int index_lat = url.IndexOf("lat=");
                if (index_lat >= 0)
                {
                    index_sperator = url.IndexOf("&", index_lat);
                    if (index_sperator <= index_lat) index_sperator = url.IndexOf("%2C", index_lat);
                    if (index_sperator <= index_lat) return locationCoordinate; //Not found, can't find values

                    string_lat = url.Substring(index_lat + 4, index_sperator - index_lat - 4);

                    int index_lon = url.IndexOf("lon=");
                    if (index_lon <= index_lat) return locationCoordinate;
                    index_sperator = url.IndexOf("#map", index_lon);
                    if (index_sperator == -1) index_sperator = url.Length; //Not found, can't find values
                    string_lon = url.Substring(index_lon + 4, index_sperator - index_lon - 4);
                }
                else
                {
                    const int coordLength = 11;
                    index_lat = url.IndexOf("query=") + 6;
                    if (index_lat >= 0 && index_lat + coordLength <= url.Length)
                    {
                        index_sperator = url.IndexOf("%2C", index_lat, coordLength);
                        if (index_sperator == -1) index_sperator = url.IndexOf("&", index_lat, 9);
                        if (index_sperator == -1) return locationCoordinate; //Not found, can't find seperator
                        string_lat = url.Substring(index_lat, index_sperator - index_lat);

                        int index_lon = -1;
                        if (url.IndexOf("%2C", index_lat, 11) != -1) index_lon = index_sperator + 3;
                        if (url.IndexOf("&", index_lat, 9) != -1) index_lon = index_sperator + 1;
                        if (index_lon == -1) return locationCoordinate;

                        index_sperator = url.IndexOf("#map", index_lon);
                        if (index_sperator == -1) index_sperator = url.Length;

                        string_lon = url.Substring(index_lon, index_sperator - index_lon);
                    }
                    else return locationCoordinate;
                }

                float lat;
                float lon;
                if (float.TryParse(string_lon, NumberStyles.Float, CultureInfo.InvariantCulture, out lon) &&
                    float.TryParse(string_lat, NumberStyles.Float, CultureInfo.InvariantCulture, out lat))
                {
                    locationCoordinate = new LocationCoordinate(lat, lon);
                }
            }

            if (text.ToLower().StartsWith("https://www.google.com/maps/"))
            {
                string[] split = text.Split('@');
                if (split.Length >= 2)
                {
                    string[] coordinates = split[1].Split(',');
                    if (coordinates.Length >= 2)
                    {
                        float lat;
                        float lon;
                        if (float.TryParse(coordinates[1], NumberStyles.Float, CultureInfo.InvariantCulture, out lon) &&
                            float.TryParse(coordinates[0], NumberStyles.Float, CultureInfo.InvariantCulture, out lat))
                        {
                            locationCoordinate = new LocationCoordinate(lat, lon);
                        }
                    }
                }
            }
            return locationCoordinate;
        }
        #endregion 

        #region WebBrowser - event handling
        delegate void SetTextBoxURLCallback(string text);

        #region textBoxBrowserURL_KeyPress
        private void textBoxBrowserURL_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                e.Handled = true; //Handle the Keypress event (suppress the Beep)
                try
                {
                    if (browser != null) browser.Load(textBoxBrowserURL.Text);
                } 
                catch (Exception ex)
                {
                    KryptonMessageBox.Show(
                        (GlobalData.isRunningWinSmode ? "Your Windows is running Windows 10 S / 11 S mode.\r\n" + 
                        "The Chromium Web Browser doesn't support this mode.\r\n\r\n" : "") +
                        ex.Message, "Can't run the Web Browser", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Error, showCtrlCopy: true);
                }
            }
        }
        #endregion

        #region PopulateDataGridViewMapWithBrowserCoordinate
        private void PopulateDataGridViewMapWithBrowserCoordinate(DataGridView dataGridView, string text)
        {
            if (dataGridView.CurrentCell == null && dataGridView.SelectedCells.Count < 1) return;

            LocationCoordinate locationCoordinate = ParseCoordinateFromURL(text);

            if (locationCoordinate != null)
            {
                List<int> selectedColumns = DataGridViewHandler.GetColumnSelected(dataGridView);

                foreach (int columnIndex in selectedColumns)
                {
                    DataGridViewHandler.SetCellValue(dataGridView, columnIndex,
                        DataGridViewHandlerMap.headerBrowser, DataGridViewHandlerMap.tagMediaCoordinates, locationCoordinate.ToString());
                }

            }
        }
        #endregion

        #region OnBrowserAddressChanged
        private void OnBrowserAddressChanged(object sender, AddressChangedEventArgs e)
        {
            SetTextBoxBrowserURLText(e.Address);
        }
        #endregion

        #region SetTextBoxBrowserURLText
        private void SetTextBoxBrowserURLText(string text)
        {
            // InvokeRequired required compares the thread ID of the
            // calling thread to the thread ID of the creating thread.
            // If these threads are different, it returns true.
            try
            {
                DataGridView dataGridView = dataGridViewMap;
                if (this.textBoxBrowserURL.InvokeRequired)
                {
                    SetTextBoxURLCallback d = new SetTextBoxURLCallback(SetTextBoxBrowserURLText);
                    this.Invoke(d, new object[] { text });
                }
                else
                {
                    this.textBoxBrowserURL.Text = text;
                    PopulateDataGridViewMapWithBrowserCoordinate(dataGridView, text);
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "PopulateGrivViewMapMediaFile.");
                //throw; Windows closing, this will cause error. But ok...
            }
        }
        #endregion

        #region GetZoomLevel()
        private int GetZoomLevel()
        {
            return comboBoxMapZoomLevel.SelectedIndex + 1;
        }
        #endregion

        LocationCoordinate locationCoordinateRememberForZooming = null;
        #region UpdateBrowserMap - Only when one cell selected
        private void UpdateBrowserMap(string combinedCorordinateString, MapProvider mapProvider)
        {
            if (ClipboardUtility.IsClipboardActive && ClipboardUtility.NuberOfItemsToEdit>1) return;
            DataGridView dataGridView = dataGridViewMap;
            if (DataGridViewHandler.GetCellSelectedCount(dataGridView) == 1) //Only updated the Browser Map when one cell are updated
            {
                LocationCoordinate locationCoordinate = LocationCoordinate.Parse(combinedCorordinateString);

                if (locationCoordinate != null)
                {
                    locationCoordinateRememberForZooming = new LocationCoordinate(locationCoordinate.Latitude, locationCoordinate.Longitude);
                    try
                    {
                        ShowMediaOnMap.UpdateBrowserMap(browser, locationCoordinate, GetZoomLevel(), mapProvider);
                    }
                    catch (Exception ex)
                    {
                        KryptonMessageBox.Show(
                            (GlobalData.isRunningWinSmode ? "Your Windows is running Windows 10 S / 11 S mode.\r\n" +
                            "The Chromium Web Browser doesn't support this mode.\r\n\r\n" : "") +
                            ex.Message, "Syntax Error", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Error, showCtrlCopy: true);
                    }
                }
            }
        }
        #endregion

        #region MapProvider GetMapProvider
        private MapProvider GetMapProvider()
        {
            return ShowMediaOnMap.GetMapProvider(textBoxBrowserURL.Text);
        }
        #endregion

        #region GetLocationAndShow
        private void GetLocationAndShow(MapProvider mapProvider)
        {
            List<LocationCoordinate> locationCoordinates = new List<LocationCoordinate>();

            DataGridView dataGridView = dataGridViewMap;
            foreach (DataGridViewCell dataGridViewCell in dataGridView.SelectedCells)
            {

                if (LocationCoordinate.TryParse(DataGridViewHandler.GetCellValueNullOrStringTrim(dataGridView, dataGridViewCell.ColumnIndex, dataGridViewCell.RowIndex), out LocationCoordinate locationCoordinate))
                {
                    if (!locationCoordinates.Contains(locationCoordinate)) locationCoordinates.Add(locationCoordinate);
                }
            }
            try
            {
                ShowMediaOnMap.UpdatedBroswerMap(browser, locationCoordinates, GetZoomLevel(), mapProvider);
            }
            catch (Exception ex)
            {
                KryptonMessageBox.Show(
                    (GlobalData.isRunningWinSmode ? "Your Windows is running Windows 10 S / 11 S mode.\r\n" +
                    "The Chromium Web Browser doesn't support this mode.\r\n\r\n" : "") +
                    ex.Message, "Syntax Error", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Error, showCtrlCopy: true);
            }
        }
        #endregion

        #region DataGridView - Map - ShowCoordinate On OpenStreetMap_Click

        private void KryptonContextMenuItemMapShowCoordinateOnOpenStreetMap_Click(object sender, EventArgs e)
        {
            GetLocationAndShow(MapProvider.OpenStreetMap);
        }
        #endregion 

        #region DataGridView - Map - ShowCoordinate On GoogleMap_Click
        private void KryptonContextMenuItemMapShowCoordinateOnGoogleMap_Click(object sender, EventArgs e)
        {
            GetLocationAndShow(MapProvider.GoogleMap);
        }
        #endregion

        #endregion

        #region UpdateGoodleHistoryCoordinate after Select new TimeZoneShift or AccepedIntervalSecound

        #region GetAccepedIntervalSecound()
        private int GetAccepedIntervalSecound()
        {
            int accepedIntervalSecound;
            switch (comboBoxGoogleLocationInterval.SelectedIndex)
            {
                case 0:
                    accepedIntervalSecound = 60; // 1 minute
                    break;
                case 1:
                    accepedIntervalSecound = 60 * 5; //5 minutes
                    break;
                case 2:
                    accepedIntervalSecound = 60 * 30; //30 minutes
                    break;
                case 3:
                    accepedIntervalSecound = 60 * 60; //1 hour
                    break;
                case 4:
                    accepedIntervalSecound = 60 * 60 * 24; //1 day
                    break;
                case 5:
                    accepedIntervalSecound = 60 * 60 * 24 * 2; //2 days
                    break;
                case 6:
                    accepedIntervalSecound = 60 * 60 * 24 * 10; //2 days
                    break;
                default:
                    accepedIntervalSecound = 3600;
                    break;
            }
            return accepedIntervalSecound;
        }
        #endregion

        #region GetTimeZoneShift()
        private int GetTimeZoneShift()
        {
            return comboBoxGoogleTimeZoneShift.SelectedIndex - 12;
        }
        #endregion

        #region UpdateGoodleHistoryCoordinateAndNearBy
        private void UpdateGoodleHistoryCoordinateAndNearBy(int columnIndex)
        {
            DataGridView dataGridView = dataGridViewMap;
            if (!DataGridViewHandler.GetIsAgregated(dataGridView)) return;


            DataGridViewHandlerMap.TimeZoneShift = GetTimeZoneShift();
            DataGridViewHandlerMap.AccepedIntervalSecound = GetAccepedIntervalSecound();

            DataGridViewHandlerMap.PopulateGoogleHistoryCoordinateAndNearby(dataGridView, dataGridViewDate, columnIndex, DataGridViewHandlerMap.TimeZoneShift, DataGridViewHandlerMap.AccepedIntervalSecound);
        }
        private void UpdateGoodleHistoryCoordinateAndNearBy()
        {
            DataGridView dataGridView = dataGridViewMap;
            if (!DataGridViewHandler.GetIsAgregated(dataGridView)) return;

            DataGridViewHandlerMap.TimeZoneShift = GetTimeZoneShift();
            DataGridViewHandlerMap.AccepedIntervalSecound = GetAccepedIntervalSecound();

            
            for (int columnIndex = 0; columnIndex < DataGridViewHandler.GetColumnCount(dataGridView); columnIndex++)
            {
                DataGridViewHandlerMap.PopulateGoogleHistoryCoordinateAndNearby(dataGridView, dataGridViewDate, columnIndex, DataGridViewHandlerMap.TimeZoneShift, DataGridViewHandlerMap.AccepedIntervalSecound);
            }
            
        }
        #endregion

        #endregion

        #region User control handling

        #region comboBoxGoogleTimeZoneShift_SelectedIndexChanged
        private void comboBoxGoogleTimeZoneShift_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (GlobalData.IsApplicationClosing) return;
            if (isSettingDefaultComboxValues) return;
            if (GlobalData.IsPopulatingMap) return;

            Properties.Settings.Default.ComboBoxGoogleTimeZoneShift = comboBoxGoogleTimeZoneShift.SelectedIndex;
            
            UpdateGoodleHistoryCoordinateAndNearBy();

            if (dataGridViewMap.CurrentCell != null && dataGridViewMap.CurrentCell.Value != null) UpdateBrowserMap(dataGridViewMap.CurrentCell.Value.ToString(), GetMapProvider());
        }
        #endregion 

        #region comboBoxGoogleLocationInterval_SelectedIndexChanged
        private void comboBoxGoogleLocationInterval_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (GlobalData.IsApplicationClosing) return;
            if (isSettingDefaultComboxValues) return;
            if (GlobalData.IsPopulatingMap) return;
            if (dataGridViewMap.CurrentCell == null) return;

            Properties.Settings.Default.ComboBoxGoogleLocationInterval = comboBoxGoogleLocationInterval.SelectedIndex;    //30 minutes Index 2
            
            UpdateGoodleHistoryCoordinateAndNearBy();

            if (dataGridViewMap.CurrentCell.Value != null) UpdateBrowserMap(dataGridViewMap.CurrentCell.Value.ToString(), GetMapProvider());
        }
        #endregion

        #region comboBoxMapZoomLevel_SelectedIndexChanged
        private void comboBoxMapZoomLevel_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (GlobalData.IsApplicationClosing) return;
            if (isSettingDefaultComboxValues) return;
            if (GlobalData.IsPopulatingMap) return;

            Properties.Settings.Default.ComboBoxMapZoomLevel = comboBoxMapZoomLevel.SelectedIndex;
            try
            {
                if (locationCoordinateRememberForZooming != null) ShowMediaOnMap.UpdateBrowserMap(browser, locationCoordinateRememberForZooming, GetZoomLevel(), GetMapProvider()); //Use last valid coordinates clicked
            }
            catch (Exception ex)
            {
                KryptonMessageBox.Show(
                    (GlobalData.isRunningWinSmode ? "Your Windows is running Windows 10 S / 11 S mode.\r\n" +
                    "The Chromium Web Browser doesn't support this mode.\r\n\r\n" : "") +
                    ex.Message, "Syntax Error", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Error, showCtrlCopy: true);
            }
        }
        #endregion 

        #endregion

        #region DataGridView - Map - CellEnter
        private void dataGridViewMap_CellEnter(object sender, DataGridViewCellEventArgs e)
        {
            DataGridView dataGridView = dataGridViewMap;
            RegionSelectorLoadAndSelect(dataGridView, e.RowIndex, e.ColumnIndex);
        }
        #endregion

        #region DataGridView - Map - CellMouseClick
        private void dataGridViewMap_CellMouseClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            DataGridView dataGridView = ((DataGridView)sender);
            if (e.RowIndex == -1) RegionSelectorLoadAndSelect(dataGridView, e.RowIndex, e.ColumnIndex);
        }
        #endregion

        #region DataGridView - Map - Cell Mouse *Double* Click
        private void dataGridViewMap_CellMouseDoubleClick(object sender, DataGridViewCellMouseEventArgs e)
        {
            if (e.RowIndex <= 0 && e.ColumnIndex > -1)
            {
                ActionFileSystemVerbOpen(e.ColumnIndex, e.RowIndex);
                return;
            }
            DataGridView dataGridView = ((DataGridView)sender);
            
            if (!dataGridView.Enabled) return;
            if (e.ColumnIndex < 0 || e.RowIndex < 0) return;
            if (dataGridView.SelectedCells.Count > 1) return;

            if (dataGridViewMap[e.ColumnIndex, e.RowIndex].Value != null)
            {
                UpdateBrowserMap(DataGridViewHandler.GetCellValueNullOrStringTrim(dataGridView, e.ColumnIndex, e.RowIndex), GetMapProvider());
            }
        }
        #endregion

        #region DataGridView - Map - CellValueChanged
        private bool isDataGridViewMaps_CellValueChanging = false;
        private void dataGridViewMap_CellValueChanged(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex < 0) return;
            if (e.RowIndex < 0) return;

            if (isDataGridViewMaps_CellValueChanging) return; //Avoid requirng isues
            if (GlobalData.IsApplicationClosing) return;
            
            DataGridView dataGridView = ((DataGridView)sender);
            if (!dataGridView.Enabled) return;
            if (GlobalData.IsPopulatingMapLocation) return; 
            if (DataGridViewHandler.GetIsPopulatingFile(dataGridView)) return;
            if (DataGridViewHandler.GetIsPopulating(dataGridView)) return;
            

            DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, e.ColumnIndex);
            if (dataGridViewGenericColumn?.Metadata == null) return;
            DataGridViewGenericRow gridViewGenericRow = DataGridViewHandler.GetRowDataGridViewGenericRow(dataGridView, e.RowIndex);

            isDataGridViewMaps_CellValueChanging = true;
            ///////////////////////////////////////////////////////////////////////////
            /// Coordinate changes, updated Nomnatatim address 
            ///////////////////////////////////////////////////////////////////////////
            if (gridViewGenericRow.HeaderName.Equals(DataGridViewHandlerMap.headerMedia) &&
                gridViewGenericRow.RowName.Equals(DataGridViewHandlerMap.tagMediaCoordinates))
            {
                string coordinate = DataGridViewHandler.GetCellValueNullOrStringTrim(dataGridViewMap, e.ColumnIndex, e.RowIndex);
                UpdateBrowserMap(coordinate, GetMapProvider());

                DataGridViewGenericColumn dataGridViewGenericColumnLookup = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridViewMap, e.ColumnIndex);
                AddQueueLazyLoadingMapNomnatatimLock(dataGridViewGenericColumnLookup.FileEntryAttribute, forceReloadUsingReverseGeocoder: false);

                DataGridViewHandlerDate.PopulateTimeZone(dataGridViewDate, null, dataGridViewGenericColumn.FileEntryAttribute);
            }

            ///////////////////////////////////////////////////////////////////////////
            /// Camera make and model owner changed, upated all fields
            ///////////////////////////////////////////////////////////////////////////

            else if (gridViewGenericRow.HeaderName.Equals(DataGridViewHandlerMap.headerGoogleLocations) &&
                gridViewGenericRow.RowName.Equals(DataGridViewHandlerMap.tagCameraOwner))
            {
                string selectedCameraOwner = DataGridViewHandlerMap.GetUserInputCameraOwner(dataGridView, e.ColumnIndex);
                
                DataGridViewHandlerMap.SetCameraOwner(dataGridView, e.ColumnIndex, selectedCameraOwner);
                if (!string.IsNullOrWhiteSpace(selectedCameraOwner))
                {
                    if (dataGridViewGenericColumn.Metadata != null)
                    {
                        CameraOwner cameraOwner = new CameraOwner(
                            dataGridViewGenericColumn.Metadata.CameraMake,
                            dataGridViewGenericColumn.Metadata.CameraModel,
                            selectedCameraOwner);

                        databaseAndCahceCameraOwner.SaveCameraMakeModelAndOwner(cameraOwner);
                        databaseAndCahceCameraOwner.CameraMakeModelAndOwnerMakeDirty();
                        
                        for (int columnIndex = 0; columnIndex < DataGridViewHandler.GetColumnCount(dataGridViewMap); columnIndex++)
                        {
                            DataGridViewGenericColumn gridViewGenericColumnCheck = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, columnIndex);

                            if (gridViewGenericColumnCheck?.Metadata == null)
                            {
                                //DEBUG
                            }
                            if (dataGridViewGenericColumn.Metadata == null)
                            {
                                //DEBUG
                            }
                            if (gridViewGenericColumnCheck?.Metadata?.CameraMake == dataGridViewGenericColumn.Metadata.CameraMake &&
                                gridViewGenericColumnCheck?.Metadata?.CameraModel == dataGridViewGenericColumn.Metadata.CameraModel)
                            {
                                DataGridViewHandlerMap.PopulateCameraOwner(dataGridView, columnIndex, gridViewGenericColumnCheck.ReadWriteAccess,
                                    gridViewGenericColumnCheck?.Metadata?.CameraMake, gridViewGenericColumnCheck?.Metadata?.CameraModel);
                                DataGridViewHandlerMap.PopulateGoogleHistoryCoordinateAndNearby(dataGridView, dataGridViewDate, columnIndex, GetTimeZoneShift(), GetAccepedIntervalSecound());

                            }
                        }
                    }
                }
            }

            ///////////////////////////////////////////////////////////////////////////
            /// Nomnatatim
            ///////////////////////////////////////////////////////////////////////////
            float locationAccuracyLatitude = Properties.Settings.Default.LocationAccuracyLatitude;
            float locationAccuracyLongitude = Properties.Settings.Default.LocationAccuracyLongitude;
            if (gridViewGenericRow.HeaderName.Equals(DataGridViewHandlerMap.headerNominatim))
            {
                LocationCoordinate locationCoordinateNomnatatim = DataGridViewHandlerMap.GetUserInputLocationCoordinate(dataGridViewMap, e.ColumnIndex, null);
                bool createNewAccurateLocation = DataGridViewHandlerMap.GetUserInputIsCreateNewAccurateLocationUsingSearchLocation(dataGridViewMap, e.ColumnIndex, null);
 
                if (locationCoordinateNomnatatim != null)
                {
                    #region Get Coordinateds enter by user
                    LocationCoordinate locationCoordinateSearch = new LocationCoordinate(
                        (float)locationCoordinateNomnatatim.Latitude,
                        (float)locationCoordinateNomnatatim.Longitude);
                    #endregion

                    #region Get Coordinates use to store in database
                    LocationCoordinateAndDescription locationCoordinateAndDescriptionFromDatabase = databaseLocationNameAndLookUp.ReadLocationNameFromDatabaseOrCache(
                        locationCoordinateSearch, locationAccuracyLatitude, locationAccuracyLongitude);
                    #endregion

                    #region Find nearby location in Database
                    LocationCoordinate locationCoordinateFromDatabase;
                    if (locationCoordinateAndDescriptionFromDatabase != null && !createNewAccurateLocation)
                        locationCoordinateFromDatabase = locationCoordinateAndDescriptionFromDatabase.Coordinate; //If exist, updated
                    else
                        locationCoordinateFromDatabase = locationCoordinateSearch; //If not, create new

                    LocationCoordinateAndDescription locationCoordinateAndDescriptionUpdated =
                        new LocationCoordinateAndDescription
                        (
                            locationCoordinateSearch,
                            new LocationDescription(
                                (string)DataGridViewHandler.GetCellValue(dataGridView, e.ColumnIndex, DataGridViewHandlerMap.headerNominatim, DataGridViewHandlerMap.tagLocationName), //Name
                                (string)DataGridViewHandler.GetCellValue(dataGridView, e.ColumnIndex, DataGridViewHandlerMap.headerNominatim, DataGridViewHandlerMap.tagCity), //City
                                (string)DataGridViewHandler.GetCellValue(dataGridView, e.ColumnIndex, DataGridViewHandlerMap.headerNominatim, DataGridViewHandlerMap.tagProvince), //State
                                (string)DataGridViewHandler.GetCellValue(dataGridView, e.ColumnIndex, DataGridViewHandlerMap.headerNominatim, DataGridViewHandlerMap.tagCountry)) //Country
                        );
                    #endregion

                    #region Updated the database
                    databaseLocationNameAndLookUp.AddressUpdate(locationCoordinateFromDatabase,
                        locationCoordinateAndDescriptionUpdated, locationAccuracyLatitude, locationAccuracyLongitude);
                    #endregion

                    #region Updated DataGridView with new data
                    for (int columnIndex = 0; columnIndex < dataGridViewMap.ColumnCount; columnIndex++)
                    {
                        DataGridViewGenericColumn dataGridViewGenericColumnLookup = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridViewMap, columnIndex);
                        AddQueueLazyLoadingMapNomnatatimLock(dataGridViewGenericColumnLookup.FileEntryAttribute, forceReloadUsingReverseGeocoder: false);
                    }
                    #endregion 
                }
            }

            isDataGridViewMaps_CellValueChanging = false;
        }
        #endregion


        #region Map - Save Exact Location
        private void KryptonContextMenuItemMapSaveExactLocation_Click(object sender, EventArgs e)
        {
            DataGridView dataGridView = dataGridViewMap;
            List<int> selectedColumns = DataGridViewHandler.GetColumnSelected(dataGridView);
            foreach (int columnIndex in selectedColumns)
            {
                LocationCoordinate locationCoordinate = DataGridViewHandlerMap.GetUserInputLocationCoordinate(dataGridView, columnIndex, null);
                if (locationCoordinate != null)
                {
                    DataGridViewHandlerMap.SetLocationCoordinate(dataGridView, columnIndex, locationCoordinate.ToString() + "+"); //Trigger dataGridViewMap_CellValueChanged
                }
            }
        }
        #endregion

        #region Map - ReloadUsingNominatim_Click
        private void KryptonContextMenuItemMapReloadUsingNominatim_Click(object sender, EventArgs e)
        {
            
            DataGridView dataGridView = dataGridViewMap;
            List<int> selectedColumns = DataGridViewHandler.GetColumnSelected(dataGridView);
            if (selectedColumns.Count == 0) return;

            isDataGridViewMaps_CellValueChanging = true;
            int rowIndex = DataGridViewHandler.GetRowIndex(dataGridView, DataGridViewHandlerMap.headerMedia, DataGridViewHandlerMap.tagMediaCoordinates);

            //Reload data from Nomnatatim or if from database in case equal
            foreach (int columnIndex in selectedColumns)
            {
                DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, columnIndex);
                if (dataGridViewGenericColumn != null && dataGridViewGenericColumn?.FileEntryAttribute != null)
                {
                    AddQueueLazyLoadingMapNomnatatimLock(dataGridViewGenericColumn?.FileEntryAttribute, forceReloadUsingReverseGeocoder: true);
                }
            }
            isDataGridViewMaps_CellValueChanging = false;
        }
        #endregion 
    }
}
