﻿using CameraOwners;
using DataGridViewGeneric;
using GoogleLocationHistory;
using LocationNames;
using MetadataLibrary;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using Thumbnails;
using Krypton.Toolkit;

namespace PhotoTagsSynchronizer
{
    public static class DataGridViewHandlerMap
    {
        public static bool HasBeenInitialized { get; set; } = false;
        public static ThumbnailPosterDatabaseCache DatabaseAndCacheThumbnail { get; set; }
        public static MetadataDatabaseCache DatabaseAndCacheMetadataExiftool { get; set; }
        public static MetadataDatabaseCache DatabaseAndCacheMetadataMicrosoftPhotos { get; set; }
        public static MetadataDatabaseCache DatabaseAndCacheMetadataWindowsLivePhotoGallery { get; set; }
        public static LocationNameDatabaseAndLookUpCache DatabaseAndCacheLocationAddress { get; set; }
        public static CameraOwnersDatabaseCache DatabaseAndCacheCameraOwner { get; set; }
        public static GoogleLocationHistoryDatabaseCache DatabaseGoogleLocationHistory {get; set; }
        public static List<AutoKeywordConvertion> AutoKeywordConvertions { get; set; }
        public static int TimeZoneShift { get; set; } = 0;
        public static int AccepedIntervalSecound { get; set; } = 3600;

        public const string headerMedia = "Media";
        public const string headerMicrosoftPhotos = "Microsoft Photos";
        public const string headerWindowsLivePhotoGallery = "Windows Live Photo Gallery";
        public const string headerGoogleLocations = "Google Locations";
        public const string headerNearByLocations = "Near by photos";
        public const string headerWebScraping = "WebScraper";
        public const string headerNominatim = "Nominatim";        
        public const string headerBrowser = "Browser map";

        public const string tagMediaCoordinates          = "Coordinates"; //MapFastCopyTextAndOverwrite_Click() use String.StartsWith this for fast copy
        public const string tagExternalCoordinates       = "Coordinates";
        public const string tagGoogleCoordinateUTC       = "Coordinates UTC";
        public const string tagGoogleCoordinateDateTaken = "Coordinates DateTaken";
        public const string tagCoordinatesNearByPhotos   = "Coordinates";
        public const string tagCameraMakeModel = "Camera make/model";
        public const string tagCameraOwner = "Camera owner";
        public const string tagLocationName = "Location name";
        public const string tagCity = "City";
        public const string tagProvince = "Province";
        public const string tagCountry = "Country";

        #region GetUserInputChanges
        public static void GetUserInputChanges(DataGridView dataGridView, ref Metadata metadata, FileEntryAttribute fileEntryAttribute, int columnIndex = -1)
        {
            if (fileEntryAttribute == null && columnIndex == -1)
            {
                //DEBUG
                return;
            }
            if (columnIndex == -1) columnIndex = DataGridViewHandler.GetColumnIndexUserInput(dataGridView, fileEntryAttribute);
            if (columnIndex == -1) return; //Column has not yet become aggregated or has already been removed
            if (!DataGridViewHandler.IsColumnPopulated(dataGridView, columnIndex)) return;

            LocationCoordinate.TryParse(DataGridViewHandler.GetCellValueNullOrStringTrim(dataGridView, columnIndex, headerMedia, tagMediaCoordinates), out LocationCoordinate locationCoordinate);
            metadata.LocationCoordinate = locationCoordinate;

            metadata.LocationName = (string)DataGridViewHandler.GetCellValue(dataGridView, columnIndex, headerMedia, tagLocationName);
            if (metadata.LocationName != null) metadata.LocationName = metadata.LocationName.Trim();
            if (string.IsNullOrWhiteSpace(metadata.LocationName)) metadata.LocationName = null;

            metadata.LocationCity = (string)DataGridViewHandler.GetCellValue(dataGridView, columnIndex, headerMedia, tagCity);
            if (metadata.LocationCity != null) metadata.LocationCity = metadata.LocationCity.Trim();
            if (string.IsNullOrWhiteSpace(metadata.LocationCity)) metadata.LocationCity = null;

            metadata.LocationState = (string)DataGridViewHandler.GetCellValue(dataGridView, columnIndex, headerMedia, tagProvince);
            if (metadata.LocationState != null) metadata.LocationState = metadata.LocationState.Trim();
            if (string.IsNullOrWhiteSpace(metadata.LocationState)) metadata.LocationState = null;

            metadata.LocationCountry = (string)DataGridViewHandler.GetCellValue(dataGridView, columnIndex, headerMedia, tagCountry);
            if (metadata.LocationCountry != null) metadata.LocationCountry = metadata.LocationCountry.Trim();
            if (string.IsNullOrWhiteSpace(metadata.LocationCountry)) metadata.LocationCountry = null;
        }
        #endregion 

        #region Add Row
        private static int AddRow(DataGridView dataGridView, int columnIndex, DataGridViewGenericRow dataGridViewGenericDataRow)
        {
            return DataGridViewHandler.AddRow(dataGridView, columnIndex, dataGridViewGenericDataRow, false);
        }

        private static int AddRow(DataGridView dataGridView, int columnIndex, DataGridViewGenericRow dataGridViewGenericDataRow, object value, bool cellReadOnly)
        {
            int rowIndex = DataGridViewHandler.AddRow(dataGridView, columnIndex, dataGridViewGenericDataRow, value,
                new DataGridViewGenericCellStatus(MetadataBrokerType.Empty, SwitchStates.Disabled, cellReadOnly), false);            
            return rowIndex;
        }
        #endregion

        #region GetCameraOwner
        public static string GetUserInputCameraOwner(DataGridView dataGridViewMap, int? columnIndex)
        {
            if (!DataGridViewHandler.GetIsAgregated(dataGridViewMap)) return null;
            //Don't do check if (!DataGridViewHandler.IsColumnPopulated(dataGridViewMap, (int)columnIndex)) return null; This is always during populatig
            
            string cameraOwner = (string)DataGridViewHandler.GetCellValue(dataGridViewMap, (int)columnIndex, headerGoogleLocations, tagCameraOwner);
            if (cameraOwner == CameraOwnersDatabaseCache.MissingLocationsOwners) cameraOwner = null;
            return cameraOwner;
        }
        #endregion

        #region SetCameraOwner
        public static void SetCameraOwner(DataGridView dataGridView, int columnIndex, string value)
        {
            DataGridViewHandler.SetCellValue(dataGridView, columnIndex, headerGoogleLocations, tagCameraOwner, value);
        }
        #endregion

        #region SetLocationCoordinate
        public static void SetLocationCoordinate(DataGridView dataGridView, int columnIndex, string value)
        {
            DataGridViewHandler.SetCellValue(dataGridView, columnIndex, headerMedia, tagMediaCoordinates, value);
        }
        #endregion

        #region GetLocationCoordinate
        public static LocationCoordinate GetUserInputLocationCoordinate(DataGridView dataGridViewMap, int? columnIndex, FileEntryAttribute fileEntryAttribute)
        {
            if (!DataGridViewHandler.GetIsAgregated(dataGridViewMap)) return null;
            if (columnIndex == null) columnIndex = DataGridViewHandler.GetColumnIndexUserInput(dataGridViewMap, fileEntryAttribute);
            if (columnIndex == -1) return null;
            if (!DataGridViewHandler.IsColumnPopulated(dataGridViewMap, (int)columnIndex)) return null;
            
            LocationCoordinate locationCoordinate;            
            string locationCoordinateString = DataGridViewHandler.GetCellValueNullOrStringTrim(dataGridViewMap, (int)columnIndex, headerMedia, tagMediaCoordinates);
            if (!string.IsNullOrEmpty(locationCoordinateString)) locationCoordinateString = locationCoordinateString.TrimEnd('+');
            locationCoordinate = LocationCoordinate.Parse(locationCoordinateString);
            return locationCoordinate;
        }

        public static bool GetUserInputIsCreateNewAccurateLocationUsingSearchLocation(DataGridView dataGridViewMap, int? columnIndex, FileEntryAttribute fileEntryAttribute)
        {
            if (!DataGridViewHandler.GetIsAgregated(dataGridViewMap)) return false;
            if (columnIndex == null) columnIndex = DataGridViewHandler.GetColumnIndexUserInput(dataGridViewMap, fileEntryAttribute);
            if (columnIndex == -1) return false;
            if (!DataGridViewHandler.IsColumnPopulated(dataGridViewMap, (int)columnIndex)) return false;
            string locationCoordinateString = DataGridViewHandler.GetCellValueNullOrStringTrim(dataGridViewMap, (int)columnIndex, headerMedia, tagMediaCoordinates);

            if (string.IsNullOrEmpty(locationCoordinateString)) return false;
            LocationCoordinate locationCoordinate = LocationCoordinate.Parse(locationCoordinateString.TrimEnd('+'));
            if (locationCoordinate == null) return false; 

            return locationCoordinateString.EndsWith("+");
        }
        #endregion 

        #region PopulateGrivViewMapCameraOwner
        private static DataGridViewComboBoxCell dataGridViewComboBoxCellCameraOwners = null;

        public static void PopulateCameraOwner(DataGridView dataGridView, int columnIndex, ReadWriteAccess readWriteAccessColumn, string cameraMake, string cameraModel)
        {
            int rowIndex = DataGridViewHandler.GetRowIndex(dataGridView, headerGoogleLocations, tagCameraOwner);
            string cameraOwner = DatabaseAndCacheCameraOwner.GetOwenerForCameraMakeModel(cameraMake, cameraModel);

            if (dataGridViewComboBoxCellCameraOwners == null || DatabaseAndCacheCameraOwner.IsCameraMakeModelAndOwnerDirty())
            {
                //Create or updated common dropbox for all cells with "List of camera owners"
                dataGridViewComboBoxCellCameraOwners = null;
                dataGridViewComboBoxCellCameraOwners = new DataGridViewComboBoxCell();
                dataGridViewComboBoxCellCameraOwners.FlatStyle = FlatStyle.Flat;
                dataGridViewComboBoxCellCameraOwners.Items.AddRange(DatabaseAndCacheCameraOwner.ReadCameraOwners().ToArray());
            }

            if (readWriteAccessColumn == ReadWriteAccess.AllowCellReadAndWrite)
            {                
                DataGridViewHandler.SetCellControlType(dataGridView, columnIndex, rowIndex, dataGridViewComboBoxCellCameraOwners);
               
                if (!string.IsNullOrWhiteSpace(cameraOwner) && dataGridViewComboBoxCellCameraOwners.Items.Contains(cameraOwner))
                    DataGridViewHandler.SetCellValue(dataGridView, columnIndex, rowIndex, cameraOwner, false);
                else
                    DataGridViewHandler.SetCellValue(dataGridView, columnIndex, rowIndex, null, false);
            }
            else
                DataGridViewHandler.SetCellValue(dataGridView, columnIndex, rowIndex, cameraOwner, false);




        }
        #endregion

        #region PopulateNearbyCoordinate
        public static void PopulateNearbyCoordinate(DataGridView dataGridView, int columnIndex,
            int timeZoneShift, int accepedIntervalSecound, DateTime date, DateTime? dateTaken, DateTime? locationDate)
        {
            List<Metadata> metadatasLocationBasedOnBestGuess = DatabaseGoogleLocationHistory.FindLocationBasedOtherMediaFiles
                (locationDate, dateTaken, date, //UseSmartDate ? metadata?.FileSmartDate(allowedDateFormats) : metadata?.FileDate,
                    AccepedIntervalSecound);

            int count = 0;
            foreach (Metadata metadataLocationBasedOnBestGuess in metadatasLocationBasedOnBestGuess)
            {
                string tag = tagCoordinatesNearByPhotos + (count == 0 ? "" : " " + count.ToString());
                if (metadataLocationBasedOnBestGuess != null && metadataLocationBasedOnBestGuess.LocationLatitude != null && metadataLocationBasedOnBestGuess.LocationLongitude != null)
                {
                    int rowIndex = AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerNearByLocations, tag), metadataLocationBasedOnBestGuess.LocationCoordinate, true);
                    DataGridViewHandler.SetCellToolTipText(dataGridView, columnIndex, rowIndex,
                        "Date: " + (date == null ? "(Empty)" : date.ToString()) + "\r\n" +
                        (metadataLocationBasedOnBestGuess.FileDateCreated == null ? "" : "Date: " + metadataLocationBasedOnBestGuess.FileDateCreated.ToString() + " on found media\r\n") +

                        "MediaTaken: " + (dateTaken == null ? "(Empty)" : dateTaken.ToString()) + "\r\n" +
                        (metadataLocationBasedOnBestGuess.MediaDateTaken == null ? "" : "MediaTaken: " + metadataLocationBasedOnBestGuess.MediaDateTaken.ToString() + " on found media\r\n") +

                        "LocationDate: " + (locationDate == null ? "(Empty)" : locationDate.ToString()) + "\r\n" +
                        (metadataLocationBasedOnBestGuess.LocationDateTime == null ? "" : "LocationDate: " + metadataLocationBasedOnBestGuess.LocationDateTime.ToString() + " on found media\r\n")
                        );
                    dataGridView.ShowCellToolTips = true;
                }
                else
                {
                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerNearByLocations, tag), "Not found", true);
                }
                count++;
            }
        }
        #endregion

        #region TagGoogleCoordinateDateTaken
        private static string TagGoogleCoordinateDateTaken(int timeZoneShift)
        {
            return tagGoogleCoordinateDateTaken + " " + (timeZoneShift < 0 ? timeZoneShift.ToString() : "+" + timeZoneShift.ToString());

        }
        #endregion

        #region PopulateGrivViewMapGoogle
        public static void PopulateGoogleHistoryCoordinate(DataGridView dataGridViewMap, int columnIndexMap, 
            int timeZoneShift, int accepedIntervalSecound, DateTime? dateTaken, DateTime? locationDate, Metadata metadata)
        {
            string cameraOwner = GetUserInputCameraOwner(dataGridViewMap, columnIndexMap);
            if (string.IsNullOrWhiteSpace(cameraOwner))
            {
                DataGridViewHandler.SetCellValue(dataGridViewMap, columnIndexMap, headerGoogleLocations, tagGoogleCoordinateUTC, "Need select camera owner");
                return;
            }

            Metadata metadataLocation;

            if (locationDate != null)
            {
                metadataLocation = DatabaseGoogleLocationHistory.FindLocationBasedOnTime(cameraOwner, (DateTime)locationDate, accepedIntervalSecound);
                if (metadataLocation != null)
                    AddRow(dataGridViewMap, columnIndexMap, new DataGridViewGenericRow(headerGoogleLocations, tagGoogleCoordinateUTC), metadataLocation.LocationCoordinate, true);
                else
                    AddRow(dataGridViewMap, columnIndexMap, new DataGridViewGenericRow(headerGoogleLocations, tagGoogleCoordinateUTC),
                        "Not found: Coordinates timestamp " + ((DateTime)locationDate).ToShortDateString() + " " + ((DateTime)locationDate).ToShortTimeString()
                        + " +/- " + accepedIntervalSecound + " secounds not found", true);
            } else DataGridViewHandler.SetCellValue(dataGridViewMap, columnIndexMap, headerGoogleLocations, tagGoogleCoordinateUTC, "Missing UTC date");

            if (dateTaken != null)
            {
                DateTime mediaCreateUTC = new DateTime(((DateTime)dateTaken).Ticks, DateTimeKind.Utc).AddHours(timeZoneShift);

                metadataLocation = DatabaseGoogleLocationHistory.FindLocationBasedOnTime(cameraOwner, mediaCreateUTC, accepedIntervalSecound);
                if (metadataLocation != null)
                    AddRow(dataGridViewMap, columnIndexMap, new DataGridViewGenericRow(headerGoogleLocations, TagGoogleCoordinateDateTaken(timeZoneShift)), metadataLocation.LocationCoordinate, true);
                else
                    AddRow(dataGridViewMap, columnIndexMap, new DataGridViewGenericRow(headerGoogleLocations, TagGoogleCoordinateDateTaken(timeZoneShift)),
                        "Not found: Coordinates timestamp " + mediaCreateUTC.ToShortDateString() + " " + mediaCreateUTC.ToShortTimeString()
                        + " +/- " + accepedIntervalSecound + " secounds not found", true);
            }
            else DataGridViewHandler.SetCellValue(dataGridViewMap, columnIndexMap, headerGoogleLocations, TagGoogleCoordinateDateTaken(timeZoneShift), "Missing DateTaken");
        }
        #endregion

        #region PopulateGoogleHistoryCoordinateAndNearby
        public static void PopulateGoogleHistoryCoordinateAndNearby(DataGridView dataGridViewMap, DataGridView dataGridViewDate, int columnIndexMap, int timeZoneShift, int accepedIntervalSecound)
        {
            #region Check if Aggegated
            DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridViewMap, columnIndexMap);
            if (dataGridViewGenericColumn == null) return;
            #endregion

            #region Get Metadata
            Metadata metadata = dataGridViewGenericColumn.Metadata;
            if (metadata == null)
            {
                DataGridViewHandler.SetCellValue(dataGridViewMap, columnIndexMap, headerGoogleLocations, tagGoogleCoordinateUTC, "No metadata loaded");
                return;
            }
            #endregion

            DateTime? dateTaken = DataGridViewHandlerDate.GetUserInputDateTaken(dataGridViewDate, null, dataGridViewGenericColumn.FileEntryAttribute);
            DateTime? locationDate = DataGridViewHandlerDate.GetUserInputLocationDate(dataGridViewDate, null, dataGridViewGenericColumn.FileEntryAttribute);
            if (dateTaken == null) dateTaken = metadata.MediaDateTaken;
            if (locationDate == null) locationDate = metadata.LocationDateTime;

            PopulateGoogleHistoryCoordinate(
                dataGridViewMap, columnIndexMap, timeZoneShift, accepedIntervalSecound, dateTaken, locationDate, metadata);

            PopulateNearbyCoordinate(
                dataGridViewMap, columnIndexMap, timeZoneShift, accepedIntervalSecound, (DateTime)metadata.FileDate, dateTaken, locationDate);
        }
        #endregion

        #region PopulateGrivViewMapNomnatatim
        public static void PopulateGrivViewMapNomnatatim(DataGridView dataGridView, int columnIndex, LocationCoordinate locationCoordinateSearch, 
            bool onlyFromCache, bool canReverseGeocoder, bool forceReloadUsingReverseGeocoder, bool createNewAccurateLocationUsingSearchLocation)
        {
            GlobalData.IsPopulatingMapLocation = true;
            try
            {
                LocationCoordinateAndDescription locationCoordinateAndDescriptionInDatabase = null;

                float locationAccuracyLatitude = Properties.Settings.Default.LocationAccuracyLatitude;
                float locationAccuracyLongitude = Properties.Settings.Default.LocationAccuracyLongitude;

                //LocationDescription locationDescription = null;
                LocationDescription locationDescription = null;

                #region Get Location Info from User when allowed
                DataGridViewGenericColumn dataGridViewGenericColumn = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, columnIndex);
                if (!forceReloadUsingReverseGeocoder && dataGridViewGenericColumn?.Metadata != null)
                {
                    #region Get UserInput Location data
                    Metadata metadataUser = new Metadata(MetadataBrokerType.Empty);
                    GetUserInputChanges(dataGridView, ref metadataUser, null, columnIndex);
                    #endregion

                    if (!string.IsNullOrEmpty(metadataUser.LocationName) || !string.IsNullOrEmpty(metadataUser.LocationCity) ||
                        !string.IsNullOrEmpty(metadataUser.LocationState) || !string.IsNullOrEmpty(metadataUser.LocationCountry))
                    {
                        locationDescription = new LocationDescription(metadataUser.LocationName, metadataUser.LocationCity, metadataUser.LocationState, metadataUser.LocationCountry);

                        #region createNewAccurateLocationUsingSearchLocation
                        if (createNewAccurateLocationUsingSearchLocation)
                        {
                            try
                            {
                                LocationCoordinateAndDescription locationCoordinateAndDescriptionFromUserInput = new LocationCoordinateAndDescription(
                                    locationCoordinateSearch, locationDescription);
                                DatabaseAndCacheLocationAddress.WriteLocationName(locationCoordinateSearch, locationCoordinateAndDescriptionFromUserInput);

                                dataGridView.EndEdit();
                                //Remove + sign
                                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMedia, tagMediaCoordinates,
                                    ReadWriteAccess.AllowCellReadAndWrite), locationCoordinateSearch.ToString(), false);
                            }
                            catch
                            {
                                //DEBUG
                            }

                        }
                        #endregion
                    }
                }
                #endregion

                if (locationCoordinateSearch != null)
                {
                    #region Get Nearby Location Coordinate and Info in Database
                    locationCoordinateAndDescriptionInDatabase = DatabaseAndCacheLocationAddress.AddressLookupAndReverseGeocoder(
                    locationCoordinateSearch, locationAccuracyLatitude, locationAccuracyLongitude, onlyFromCache: onlyFromCache,
                    canReverseGeocoder: canReverseGeocoder, metadataLocationDescription: locationDescription, forceReloadUsingReverseGeocoder: false);
                    #endregion

                    

                    #region If Asked to Reload, reload from UsingReverseGeocoder
                    if (forceReloadUsingReverseGeocoder && locationCoordinateAndDescriptionInDatabase != null)
                    {
                        locationCoordinateAndDescriptionInDatabase = DatabaseAndCacheLocationAddress.AddressLookupAndReverseGeocoder(
                            locationCoordinateSearch, locationAccuracyLatitude, locationAccuracyLongitude, onlyFromCache: false,
                            canReverseGeocoder: true, metadataLocationDescription: null, forceReloadUsingReverseGeocoder: true);
                    }
                    #endregion
                }
                else
                {
                    #region No coordinates found
                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMedia, tagMediaCoordinates,
                            ReadWriteAccess.AllowCellReadAndWrite), null, false);
                    #endregion
                }
                #region Show Tooltip when Use need Nearby coordinate
                int rowIndex = DataGridViewHandler.GetRowIndex(dataGridView, headerMedia, tagMediaCoordinates);
                if (locationCoordinateAndDescriptionInDatabase != null && locationCoordinateSearch != locationCoordinateAndDescriptionInDatabase.Coordinate)
                {                    
                    DataGridViewHandler.SetCellToolTipText(dataGridView, columnIndex, rowIndex, "Near by location used: " + locationCoordinateAndDescriptionInDatabase.Coordinate.ToString());
                } else
                {
                    DataGridViewHandler.SetCellToolTipText(dataGridView, columnIndex, rowIndex, "");
                }
                #endregion

                #region No data location data loaded, set as readonly
                bool isReadOnly = (locationCoordinateAndDescriptionInDatabase == null);
                #endregion

                #region Updated DataGridView with new data
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerNominatim, tagLocationName, ReadWriteAccess.AllowCellReadAndWrite),
                    locationCoordinateAndDescriptionInDatabase?.Description.Name, isReadOnly);
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerNominatim, tagCity, ReadWriteAccess.AllowCellReadAndWrite),
                    locationCoordinateAndDescriptionInDatabase?.Description.City, isReadOnly);
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerNominatim, tagProvince, ReadWriteAccess.AllowCellReadAndWrite),
                    locationCoordinateAndDescriptionInDatabase?.Description.Region, isReadOnly);
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerNominatim, tagCountry, ReadWriteAccess.AllowCellReadAndWrite),
                    locationCoordinateAndDescriptionInDatabase?.Description.Country, isReadOnly);
                #endregion
            }
            catch (Exception ex)
            {
                KryptonMessageBox.Show("Unexpected error occur.\r\nException message:" + ex.Message + "\r\n",
                    "Unexpected error occur", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Error, showCtrlCopy: true);
            }
            finally
            {
                GlobalData.IsPopulatingMapLocation = false;
            }
        }
        #endregion

        #region PopulateFile
        public static int PopulateFile(DataGridView dataGridView, DataGridView dataGridViewDate, FileEntryAttribute fileEntryAttribute, ShowWhatColumns showWhatColumns, Metadata metadataAutoCorrected, bool onlyRefresh)
        {
            //-----------------------------------------------------------------
            //Chech if need to stop
            if (GlobalData.IsApplicationClosing) return -1;
            if (!DataGridViewHandler.GetIsAgregated(dataGridView)) return -1;      //Not default columns or rows added
            if (DataGridViewHandler.GetIsPopulatingFile(dataGridView)) return -1;  //In progress doing so

            //Check if file is in DataGridView, and needs updated
            if (!DataGridViewHandler.DoesColumnFilenameExist(dataGridView, fileEntryAttribute.FileFullPath)) return -1;

            //When file found, Tell it's populating file, avoid two process updates
            DataGridViewHandler.SetIsPopulatingFile(dataGridView, true);

            //-----------------------------------------------------------------
            FileEntryBroker fileEntryBrokerReadVersion = fileEntryAttribute.GetFileEntryBroker(MetadataBrokerType.ExifTool);
            Image thumbnail = DatabaseAndCacheThumbnail.ReadThumbnailFromCacheOnly(fileEntryBrokerReadVersion);
            if (thumbnail == null && metadataAutoCorrected != null) thumbnail = DatabaseAndCacheThumbnail.ReadThumbnailFromCacheOnly(metadataAutoCorrected.FileEntry);

            Metadata metadataExiftool = DatabaseAndCacheMetadataExiftool.ReadMetadataFromCacheOnly(fileEntryBrokerReadVersion);
            if (metadataExiftool != null) metadataExiftool = new Metadata(metadataExiftool);
            if (metadataAutoCorrected != null) metadataExiftool = metadataAutoCorrected; //If AutoCorrect is run, use AutoCorrect values. Needs to be after DataGridViewHandler.AddColumnOrUpdateNew, so orignal metadata stored will not be overwritten

            ReadWriteAccess readWriteAccessColumn =
                (FileEntryVersionHandler.IsReadOnlyColumnType(fileEntryAttribute.FileEntryVersion) ||
                metadataExiftool == null) ? ReadWriteAccess.ForceCellToReadOnly : ReadWriteAccess.AllowCellReadAndWrite;

            int columnIndex = DataGridViewHandler.AddColumnOrUpdateNew(
                dataGridView, fileEntryAttribute, thumbnail, metadataExiftool, readWriteAccessColumn, showWhatColumns,
                DataGridViewGenericCellStatus.DefaultEmpty(), out FileEntryVersionCompare fileEntryVersionCompareReason);

            //Chech if populated and new refresh data
            if (onlyRefresh && FileEntryVersionHandler.DoesCellsNeedUpdate(fileEntryVersionCompareReason) && !DataGridViewHandler.IsColumnPopulated(dataGridView, columnIndex))
                fileEntryVersionCompareReason = FileEntryVersionCompare.LostNoneEqualFound_ContinueSearch_Update_Nothing; //No need to populate
            //-----------------------------------------------------------------

            if (FileEntryVersionHandler.DoesCellsNeedUpdate(fileEntryVersionCompareReason))
            {
                //Media
                int rowIndex;
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMedia));
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMedia, tagMediaCoordinates), metadataExiftool?.LocationCoordinate, false);
                rowIndex = AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMedia, tagLocationName), metadataExiftool?.LocationName, false);
                List<string> newKeywords = AutoKeywordHandler.NewKeywords(AutoKeywordConvertions, metadataExiftool?.LocationName, null, null, null, null, null);
                DataGridViewHandler.SetCellToolTipText(dataGridView, columnIndex, rowIndex, "Running AutoCorrect will add these keywords", newKeywords);
                
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMedia, tagCity), metadataExiftool?.LocationCity, false);                
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMedia, tagProvince), metadataExiftool?.LocationState, false);
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMedia, tagCountry), metadataExiftool?.LocationCountry, false);

                //List<string> newKeywords = AutoKeywordHandler.NewKeywords(autoKeywordConvertions, metadataCopy.LocationName, metadataCopy.PersonalTitle,
                //  metadataCopy.PersonalAlbum, metadataCopy.PersonalDescription, metadataCopy.PersonalComments, metadataCopy.PersonalKeywordTags);
                
                //Google location history
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerGoogleLocations));

                if (metadataExiftool != null)
                {
                    CameraOwner cameraOwnerPrint = new CameraOwner(metadataExiftool.CameraMake, metadataExiftool.CameraModel, "");
                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerGoogleLocations, tagCameraMakeModel), cameraOwnerPrint, true);
                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerGoogleLocations, tagCameraOwner), "Owner???", false);

                    DataGridViewGenericColumn gridViewGenericColumnCheck = DataGridViewHandler.GetColumnDataGridViewGenericColumn(dataGridView, columnIndex);
                    PopulateCameraOwner(dataGridView, columnIndex, readWriteAccessColumn, metadataExiftool.CameraMake, metadataExiftool.CameraModel);
                }
                else
                {
                    if (!DataGridViewHandler.IsColumnPopulated(dataGridView, columnIndex))
                    {
                        AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMedia, tagCameraMakeModel), "", false);
                        AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerGoogleLocations, tagCameraOwner), "Select Camera owner/locations", true);
                    }
                }

                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerGoogleLocations, tagGoogleCoordinateUTC), metadataExiftool?.LocationCoordinate, true);
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerNearByLocations));

                PopulateGoogleHistoryCoordinateAndNearby(dataGridView, dataGridViewDate, columnIndex, TimeZoneShift, AccepedIntervalSecound);

                //Nominatim.API
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerNominatim));
                PopulateGrivViewMapNomnatatim(dataGridView, columnIndex, metadataExiftool?.LocationCoordinate, 
                    onlyFromCache: true, canReverseGeocoder: false, forceReloadUsingReverseGeocoder: false, createNewAccurateLocationUsingSearchLocation:false);

                //WebScraper
                //headerWebScraping = "WebScraper";
                // WebScarping
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerWebScraping));
                Metadata metadataWebScraping = null;
                if (metadataExiftool != null) metadataWebScraping = DatabaseAndCacheMetadataExiftool.ReadWebScraperMetadataFromCacheOrDatabase(new FileEntryBroker(fileEntryBrokerReadVersion, MetadataBrokerType.WebScraping));
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerWebScraping, tagLocationName), metadataWebScraping?.LocationName, true);
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerWebScraping, tagCountry), metadataWebScraping?.LocationCountry, true);

                //Microsoft Photos Locations
                Metadata metadataMicrosoftPhotos = null;
                if (GlobalData.doesMircosoftPhotosExists)
                {
                    if (metadataExiftool != null) metadataMicrosoftPhotos = DatabaseAndCacheMetadataMicrosoftPhotos.ReadMetadataFromCacheOrDatabase(
                        new FileEntryBroker(fileEntryBrokerReadVersion, MetadataBrokerType.MicrosoftPhotos));

                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMicrosoftPhotos));
                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMicrosoftPhotos, tagExternalCoordinates), metadataMicrosoftPhotos?.LocationCoordinate, true);
                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMicrosoftPhotos, tagLocationName), metadataMicrosoftPhotos?.LocationName, true);
                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMicrosoftPhotos, tagCity), metadataMicrosoftPhotos?.LocationCity, true);
                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMicrosoftPhotos, tagProvince), metadataMicrosoftPhotos?.LocationState, true);
                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerMicrosoftPhotos, tagCountry), metadataMicrosoftPhotos?.LocationCountry, true);
                }

                //Windows Live Photo Gallary Locations
                Metadata metadataWindowsLivePhotoGallery = null;
                if (GlobalData.doesWindowsLivePhotoGalleryExists)
                {
                    if (metadataExiftool != null) metadataWindowsLivePhotoGallery = DatabaseAndCacheMetadataWindowsLivePhotoGallery.ReadMetadataFromCacheOrDatabase(
                        new FileEntryBroker(fileEntryBrokerReadVersion, MetadataBrokerType.WindowsLivePhotoGallery));

                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerWindowsLivePhotoGallery));
                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerWindowsLivePhotoGallery, tagExternalCoordinates), metadataWindowsLivePhotoGallery?.LocationCoordinate, true);
                    AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerWindowsLivePhotoGallery, tagLocationName), metadataWindowsLivePhotoGallery?.LocationName, true);
                }

                //Browser
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerBrowser));
                AddRow(dataGridView, columnIndex, new DataGridViewGenericRow(headerBrowser, tagExternalCoordinates), "", true);

                DataGridViewHandler.SetColumnPopulatedFlag(dataGridView, columnIndex, true);
            }

            //-----------------------------------------------------------------
            DataGridViewHandler.SetIsPopulatingFile(dataGridView, false);
            //-----------------------------------------------------------------
            return columnIndex;
        }
        #endregion

        #region PopulateSelectedFiles
        public static void PopulateSelectedFiles(DataGridView dataGridView, HashSet<FileEntry> imageListViewSelectItems, DataGridViewSize dataGridViewSize, ShowWhatColumns showWhatColumns)
        {
            //-----------------------------------------------------------------
            //Chech if need to stop
            if (GlobalData.IsApplicationClosing) return;
            if (DataGridViewHandler.GetIsAgregated(dataGridView)) return;
            if (DataGridViewHandler.GetIsPopulating(dataGridView)) return;
            //Tell that work in progress, can start a new before done.
            DataGridViewHandler.SetIsPopulating(dataGridView, true);
            //Clear current DataGridView
            DataGridViewHandler.Clear(dataGridView, dataGridViewSize);
            DataGridViewHandler.SetDataGridViewAllowUserToAddRows(dataGridView, false);
            //Add Columns for all selected files, one column per select file
            DataGridViewHandlerCommon.AddColumnSelectedFiles(dataGridView, DatabaseAndCacheThumbnail, imageListViewSelectItems, ReadWriteAccess.ForceCellToReadOnly, showWhatColumns,
                new DataGridViewGenericCellStatus(MetadataBrokerType.Empty, SwitchStates.Off, true)); //ReadOnly until data is read //Add all default rows
            //AddRowsDefault(dataGridView);
            //Tell data default columns and rows are agregated
            DataGridViewHandler.SetIsAgregated(dataGridView, true);
            //-----------------------------------------------------------------

            //Tell that work is done
            DataGridViewHandler.SetIsPopulating(dataGridView, false);
            //-----------------------------------------------------------------
        }
        #endregion 

    }
}