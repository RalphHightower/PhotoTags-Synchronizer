﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using ApplicationAssociations;
using MetadataLibrary;
using MetadataPriorityLibrary;
using TimeZone;
using FileHandeling;
using System.Threading.Tasks;
using System.Threading;

namespace Exiftool
{
    public class ExiftoolReader : ImetadataReader
    {
        private enum LocationTristate
        {
            NotSet,
            Negative,
            Posetiv
        }
        private static readonly NLog.Logger Logger = NLog.LogManager.GetCurrentClassLogger();

        private MetadataDatabaseCache metadataDatabaseCache;
        private ExiftoolDataDatabase metadataExiftoolDatabase;
        private ExiftoolWarningDatabase metadataExiftoolWarningDatabase;

        public ExiftoolReader(
            MetadataDatabaseCache metadataDatabaseCache,
            ExiftoolDataDatabase metadataExiftoolDatabase,
            ExiftoolWarningDatabase metadataExiftoolWarningDatabase)
        {
            this.metadataDatabaseCache = metadataDatabaseCache;
            this.metadataExiftoolDatabase = metadataExiftoolDatabase;
            this.metadataExiftoolWarningDatabase = metadataExiftoolWarningDatabase;
        }

        public delegate void AfterNewMediaFound(FileEntry fileEntry);
        public event AfterNewMediaFound afterNewMediaFoundEvent;
        public MetadataReadPrioity MetadataReadPrioity { get; set; } = new MetadataReadPrioity();

        #region ExiftoolData old - Used to check new data equal with old data that was read

        private List<string> oldKeywordList = null;
        private ExiftoolData oldExifToolKeywords = new ExiftoolData();

        private List<RegionStructure> oldRegionList = null;
        private ExiftoolData oldExifToolRegion = new ExiftoolData();

        private ExiftoolData oldExifToolFileName = new ExiftoolData();
        private ExiftoolData oldExifToolFilePath = new ExiftoolData();
        private ExiftoolData oldExifToolFileModifyDate = new ExiftoolData();
        private ExiftoolData oldExifToolFileAccessDate = new ExiftoolData();
        private ExiftoolData oldExifToolFileCreateDate = new ExiftoolData();
        private ExiftoolData oldExifToolFileSize = new ExiftoolData();
        private ExiftoolData oldExifToolMediaWidth = new ExiftoolData();
        private ExiftoolData oldExifToolMediaHeight = new ExiftoolData();
        private ExiftoolData oldExifToolMIMEType = new ExiftoolData();
        private ExiftoolData oldExifToolMake = new ExiftoolData();
        private ExiftoolData oldExifToolModel = new ExiftoolData();
        private ExiftoolData oldExifToolCreateDate = new ExiftoolData();
        private ExiftoolData oldExifToolAuthor = new ExiftoolData();
        private ExiftoolData oldExifToolAlbum = new ExiftoolData();
        private ExiftoolData oldExifToolDescription = new ExiftoolData();
        private ExiftoolData oldExifToolTitle = new ExiftoolData();
        private ExiftoolData oldExifToolComment = new ExiftoolData();
        private ExiftoolData oldExifToolRating = new ExiftoolData();
        private ExiftoolData oldExifToolRatingPercent = new ExiftoolData();
        private ExiftoolData oldExifToolLocationName = new ExiftoolData();
        private ExiftoolData oldExifToolLocationCity = new ExiftoolData();
        private ExiftoolData oldExifToolLocationState = new ExiftoolData();
        private ExiftoolData oldExifToolLocationCountry = new ExiftoolData();
        private ExiftoolData oldExifToolGPSAltitude = new ExiftoolData();
        private ExiftoolData oldExifToolGPSLatitude = new ExiftoolData();
        private LocationTristate isGPSLatitudeRef = LocationTristate.NotSet;
        private string locationEXIFGPSLatitudeParameterWithoutPrefix = null;
        private ExiftoolData oldExifToolGPSLongitude = new ExiftoolData();
        private LocationTristate isGPSLongitudeRef = LocationTristate.NotSet;
        private string locationEXIFGPSLongitudeParameterWithoutPrefix = null;
        private ExiftoolData oldExifToolGPSDateTime = new ExiftoolData();

        private void CleanAllOldExiftoolDataForReadNewFile()
        {
            MetadataReadPrioity.CleanHighestPriority();

            isGPSLatitudeRef = LocationTristate.NotSet;
            isGPSLongitudeRef = LocationTristate.NotSet;
            locationEXIFGPSLatitudeParameterWithoutPrefix = null;
            locationEXIFGPSLongitudeParameterWithoutPrefix = null;

            oldKeywordList = null;
            oldExifToolKeywords = new ExiftoolData();

            oldRegionList = null;
            oldExifToolRegion = new ExiftoolData();

            oldExifToolFileName = new ExiftoolData();
            oldExifToolFilePath = new ExiftoolData();
            oldExifToolFileModifyDate = new ExiftoolData();
            oldExifToolFileAccessDate = new ExiftoolData();
            oldExifToolFileCreateDate = new ExiftoolData();
            oldExifToolFileSize = new ExiftoolData();
            oldExifToolMediaWidth = new ExiftoolData();
            oldExifToolMediaHeight = new ExiftoolData();
            oldExifToolMIMEType = new ExiftoolData();
            oldExifToolMake = new ExiftoolData();
            oldExifToolModel = new ExiftoolData();
            oldExifToolCreateDate = new ExiftoolData();
            oldExifToolAuthor = new ExiftoolData();
            oldExifToolAlbum = new ExiftoolData();
            oldExifToolDescription = new ExiftoolData();
            oldExifToolTitle = new ExiftoolData();
            oldExifToolComment = new ExiftoolData();
            oldExifToolRating = new ExiftoolData();
            oldExifToolRatingPercent = new ExiftoolData();
            oldExifToolLocationName = new ExiftoolData();
            oldExifToolLocationCity = new ExiftoolData();
            oldExifToolLocationState = new ExiftoolData();
            oldExifToolLocationCountry = new ExiftoolData();
            oldExifToolGPSAltitude = new ExiftoolData();
            oldExifToolGPSLatitude = new ExiftoolData();
            oldExifToolGPSLongitude = new ExiftoolData();
            oldExifToolGPSDateTime = new ExiftoolData();
        }
        #endregion

        #region Is Application Closing
        private bool isClosing = false;
        public void Close()
        {
            isClosing = true;
        }
        #endregion 

        #region MetadataGroupPrioity
        public void MetadataGroupPrioritiesWrite()
        {
            MetadataReadPrioity.Write();
        }
        public void MetadataGroupPrioityRead()
        {
            MetadataReadPrioity.ReadOnlyOnce();
        }
        #endregion ExiftoolTagsWarning_Write

        #region ExiftoolTagsWarning_Write
        private void ExiftoolTagsWarning_Write(ExiftoolData exifToolDataPrevious, ExiftoolData exifToolDataConvertThis, string warning)
        {
            metadataExiftoolWarningDatabase.Write(exifToolDataPrevious, exifToolDataConvertThis, warning);
        }
        #endregion

        #region Check Time Zone
        private void CheckTimeZone(Metadata metadata, DateTime fileDateModified, ref String error)
        {
            string verificationRegion = "Verification";
            string verificationMediaTaken = "MediaTaken";
            string verificationTimeZone = "TimeZone";
            string verificationLocationDateTime = "GPSDateTime";
            string verificationLocationCoordinates = "GPSCoordinates";


            if (metadata.MediaDateTaken == null)
            {
                ExiftoolData exifToolData = new ExiftoolData(metadata.FileName, metadata.FileDirectory, fileDateModified,
                verificationRegion, verificationMediaTaken, "Check Date and Time has correct time", metadata.MediaDateTaken);

                string warning = "Warning! Missing metadata tag " + CompositeTags.DateTimeDigitized + "\r\n";
                error += warning;
                ExiftoolTagsWarning_Write(exifToolData, exifToolData, warning);
            }
            else if (metadata.LocationLatitude == null || metadata.LocationLongitude == null)
            {
                //Missing GPS ccorinate
                ExiftoolData exifToolData = new ExiftoolData(metadata.FileName, metadata.FileDirectory, fileDateModified,
                    verificationRegion, verificationLocationCoordinates, "Check Date and Time has correct time", null);

                string warning = "Warning! Missing metadata tags " + CompositeTags.GPSCoordinatesLatitude + " and " + CompositeTags.GPSCoordinatesLongitude + "\r\n";
                error += warning;
                ExiftoolTagsWarning_Write(exifToolData, exifToolData, warning);
            }
            else if (metadata.LocationDateTime == null)
            {
                ExiftoolData exifToolData = new ExiftoolData(metadata.FileName, metadata.FileDirectory, fileDateModified,
                        verificationRegion, verificationLocationDateTime, "Check Date and Time has correct time", metadata.LocationDateTime);

                string warning = "Warning! Missing metadata tag " + CompositeTags.GPSDateTime + "\r\n";
                error += warning;
                ExiftoolTagsWarning_Write(exifToolData, exifToolData, warning);
            }
            else
            {
                if (!TimeZoneLibrary.IsTimeZoneEqual(
                    (double)metadata.LocationLatitude, (double)metadata.LocationLongitude,
                    (DateTime)metadata.LocationDateTime, fileDateModified, out string TimeZoneVerfification))
                {
                    ExiftoolData exifToolData = new ExiftoolData(metadata.FileName, metadata.FileDirectory, fileDateModified,
                        verificationRegion, verificationTimeZone, "Check Date and Time has correct time", metadata.MediaDateTaken);

                    string warning = "Warning! Metadata has mismatch between " + CompositeTags.DateTimeDigitized + " and " + CompositeTags.GPSDateTime + "\r\n" +
                        TimeZoneVerfification;
                    error += warning;
                    ExiftoolTagsWarning_Write(exifToolData, exifToolData, warning);

                }
            }
        }
        #endregion

        #region Check Keyword List
        private void CheckKeywordList(List<string> oldKeywordList, List<string> newKeywordList, ExiftoolData exifToolDataConvertThis, ExiftoolData exifToolDataPrevious, String compositeTag, ref String error)
        {
            if (oldKeywordList == null) return;
            if (newKeywordList == null) return;
            bool isListEqual = true;

            foreach (string keyword in newKeywordList)
            {
                if (!oldKeywordList.Contains(keyword))
                {
                    isListEqual = false;
                    break;
                }
            }
            if (isListEqual)
            {
                foreach (string keyword in oldKeywordList)
                {
                    if (!newKeywordList.Contains(keyword))
                    {
                        isListEqual = false;
                        break;
                    }
                }
            }

            if (!isListEqual)
            {
                string warning = "Warning! Metadata missmatching between two metadata values.\r\nComposite tag:" + compositeTag + "\r\n";

                warning += string.Format(CultureInfo.InvariantCulture,
                        "\r\nRegion: {0} Command: {1}\r\nValues:'\r\n", exifToolDataConvertThis.Region, exifToolDataConvertThis.Command);
                foreach (string keyword in newKeywordList) warning += keyword + "\r\n";

                warning += string.Format(CultureInfo.InvariantCulture,
                        "\r\nRegion: {0} Command: {1}\r\nValue\r\n", exifToolDataPrevious.Region, exifToolDataPrevious.Command);
                foreach (string keyword in oldKeywordList) warning += keyword + "\r\n";

                error += warning;
                ExiftoolTagsWarning_Write(exifToolDataPrevious, exifToolDataConvertThis, warning);

                //Select the list that has highst priority
                //if (MetadataReadPrioity.Get(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag) > MetadataReadPrioity.GetCompositeTagsHighestPrioity(compositeTag))
            }
        }
        #endregion

        #region Check Face Region List
        private void CheckRegionList(List<RegionStructure> oldRegionList, List<RegionStructure> newRegionList, Size? mediaSize, ExiftoolData exifToolDataConvertThis, ExiftoolData exifToolDataPrevious, String compositeTag, ref String error)
        {
            if (oldRegionList == null) return;
            if (newRegionList == null) return;
            bool isListEqual = true;

            foreach (RegionStructure regionStructure in newRegionList)
            {                
                if (!regionStructure.DoesThisRectangleAndNameExistInList(oldRegionList))
                {
                    isListEqual = false;
                    break;
                }
            }
            if (isListEqual)
            {
                foreach (RegionStructure regionStructure in oldRegionList)
                {
                    if (!regionStructure.DoesThisRectangleAndNameExistInList(newRegionList))
                    {
                        isListEqual = false;
                        break;
                    }
                }
            }

            if (!isListEqual)
            {
                string warning = "Warning! Metadata missmatching between two metadata values.\r\nComposite tag:" + compositeTag + "\r\n";

                if (mediaSize == null) mediaSize = new Size(1000, 1000); 

                warning += string.Format(CultureInfo.InvariantCulture,
                        "\r\nRegion: {0} Command: {1}\r\nValues:'\r\n", exifToolDataConvertThis.Region, exifToolDataConvertThis.Command);
                foreach (RegionStructure regionStructure in newRegionList) warning += regionStructure.RegionErrorText((Size)mediaSize) + "\r\n";

                warning += string.Format(CultureInfo.InvariantCulture,
                        "\r\nRegion: {0} Command: {1}\r\nValue\r\n", exifToolDataPrevious.Region, exifToolDataPrevious.Command);
                foreach (RegionStructure regionStructure in oldRegionList) warning += regionStructure.RegionErrorText((Size)mediaSize) + "\r\n";

                error += warning;
                ExiftoolTagsWarning_Write(exifToolDataPrevious, exifToolDataConvertThis, warning);

            }
        }
        #endregion 

        #region ConvertDateTimeLocalFromString
        public DateTime? ConvertDateTimeLocalFromString(String dateTimeToConvert)
        {
            DateTimeOffset pictureTime;
            DateTimeOffset localTimeZone;
            String[] dateFormats = { "yyyy:MM:dd HH:mm:ssZ", "yyyy:MM:dd HH:mm", "yyyy:MM:dd HH:mm:ss", "yyyy:MM:dd HH:mm:sszzz", "yyyy:MM:dd HH:mm:ss.fff", "yyyy:MM:dd HH:mm:ss.ff", "yyyy:MM:dd HH:mm:ss.ffff", "yyyy:MM:dd HH:mm:ss.fffff", "yyyy:MM:dd HH:mm:ss.ffffff" };
            String[] convertThisDateZone = dateTimeToConvert.Split('+');
            
            try
            {
                if (convertThisDateZone.Length > 1)
                {
                    if (DateTimeOffset.TryParseExact(dateTimeToConvert, dateFormats, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal, out pictureTime))
                    {                        
                        localTimeZone = TimeZoneInfo.ConvertTime(pictureTime, TimeZoneInfo.Local);
                        return new DateTime(localTimeZone.Ticks, DateTimeKind.Local);
                    }
                    else return null;
                }
                else if (convertThisDateZone[0][convertThisDateZone[0].Length - 1] == 'Z')
                {
                    if (DateTimeOffset.TryParseExact(dateTimeToConvert, "yyyy:MM:dd HH:mm:ssZ", CultureInfo.CurrentCulture, DateTimeStyles.AssumeUniversal, out pictureTime))
                    {                        
                        localTimeZone = TimeZoneInfo.ConvertTime(pictureTime, TimeZoneInfo.Utc);
                        return new DateTime(localTimeZone.Ticks, DateTimeKind.Utc);
                    }
                    else return null;
                }
                else
                {
                    if (DateTimeOffset.TryParseExact(dateTimeToConvert, dateFormats, CultureInfo.CurrentCulture, DateTimeStyles.AssumeLocal, out pictureTime))
                    {
                        localTimeZone = TimeZoneInfo.ConvertTime(pictureTime, TimeZoneInfo.Local);
                        return new DateTime(localTimeZone.Ticks, DateTimeKind.Local);
                    }
                    else return null;
                }
            } catch (Exception ex)
            {
                Logger.Error(ex, "Failed convert date: " + dateTimeToConvert); //TODO: Need to fix problems with date formats
                return null; 
            }
        }
        #endregion

        #region ConvertAndCheckDateFromString
        private DateTime? ConvertAndCheckDateFromString(ExiftoolData exifToolDataConvertThis, ExiftoolData exifToolDataPrevious, String compositeTag, ref String error)
        {
            DateTime? oldValue = (DateTime?)exifToolDataPrevious.TristateValue;
            MetadataReadPrioity.Add(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            DateTime? newValue = ConvertDateTimeLocalFromString(exifToolDataConvertThis.Parameter);

            if (!newValue.HasValue)
            {
                string warning = string.Format(CultureInfo.InvariantCulture, "Warning: Error in date and time formating value Region: {0} Command: {1} Value: '{2}': ",
                        exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, exifToolDataConvertThis.Parameter);
                error += warning;
                ExiftoolTagsWarning_Write(exifToolDataPrevious, exifToolDataConvertThis, warning);

                return null;
            }

            if (exifToolDataPrevious.HasValueSet &&
                (
                (newValue == null && oldValue != null) ||
                (newValue != null && oldValue == null) ||
                (newValue != null && oldValue != null && !TimeZoneLibrary.IsDateTimeEqualWithinOneSecond((DateTime)newValue, (DateTime)oldValue)))
                )
            {
                string warning = string.Format(CultureInfo.InvariantCulture, 
                        "Warning! Metadata missmatching between two metadata values.\r\n" +
                        "Region: {0} Command: {1}\r\nValue '{2}'\r\n" +
                        "Region: {3} Command: {4}\r\nValue '{5}'",
                        exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, exifToolDataConvertThis.Parameter,
                        exifToolDataPrevious.Region, exifToolDataPrevious.Command, exifToolDataPrevious.Parameter);                
                error += warning;
                ExiftoolTagsWarning_Write(exifToolDataPrevious, exifToolDataConvertThis, warning);

            }

            int prioirty = MetadataReadPrioity.Get(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            int highestPrioity = MetadataReadPrioity.GetCompositeTagsHighestPrioity(compositeTag);
            MetadataReadPrioity.SetCompositeTagsHighestPrioity(compositeTag, prioirty);

            if (!exifToolDataPrevious.HasValueSet || prioirty > highestPrioity) return newValue;
            else return oldValue;

        }
        #endregion

        #region ConvertAndCheckByteFromString
        private byte? ConvertAndCheckByteFromString(ExiftoolData exifToolDataConvertThis, ExiftoolData exifToolDataPrevious, String compositeTag, ref String error)
        {
            byte? oldValue = (byte?)exifToolDataPrevious.TristateValue;
            MetadataReadPrioity.Add(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            byte? newValue = exifToolDataConvertThis.Parameter == null ? (byte?)null : byte.Parse(exifToolDataConvertThis.Parameter, CultureInfo.InvariantCulture);

            if (exifToolDataPrevious.HasValueSet && oldValue != newValue)
            {
                string warning = string.Format(CultureInfo.InvariantCulture,
                        "Warning! Metadata missmatching between two metadata values.\r\n" +
                        "Region: {0} Command: {1}\r\nValue '{2}'\r\n" +
                        "Region: {3} Command: {4}\r\nValue '{5}'",
                        exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, exifToolDataConvertThis.Parameter,
                        exifToolDataPrevious.Region, exifToolDataPrevious.Command, exifToolDataPrevious.Parameter);
                error += warning;
                ExiftoolTagsWarning_Write(exifToolDataPrevious, exifToolDataConvertThis, warning);

            }

            int prioirty = MetadataReadPrioity.Get(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            int highestPrioity = MetadataReadPrioity.GetCompositeTagsHighestPrioity(compositeTag);
            MetadataReadPrioity.SetCompositeTagsHighestPrioity(compositeTag, prioirty);

            if (!exifToolDataPrevious.HasValueSet || prioirty > highestPrioity) return newValue;
            else return oldValue;
        }
        #endregion

        #region ConvertAndCheckIntFromString
        private int? ConvertAndCheckIntFromString(ExiftoolData exifToolDataConvertThis, ExiftoolData exifToolDataPrevious, String compositeTag, ref String error)
        {
            int? oldValue = (int?)exifToolDataPrevious.TristateValue;
            MetadataReadPrioity.Add(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            int? newValue = exifToolDataConvertThis.Parameter == null ? (int?)null : int.Parse(exifToolDataConvertThis.Parameter, CultureInfo.InvariantCulture);

            if (exifToolDataPrevious.HasValueSet && oldValue != newValue)
            {
                string warning = string.Format(CultureInfo.InvariantCulture,
                        "Warning! Metadata missmatching between two metadata values.\r\n" +
                        "Region: {0} Command: {1}\r\nValue '{2}'\r\n" +
                        "Region: {3} Command: {4}\r\nValue '{5}'",
                        exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, exifToolDataConvertThis.Parameter,
                        exifToolDataPrevious.Region, exifToolDataPrevious.Command, exifToolDataPrevious.Parameter);
                error += warning;
                ExiftoolTagsWarning_Write(exifToolDataPrevious, exifToolDataConvertThis, warning);

            }

            int prioirty = MetadataReadPrioity.Get(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            int highestPrioity = MetadataReadPrioity.GetCompositeTagsHighestPrioity(compositeTag);
            MetadataReadPrioity.SetCompositeTagsHighestPrioity(compositeTag, prioirty);

            if (!exifToolDataPrevious.HasValueSet || prioirty > highestPrioity) return newValue;
            else return oldValue;
        }
        #endregion

        #region ConvertAndCheckLongFromString
        private long? ConvertAndCheckLongFromString(ExiftoolData exifToolDataConvertThis, ExiftoolData exifToolDataPrevious, String compositeTag, ref String error)
        {
            long? oldValue = (long?)exifToolDataPrevious.TristateValue;
            MetadataReadPrioity.Add(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            long? newValue = exifToolDataConvertThis.Parameter == null ? (long?)null : long.Parse(exifToolDataConvertThis.Parameter, CultureInfo.InvariantCulture);

            if (exifToolDataPrevious.HasValueSet && oldValue != newValue)
            {
                string warning = string.Format(CultureInfo.InvariantCulture,
                        "Warning! Metadata missmatching between two metadata values.\r\n" +
                        "Region: {0} Command: {1}\r\nValue '{2}'\r\n" +
                        "Region: {3} Command: {4}\r\nValue '{5}'",
                        exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, exifToolDataConvertThis.Parameter,
                        exifToolDataPrevious.Region, exifToolDataPrevious.Command, exifToolDataPrevious.Parameter);
                error += warning;
                ExiftoolTagsWarning_Write(exifToolDataPrevious, exifToolDataConvertThis, warning);

            }

            int prioirty = MetadataReadPrioity.Get(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            int highestPrioity = MetadataReadPrioity.GetCompositeTagsHighestPrioity(compositeTag);
            MetadataReadPrioity.SetCompositeTagsHighestPrioity(compositeTag, prioirty);

            if (!exifToolDataPrevious.HasValueSet || prioirty > highestPrioity) return newValue;
            else return oldValue;
        }
        #endregion

        #region ConvertAndCheckNumberFromString
        private float? ConvertAndCheckNumberFromString(ExiftoolData exifToolDataConvertThis, ExiftoolData exifToolDataPrevious, String compositeTag, ref String error)
        {
            float? oldValue = (float?)exifToolDataPrevious.TristateValue;
            MetadataReadPrioity.Add(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            float? newValue = exifToolDataConvertThis.Parameter == null ? (float?)null : float.Parse(exifToolDataConvertThis.Parameter, CultureInfo.InvariantCulture);
            if (newValue != null) newValue = (float)Math.Round((float)newValue, SqliteDatabase.SqliteDatabaseUtilities.NumberOfDecimals);

            if (exifToolDataPrevious.HasValueSet &&
                ((oldValue != null && newValue == null) ||
                (oldValue == null && newValue != null) ||
                (oldValue != null && newValue != null && Math.Abs((float)newValue - (float)oldValue) >= 0.000000000001) //Due not not excant numbers in float
                )) 
            {
                string warning = string.Format(CultureInfo.InvariantCulture,
                    "Warning! Metadata missmatching between two metadata values.\r\n" +
                    "Region: {0} Command: {1}\r\nValue '{2}'\r\n" +
                    "Region: {3} Command: {4}\r\nValue '{5}'",
                    exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, exifToolDataConvertThis.Parameter,
                    exifToolDataPrevious.Region, exifToolDataPrevious.Command, exifToolDataPrevious.Parameter);
                error += warning;
                ExiftoolTagsWarning_Write(exifToolDataPrevious, exifToolDataConvertThis, warning);

            }

            int prioirty = MetadataReadPrioity.Get(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            int highestPrioity = MetadataReadPrioity.GetCompositeTagsHighestPrioity(compositeTag);
            MetadataReadPrioity.SetCompositeTagsHighestPrioity(compositeTag, prioirty);

            if (!exifToolDataPrevious.HasValueSet || prioirty > highestPrioity) return newValue;
            else return oldValue;
        }
        #endregion

        #region ConvertAndCheckStringFromString
        private string ConvertAndCheckStringFromString(ExiftoolData exifToolDataConvertThis, ExiftoolData exifToolDataPrevious, String compositeTag, ref String error)
        {
            string oldValue = (string)exifToolDataPrevious.TristateValue;
            MetadataReadPrioity.Add(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            string newValue = exifToolDataConvertThis.Parameter;

            if (exifToolDataPrevious.HasValueSet && oldValue != newValue)
            {
                string warning = string.Format(CultureInfo.InvariantCulture,
                        "Warning! Metadata missmatching between two metadata values.\r\n" +
                        "Region: {0} Command: {1}\r\nValue '{2}'\r\n" +
                        "Region: {3} Command: {4}\r\nValue '{5}'",
                        exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, exifToolDataConvertThis.Parameter,
                        exifToolDataPrevious.Region, exifToolDataPrevious.Command, exifToolDataPrevious.Parameter);
                error += warning;
                ExiftoolTagsWarning_Write(exifToolDataPrevious, exifToolDataConvertThis, warning);
            }

            int prioirty = MetadataReadPrioity.Get(exifToolDataConvertThis.Region, exifToolDataConvertThis.Command, compositeTag);
            int highestPrioity = MetadataReadPrioity.GetCompositeTagsHighestPrioity(compositeTag);
            MetadataReadPrioity.SetCompositeTagsHighestPrioity(compositeTag, prioirty);

            if (!exifToolDataPrevious.HasValueSet || prioirty > highestPrioity) return newValue;
            else return oldValue;
        }
        #endregion

        #region Read Metadata

        #region Read fullFilePath
        public Metadata Read(MetadataBrokerType broker, string fullFilePath)
        {
            List<String> files = new List<String>();
            files.Add(fullFilePath);

            Read(broker, files, out List<Metadata> metaDataCollections, 
                out Dictionary<string, string> errorMessageOnFile, out string genericExiftoolErrorMessage,   
                false, false,  ProcessPriorityClass.BelowNormal);
            if (metaDataCollections.Count == 1)
            {
                return metaDataCollections[0];
            }
            else
            {
                return null;
            }
        }
        #endregion

        #region Read
        public void Read(MetadataBrokerType broker, List<string> filesToRead, 
            out List<Metadata> _metadataReadFromExiftool, 
            out Dictionary<string, string> _errorMessageOnFiles, out string genericExiftoolErrorMessage,
            bool useArguFile = false, bool showCliWindow = false, ProcessPriorityClass processPriorityClass = ProcessPriorityClass.BelowNormal)
        {
            #region Init
            Logger.Debug("Exiftool read: start");
            genericExiftoolErrorMessage = "";
            Dictionary<string, string> errorMessageOnFiles = new Dictionary<string, string>();
            _errorMessageOnFiles = errorMessageOnFiles;
            List<Metadata> metadataReadFromExiftool = new List<Metadata>();
            _metadataReadFromExiftool = metadataReadFromExiftool;

            if (filesToRead == null) return;
            if (filesToRead.Count == 0) return; 

            Dictionary<string, string> shortfilesNames = new Dictionary<string, string>();
            #endregion

            #region Find path to exiftool
            String path = NativeMethods.GetFullPathOfFile("exiftool.exe");
            if (path == null)
            {
                Logger.Debug("Exiftool read: exiftool.exe not found");
                String path2 = NativeMethods.GetFullPathOfFile("exiftool(-k).exe");
                if (path2 != null)
                {
                    throw new InvalidOperationException("Found exiftool(-k).exe. You need ro rename from exiftool(-k).exe to exiftool.exe. " + Path.Combine(path2, "exiftool(-k).exe"));
                }
            }
            #endregion 

            #region Build exiftool.exe argument parameters
            //-charset filename=cp65001 -charset exiftool=cp65001
            string arguments = "-t -a -G0:1 -s -n -P -struct ";
            //NEED USE "exiftools -struct" otherwise 'bug' in exiftool, can match region name and region rectangle, eg. list of 4 recgle and list of one name
            //can't be matched.  List names: Name1, List rectangles: R1, R2, R3, R4, where should Name1 go??? https://exiftool.org/struct.html 

            string exiftoolArgFileFullpath = "";
            if (useArguFile)
            {
                bool filesFound = false;
                exiftoolArgFileFullpath = FileHandler.GetLocalApplicationDataPath("exiftool_" + Guid.NewGuid() + ".txt", deleteOldTempFile: true);
                using (StreamWriter sw = new StreamWriter(exiftoolArgFileFullpath, false, Encoding.UTF8))
                {
                    
                    sw.WriteLine("-charset");
                    sw.WriteLine("filename=UTF8");
                    foreach (string file in filesToRead)
                    {
                        if (File.Exists(file))
                        {
                            filesFound = true;
                            sw.WriteLine(file);
                        }
                    }
                    sw.WriteLine("-execute");
                }
                arguments += "-@ \"" + exiftoolArgFileFullpath + "\"";
                if (!filesFound)
                {
                    //if (useArguFile)
                    if (File.Exists(exiftoolArgFileFullpath)) FileHandler.Delete(exiftoolArgFileFullpath, false);
                    return; // metaDataCollections;
                }
            }
            else
            {
                bool filesFound = false;
                foreach (string file in filesToRead)
                {
                    if (File.Exists(file))
                    {
                        filesFound = true;
                        string shortFileName = NativeMethods.ShortFileName(file);
                        Logger.Info("ReadMetadata: " + file + " " + shortFileName + " " + FileHandler.GetLastWriteTime(file).ToString());
                        if (!string.IsNullOrWhiteSpace(shortFileName))
                        {
                            shortfilesNames.Add(shortFileName, file);
                            arguments += "\"" + NativeMethods.ShortFileName(file) + "\" ";  //DOS Workaround for with UTF-8 filenames, like æøåÆØÅ
                        }
                    }
                }
                if (!filesFound) return; // metaDataCollections;
            }
            #endregion

            #region Init all data for start "first/each file"
            string genericExiftoolError = "";
            bool didExiftoolTimeout = false;
            CleanAllOldExiftoolDataForReadNewFile();
            Metadata metadata = null;
            DateTime? exiftoolDataFileDateModified = null;
            List<ExiftoolData> exiftoolDatas = new List<ExiftoolData>();
            #endregion

            int fileNumber = 0;
            string shortFilename = "";

            Process process = null;
            try
            {
                using (process = new Process())
                {
                    #region Start Exiftool process
                    Logger.Debug("Exiftool read: " + path + " " + arguments);

                    process.StartInfo.FileName = path;
                    process.StartInfo.Arguments = arguments;
                    process.StartInfo.UseShellExecute = false;
                    process.StartInfo.RedirectStandardOutput = true;
                    process.StartInfo.RedirectStandardError = true;
                    process.StartInfo.CreateNoWindow = !showCliWindow;
                    process.StartInfo.WindowStyle = ProcessWindowStyle.Minimized;
                    process.StartInfo.RedirectStandardInput = true;
                    process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                    #endregion

                    using (AutoResetEvent outputWaitHandle = new AutoResetEvent(false))
                    using (AutoResetEvent errorWaitHandle = new AutoResetEvent(false))
                    {
                        #region process.OutputDataReceived
                        process.OutputDataReceived += new DataReceivedEventHandler((sender, e) =>
                    {
                        try
                        {
                            String readedLineFromExiftool = e.Data;
                            try
                            {
                                if (e.Data == null)
                                {
                                    outputWaitHandle.Set();
                                    return;
                                }
                            }
                            catch //Crash often due debug, due to timeout
                            {
                                return;
                            }

                            if (isClosing)
                            {                                
                                try
                                {
                                    if (process != null)
                                    {
                                        Logger.Debug("Exiftool read: appliaction closing, force stopping Exiftool process");
                                        process.Kill();
                                    }
                                }
                                catch { }
                                finally
                                {
                                    process = null;
                                } 
                                return;
                            }

                            #region Substring(0, 9) == "======== "
                            Int32 foundToSeperatorInLine = readedLineFromExiftool.IndexOf("\t");
                            ExiftoolData exifToolData;
                            if (readedLineFromExiftool.Length >= 9 && readedLineFromExiftool.Substring(0, 9) == "======== ") //This occures only when multiple files found
                            {
                                if (metadata != null) //New file found, save all metadata found. 
                                {
                                    #region Write Metadata and trigger Event afterNewMediaFoundEvent
                                    Logger.Debug("Exiftool read: add to metadata to database: " + metadata.FileFullPath);
                                    if (metadata.Broker != MetadataBrokerType.Empty) //It's empty when rename long filenames and 8.3 filenames still equal
                                    {

                                        if (metadata.FileDateCreated != null && metadata.FileDateModified != null && metadata.FileSize != null)
                                        {
                                            CheckTimeZone(metadata, (DateTime)exiftoolDataFileDateModified, ref metadata.errors);
                                            metadataReadFromExiftool.Add(metadata); //Add list of metadatas read

                                            metadataDatabaseCache.Write(metadata);
                                            
                                            if (afterNewMediaFoundEvent != null) afterNewMediaFoundEvent.Invoke(new FileEntry(Path.Combine(metadata.FileDirectory, metadata.FileName), (DateTime)metadata.FileDateModified));
                                        }
                                        else
                                        {
                                            string error = "Was not able to read exifdata from file. This often occure when File is corrupt, deleted, locked, renamed or OneDrive download files";
                                            if (!errorMessageOnFiles.ContainsKey(metadata.FileFullPath)) errorMessageOnFiles.Add(metadata.FileFullPath, error);
                                            Logger.Warn("Error on file: " + metadata.FileFullPath + " Error:" + error);
                                        }
                                    }
                                    #endregion

                                    #region Clean up temporary data - get ready for new media file
                                    CleanAllOldExiftoolDataForReadNewFile();
                                    metadata = null; //Start with new empty data when new file found
                                    exiftoolDatas = new List<ExiftoolData>();
                                    #endregion
                                }
                                shortFilename = readedLineFromExiftool.Substring(9, readedLineFromExiftool.Length - 9).Replace("/", "\\");
                            }
                            #endregion

                            #region Init metadata with Init known data as directory, filename and last modyfied time
                            if (metadata == null)  //After loop we also check if metaData not null, to save the last file found
                            {
                                fileNumber++;
                                metadata = new Metadata(MetadataBrokerType.ExifTool); //Get ready to read a meta data

                                //When files gien as argument, sysyetm will short windows 8 names used to avoid  UTF8 filenames problems, like ÆØÅ not converted
                                string longFilename;

                                if (shortfilesNames.ContainsKey(shortFilename)) longFilename = shortfilesNames[shortFilename];
                                else longFilename = filesToRead[fileNumber - 1];

                                if (!File.Exists(longFilename))
                                    metadata.Broker = MetadataBrokerType.Empty;

                                metadata.FileName = Path.GetFileName(longFilename);
                                metadata.FileDirectory = Path.GetDirectoryName(longFilename);
                                exiftoolDataFileDateModified = FileHandler.GetLastWriteTime(Path.Combine(metadata.FileDirectory, metadata.FileName));
                                metadata.FileDateModified = exiftoolDataFileDateModified;

                                if (metadata.Broker == MetadataBrokerType.Empty)
                                {
                                    string error = "Exiftool read: File been removed or renamed. " + longFilename;
                                    genericExiftoolError += (string.IsNullOrWhiteSpace(genericExiftoolError) ? "" : "\r\n") + error;

                                    Logger.Warn(error);
                                }
                                else
                                {
                                    //For debugging
                                    exifToolData = new ExiftoolData();
                                    exifToolData.FileName = metadata.FileName;
                                    exifToolData.FileDirectory = metadata.FileDirectory;
                                    exifToolData.FileDateModified = (DateTime)exiftoolDataFileDateModified;
                                    exifToolData.Region = "FileSystem";
                                    exifToolData.Command = "FileName";
                                    exifToolData.Parameter = metadata.FileName;
                                    exifToolData.TristateValue = metadata.FileName;
                                    ConvertAndCheckStringFromString(exifToolData, oldExifToolFileName, CompositeTags.FileName, ref metadata.errors);
                                    oldExifToolFileName = new ExiftoolData(exifToolData, metadata.FileName, true);

                                    exifToolData = new ExiftoolData();
                                    exifToolData.FileName = metadata.FileName;
                                    exifToolData.FileDirectory = metadata.FileDirectory;
                                    exifToolData.FileDateModified = (DateTime)exiftoolDataFileDateModified;
                                    exifToolData.Region = "FileSystem";
                                    exifToolData.Command = "Directory";
                                    exifToolData.Parameter = metadata.FileDirectory;
                                    exifToolData.TristateValue = metadata.FileDirectory;
                                    ConvertAndCheckStringFromString(exifToolData, oldExifToolFilePath, CompositeTags.Directory, ref metadata.errors);
                                    oldExifToolFilePath = new ExiftoolData(exifToolData, metadata.FileDirectory, true);

                                    exifToolData = new ExiftoolData();
                                    exifToolData.FileName = metadata.FileName;
                                    exifToolData.FileDirectory = metadata.FileDirectory;
                                    exifToolData.FileDateModified = (DateTime)exiftoolDataFileDateModified;
                                    exifToolData.Region = "FileSystem";
                                    exifToolData.Command = "FileModifyDate";
                                    exifToolData.Parameter = ((DateTime)exiftoolDataFileDateModified).ToString("yyyy:MM:dd HH:mm:ss", CultureInfo.CurrentCulture);
                                    exifToolData.TristateValue = (DateTime)exiftoolDataFileDateModified;
                                    metadata.FileDateModified = exiftoolDataFileDateModified;
                                    ConvertAndCheckDateFromString(exifToolData, oldExifToolFileModifyDate, CompositeTags.FileModificationDateTime, ref metadata.errors);
                                    oldExifToolFileModifyDate = new ExiftoolData(exifToolData, metadata.FileDateModified, true);
                                }
                                //In case of crash, delete old data
                                try
                                {
                                    FileEntryBroker fileEntryBroker = new FileEntryBroker(metadata.FileFullPath, (DateTime)exiftoolDataFileDateModified, MetadataBrokerType.ExifTool);
                                    if (metadataDatabaseCache.ReadMetadataFromCacheOnly(fileEntryBroker) != null) metadataDatabaseCache.DeleteFileEntry(fileEntryBroker);
                                }
                                catch (Exception ex)
                                {
                                    Logger.Error(ex);
                                }
                            }
                            #endregion

                            if (metadata.Broker != MetadataBrokerType.Empty && foundToSeperatorInLine >= 0)
                            {
                                #region Fill in exifToolData struct/class
                                int regionEnd = readedLineFromExiftool.IndexOf('\t');
                                string regionType = readedLineFromExiftool.Substring(0, regionEnd);
                                int commandEnd = readedLineFromExiftool.IndexOf('\t', regionEnd + 1);
                                string command = readedLineFromExiftool.Substring(regionEnd + 1, commandEnd - regionEnd - 1);
                                string parameter = readedLineFromExiftool.Substring(commandEnd + 1, readedLineFromExiftool.Length - commandEnd - 1);

                                exifToolData = new ExiftoolData();
                                exifToolData.FileName = metadata.FileName;
                                exifToolData.FileDirectory = metadata.FileDirectory;
                                exifToolData.FileDateModified = (DateTime)exiftoolDataFileDateModified;
                                exifToolData.Region = regionType;
                                exifToolData.Command = command;
                                exifToolData.Parameter = parameter;
                                #endregion

                                #region Log all Exiftool Region, Command and Paramters
                                //Make the Command unique, due to exif delivers not unique valies 
                                string tempCommad = exifToolData.Command;
                                int uniqueNumber = 1;
                                while (exiftoolDatas.Contains(exifToolData)) exifToolData.Command = tempCommad + "(" + (uniqueNumber++) + ")";
                                exiftoolDatas.Add(exifToolData);    //Remover what's been saved to avoid 2 equal rows in database
                                metadataExiftoolDatabase.Write(exifToolData);
                                exifToolData.Command = tempCommad;  //Put original values back, after stores into database as Unique
                                #endregion

                                #region Find Composite Tag and Priority
                                string commandPriority = exifToolData.Command;
                                MetadataPriorityKey metadataPriorityKey = new MetadataPriorityKey(exifToolData.Region, exifToolData.Command);
                                if (MetadataReadPrioity.MetadataPrioityDictionary.ContainsKey(metadataPriorityKey))
                                {
                                    if (MetadataReadPrioity.MetadataPrioityDictionary[metadataPriorityKey].Composite != CompositeTags.NotDefined)
                                        commandPriority = MetadataReadPrioity.MetadataPrioityDictionary[metadataPriorityKey].Composite;
                                    else
                                        MetadataReadPrioity.MetadataPrioityDictionary.Remove(metadataPriorityKey); //Not defind, add it back later with default values 
                                }
                                #endregion

                                switch (commandPriority)
                                {
                                    #region File
                                    case CompositeTags.FileName:
                                    case "FileName":
                                    case "File Name":
                                        string longFilename;
                                        if (exifToolData.Region == "File:System")
                                        {
                                            if (shortfilesNames.ContainsKey(shortFilename)) longFilename = shortfilesNames[shortFilename];
                                            if (String.Compare(metadata.FileName, parameter, comparisonType: StringComparison.OrdinalIgnoreCase) != 0) //Convert short windows 8 filename to windows NT long filename
                                            {
                                                exifToolData.Parameter = Path.GetFileName(NativeMethods.LongFileName(Path.Combine(metadata.FileDirectory, parameter)));
                                                if (String.Compare(metadata.FileName, exifToolData.Parameter, comparisonType: StringComparison.OrdinalIgnoreCase) != 0) //Check if shortname been renamed during process and point to diffent long name
                                                {
                                                    metadata.Broker = MetadataBrokerType.Empty;
                                                    //throw new Exception("Filename doesn't match between Exiftool and reading of file, that means something went wrong with using exif tool and therefor crash");
                                                }
                                            }
                                            ConvertAndCheckStringFromString(exifToolData, oldExifToolFileName, CompositeTags.FileName, ref metadata.errors);
                                            oldExifToolFileName = new ExiftoolData(exifToolData, metadata.FileName, true);
                                        }
                                        break;
                                    case CompositeTags.Directory:
                                        if (exifToolData.Region == "File:System")
                                        {
                                            if (metadata.FileDirectory != parameter) //Convert short windows 8 directory name to windows NT long filename
                                            {
                                                exifToolData.Parameter = Path.GetDirectoryName(NativeMethods.LongFileName(Path.Combine(parameter, metadata.FileName)));
                                                if (metadata.FileDirectory != exifToolData.Parameter) //Check if shortname been renamed during process and point to difffent long name
                                                {
                                                    metadata.Broker = MetadataBrokerType.Empty;
                                                    //throw new Exception("Filename doesn't match between Exiftool and reading of file, that means something went wrong with using exif tool and therefor crash");
                                                }
                                            }
                                            ConvertAndCheckStringFromString(exifToolData, oldExifToolFilePath, CompositeTags.Directory, ref metadata.errors);
                                            oldExifToolFileName = new ExiftoolData(exifToolData, metadata.FileDirectory, true);
                                        }
                                        break;
                                    case CompositeTags.FileModificationDateTime:
                                    case "FileModifyDate":
                                        DateTime? tempDateTimeSoNotOverwriteMilliscounds = metadata.FileDateModified;

                                        metadata.FileDateModified = ConvertAndCheckDateFromString(exifToolData, oldExifToolFileCreateDate,
                                           CompositeTags.FileCreationDateTime, ref metadata.errors);

                                        if (metadata.FileDateModified != null && tempDateTimeSoNotOverwriteMilliscounds != null) //Get store millisecounds also
                                        {
                                            TimeSpan timeSpanDiff = ((DateTime)metadata.FileDateModified - (DateTime)tempDateTimeSoNotOverwriteMilliscounds);
                                            if (Math.Abs(timeSpanDiff.TotalMilliseconds) < 1000) metadata.FileDateModified = tempDateTimeSoNotOverwriteMilliscounds;
                                        }

                                        oldExifToolFileModifyDate = new ExiftoolData(exifToolData, metadata.FileDateModified, true);
                                        break;
                                    case CompositeTags.FileAccessDateTime:
                                    case "FileAccessDate":
                                        metadata.FileDateAccessed = ConvertAndCheckDateFromString(exifToolData, oldExifToolFileAccessDate,
                                            CompositeTags.FileAccessDateTime, ref metadata.errors);
                                        oldExifToolFileAccessDate = new ExiftoolData(exifToolData, metadata.FileDateAccessed, true);
                                        break;
                                    case CompositeTags.FileCreationDateTime:
                                    case "FileCreationDate":
                                    case "FileCreateDate":
                                        DateTime? temp2DateTimeSoNotOverwriteMilliscounds = metadata.FileDateCreated;
                                        metadata.FileDateCreated = ConvertAndCheckDateFromString(exifToolData, oldExifToolFileCreateDate,
                                            CompositeTags.FileCreationDateTime, ref metadata.errors);

                                        if (metadata.FileDateCreated != null && temp2DateTimeSoNotOverwriteMilliscounds != null) //Get store millisecounds also
                                        {
                                            TimeSpan timeSpanDiff = ((DateTime)metadata.FileDateCreated - (DateTime)temp2DateTimeSoNotOverwriteMilliscounds);
                                            if (Math.Abs(timeSpanDiff.TotalMilliseconds) < 1000) metadata.FileDateCreated = temp2DateTimeSoNotOverwriteMilliscounds;
                                        }

                                        oldExifToolFileCreateDate = new ExiftoolData(exifToolData, metadata.FileDateCreated, true);
                                        break;
                                    case CompositeTags.FileSize:
                                    case "FileSize":
                                        metadata.FileSize = ConvertAndCheckLongFromString(exifToolData, oldExifToolFileSize,
                                            CompositeTags.FileSize, ref metadata.errors);
                                        oldExifToolFileSize = new ExiftoolData(exifToolData, metadata.FileSize, true);
                                        break;
                                    case CompositeTags.MIMEType:
                                    case "MIMEType":
                                        metadata.FileMimeType = ConvertAndCheckStringFromString(exifToolData, oldExifToolMIMEType,
                                            CompositeTags.MIMEType, ref metadata.errors);
                                        oldExifToolMIMEType = new ExiftoolData(exifToolData, metadata.FileMimeType, true);
                                        break;
                                    #endregion

                                    #region Camra
                                    case CompositeTags.CameraModelMake:
                                    case "Make":
                                        metadata.CameraMake = ConvertAndCheckStringFromString(exifToolData, oldExifToolMake,
                                            CompositeTags.CameraModelMake, ref metadata.errors);
                                        oldExifToolMake = new ExiftoolData(exifToolData, metadata.CameraMake, true);
                                        break;
                                    case CompositeTags.CameraModelName:
                                    case "Model":
                                        metadata.CameraModel = ConvertAndCheckStringFromString(exifToolData, oldExifToolModel,
                                            CompositeTags.CameraModelName, ref metadata.errors);
                                        oldExifToolModel = new ExiftoolData(exifToolData, metadata.CameraModel, true);
                                        break;
                                    #endregion

                                    #region Media
                                    case CompositeTags.MediaHeight:
                                    case "ImageHeight":
                                    case "ExifImageHeight":
                                    case "SourceImageHeight":
                                        if (regionType == "EXIF:IFD1")
                                        {
                                            MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, CompositeTags.Ignore);
                                            break;
                                        }
                                        metadata.MediaHeight = ConvertAndCheckIntFromString(exifToolData, oldExifToolMediaHeight,
                                            CompositeTags.MediaHeight, ref metadata.errors);
                                        oldExifToolMediaHeight = new ExiftoolData(exifToolData, metadata.MediaHeight, true);
                                        break;
                                    case CompositeTags.MediaWidth:
                                    case "ImageWidth":
                                    case "ExifImageWidth":
                                    case "SourceImageWidth":
                                        if (regionType == "EXIF:IFD1")
                                        {
                                            MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, CompositeTags.Ignore);
                                            break;
                                        }
                                        metadata.MediaWidth = ConvertAndCheckIntFromString(exifToolData, oldExifToolMediaWidth,
                                            CompositeTags.MediaWidth, ref metadata.errors);
                                        oldExifToolMediaWidth = new ExiftoolData(exifToolData, metadata.MediaWidth, true);
                                        break;
                                    case CompositeTags.DateTimeDigitized:
                                    case "Date Time Original":
                                    case "Digital Creation Date/Time":
                                    case "Date/Time Created":
                                    case "CreationTime":
                                    case "DateTimeOriginal":
                                    case "CreateDate":
                                    case "SubSecCreateDate":
                                    case "SubSecDateTimeOriginal":
                                        if (regionType == "IPTC" && command == "DateCreated")
                                        {
                                            MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, CompositeTags.Ignore);
                                            break; //IPTC:DateCreated + IPTC:TimeCreated = EXIF:CreateDate --> Only date without time, so don't read it
                                        }

                                        metadata.MediaDateTaken = ConvertAndCheckDateFromString(exifToolData, oldExifToolCreateDate,
                                            CompositeTags.DateTimeDigitized, ref metadata.errors);
                                        oldExifToolCreateDate = new ExiftoolData(exifToolData, metadata.MediaDateTaken, true);
                                        break;
                                    #endregion

                                    #region Personal
                                    case CompositeTags.ArthurStruct:
                                    case "Creator":         //XMP:XMP-dc	Creator	        [Nokia Imaging SDK]  
                                        MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, CompositeTags.ArthurStruct);
                                        List<string> arthurListStruct = StructDeSerialization.GetListOfValues(parameter, true);

                                        string arthur = string.Join(";", arthurListStruct.ToArray());

                                        exifToolData.Parameter = arthur;
                                        metadata.PersonalAuthor = ConvertAndCheckStringFromString(exifToolData, oldExifToolAuthor,
                                            CompositeTags.Arthur, ref metadata.errors);
                                        oldExifToolAuthor = new ExiftoolData(exifToolData, metadata.PersonalAuthor, true);
                                        break;
                                    case CompositeTags.Arthur:
                                    case "By-line":         //IPTC	        By-line	        string[0,32]+
                                    case "XPAuthor":        //EXIF:IFD0	    XPAuthor	    Wrong size

                                        metadata.PersonalAuthor = ConvertAndCheckStringFromString(exifToolData, oldExifToolAuthor,
                                            CompositeTags.Arthur, ref metadata.errors);
                                        oldExifToolAuthor = new ExiftoolData(exifToolData, metadata.PersonalAuthor, true);
                                        break;

                                    case CompositeTags.Description:
                                    case "ImageDescription":
                                    case "Caption-Abstract":
                                        metadata.PersonalDescription = ConvertAndCheckStringFromString(exifToolData, oldExifToolDescription,
                                            CompositeTags.Description, ref metadata.errors);
                                        oldExifToolDescription = new ExiftoolData(exifToolData, metadata.PersonalDescription, true);
                                        break;

                                    case CompositeTags.Title:
                                    case "XP Title":
                                    case "XPTitle":
                                        metadata.PersonalTitle = ConvertAndCheckStringFromString(exifToolData, oldExifToolTitle,
                                            CompositeTags.Title, ref metadata.errors);
                                        oldExifToolTitle = new ExiftoolData(exifToolData, metadata.PersonalTitle, true);
                                        break;

                                    case CompositeTags.Comment:
                                    case "User Comment":
                                    case "XP Comment":
                                    case "UserComment":
                                    case "Notes":
                                        metadata.PersonalComments = ConvertAndCheckStringFromString(exifToolData, oldExifToolComment,
                                            CompositeTags.Comment, ref metadata.errors);
                                        oldExifToolComment = new ExiftoolData(exifToolData, metadata.PersonalComments, true);
                                        break;

                                    case CompositeTags.Album:
                                    case "Headline":
                                        metadata.PersonalAlbum = ConvertAndCheckStringFromString(exifToolData, oldExifToolAlbum,
                                            CompositeTags.Album, ref metadata.errors);
                                        oldExifToolAlbum = new ExiftoolData(exifToolData, metadata.PersonalAlbum, true);
                                        break;
                                    #endregion

                                    #region Personal Rating
                                    /*
                                    Notes:
                                    RatingPercent - Setting: 5 Stars = 99, 4 stars = 75, 3 stars = 50, 2 stars = 25, 1 star = 1, 0 stars = removes tags.  
                                    Reading: 88-100+ = 5 Stars, 63-87 = 4 stars, 38-62 = 3 stars, 13-37 = 2 stars, 1-12 = 1 star, no tag or 0 = 0 star
                                    */
                                    case CompositeTags.Rating:
                                        metadata.PersonalRating = ConvertAndCheckByteFromString(exifToolData, oldExifToolRating,
                                            CompositeTags.Rating, ref metadata.errors);
                                        oldExifToolRating = new ExiftoolData(exifToolData, metadata.PersonalRating, true);

                                        //Sync with Rating Percent
                                        oldExifToolRatingPercent.TristateValue = metadata.PersonalRatingPercent;
                                        exifToolData.Parameter = (metadata.PersonalRatingPercent == null ? null : ((float)metadata.PersonalRatingPercent).ToString(CultureInfo.InvariantCulture));
                                        metadata.PersonalRatingPercent = ConvertAndCheckByteFromString(exifToolData, oldExifToolRatingPercent,
                                            CompositeTags.RatingPercent, ref metadata.errors);
                                        oldExifToolRatingPercent = new ExiftoolData(exifToolData, metadata.PersonalRatingPercent, true);
                                        break;
                                    case CompositeTags.RatingPercent:
                                        metadata.PersonalRatingPercent = ConvertAndCheckByteFromString(
                                            exifToolData, oldExifToolRatingPercent,
                                            CompositeTags.RatingPercent, ref metadata.errors);
                                        oldExifToolRatingPercent = new ExiftoolData(exifToolData, metadata.PersonalRatingPercent, true);

                                        //Sync with Rating Stars
                                        oldExifToolRating.TristateValue = metadata.PersonalRating;
                                        exifToolData.Parameter = metadata.PersonalRating == null ? null : ((byte)metadata.PersonalRating).ToString(CultureInfo.InvariantCulture);
                                        metadata.PersonalRating = ConvertAndCheckByteFromString(exifToolData, oldExifToolRating,
                                            CompositeTags.Rating, ref metadata.errors);
                                        oldExifToolRating = new ExiftoolData(exifToolData, metadata.PersonalRating, true);
                                        break;
                                    #endregion

                                    #region De-Serialization structed - Region (with 'exiftool -struct')
                                    case CompositeTags.FaceRegionIPTC:
                                    case "MyRegion":        //Struct ImageRegion IPTC ImageRegion list
                                        MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, CompositeTags.FaceRegionIPTC);
                                        RegionStructure regionIPTC = new RegionStructure();
                                        regionIPTC.RegionStructureType = RegionStructureTypes.IptcRelative; //Relative or pixel
                                        regionIPTC.Type = "Face";
                                        //{ 'RbX','RbY','RbW','RbH'} if ($$rgn{ RegionBoundary} { RbUnit} eq 'pixel')  'relative' {
                                        throw new Exception("Not implemented yet");
                                    //break;
                                    case CompositeTags.FaceRegionMicrosoft:
                                    case CompositeTags.FaceRegionMWG:
                                    case "RegionInfo":      //MWG RegionInfo
                                    case "RegionInfoMP":    //Microsoft RegionInfoMP
                                                            //De-Serialization 

                                        Logger.Debug("Face Region: " + exifToolData.Region + " " + exifToolData.Command + " " + parameter);
                                        List<RegionStructure> readRegionList = new List<RegionStructure>();

                                        if (parameter == "{Regions=}") break; //Empty list, skip
                                        if (parameter == "{RegionList=}") break; //Empty list, skip

                                        string currentRegionCompositeTag;
                                        RegionStructureTypes currentExiftoolRegionStructureType;
                                        switch (exifToolData.Command)
                                        {
                                            case "RegionInfoMP":
                                                currentRegionCompositeTag = CompositeTags.FaceRegionMicrosoft;
                                                currentExiftoolRegionStructureType = RegionStructureTypes.WindowsLivePhotoGallery;
                                                MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, currentRegionCompositeTag);

                                                break;
                                            case "RegionInfo":
                                                currentRegionCompositeTag = CompositeTags.FaceRegionMWG;
                                                currentExiftoolRegionStructureType = RegionStructureTypes.MetadataWorkingGroup;
                                                MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, currentRegionCompositeTag);

                                                break;
                                            default:
                                                throw new Exception("Not implemented yet");
                                        }

                                        RegionStructure region = null;
                                        StructDeSerialization structDeSerialization = new StructDeSerialization(parameter);
                                        StructObject structObject;
                                        string lastKnownFieldName = "";

                                        while (structDeSerialization.Read(out structObject, true))
                                        {
                                            switch (structObject.Type)
                                            {
                                                case StructTypes.EOF:
                                                    break;
                                                case StructTypes.OpeningSquareBrackets:
                                                    break;
                                                case StructTypes.ClosingSquareBrackets:
                                                    break;
                                                case StructTypes.FieldName:
                                                    Logger.Debug("Region: " + exifToolData.Region + " " + exifToolData.Command + " " + structObject.Value);
                                                    lastKnownFieldName = structObject.Value;
                                                    break;
                                                case StructTypes.OpeningCurlyBracket:
                                                    if (structObject.IsList && region == null)
                                                    {
                                                        region = new RegionStructure();
                                                        region.RegionStructureType = currentExiftoolRegionStructureType;
                                                    }
                                                    break;
                                                case StructTypes.ClosingCurlyBracket:
                                                    //break;
                                                    goto case StructTypes.Value;
                                                case StructTypes.Value:

                                                    switch (lastKnownFieldName)
                                                    {
                                                        #region WindowsLivePhotoGallery
                                                        case "Regions":
                                                            Logger.Debug("Region, Regions: " + exifToolData.Region + " " + exifToolData.Command + " " + structObject.Value);
                                                            break;
                                                        case "PersonDisplayName": //WindowsLivePhotoGallery
                                                            Logger.Debug("Region, PersonDisplayName: " + exifToolData.Region + " " + exifToolData.Command + " " + structObject.Value);
                                                            region.Name = structObject.Value;
                                                            break;

                                                        case "Rectangle": //WindowsLivePhotoGallery --> 0.086957|, 0.125926|, 0.136364|, 0.102222
                                                            try
                                                            {
                                                                string[] valueArray = structObject.Value.Split(',');
                                                                region.AreaX = float.Parse(valueArray[0], CultureInfo.InvariantCulture);
                                                                region.AreaY = float.Parse(valueArray[1], CultureInfo.InvariantCulture);
                                                                region.AreaWidth = float.Parse(valueArray[2], CultureInfo.InvariantCulture);
                                                                region.AreaHeight = float.Parse(valueArray[3], CultureInfo.InvariantCulture);
                                                            }
                                                            catch
                                                            {
                                                                region.AreaX = 0;
                                                                region.AreaY = 0;
                                                                region.AreaWidth = 1;
                                                                region.AreaHeight = 1;
                                                            }
                                                            Logger.Debug("Region, Area: " + exifToolData.Region + " " + exifToolData.Command + " " + region.ToStringDebug());
                                                            break;
                                                        #endregion

                                                        #region MetadataWorkingGroup
                                                        case "AppliedToDimensions":
                                                            break;
                                                        case "RegionList":
                                                            break;
                                                        case "Area":
                                                            break;
                                                        case "X":
                                                        case "Y":
                                                        case "H":
                                                        case "W":
                                                            if (structObject.IsList)
                                                            {
                                                                float coordinate = float.Parse(structObject.Value, CultureInfo.InvariantCulture);
                                                                switch (lastKnownFieldName)
                                                                {
                                                                    case "X":
                                                                        region.AreaX = coordinate;
                                                                        break;
                                                                    case "Y":
                                                                        region.AreaY = coordinate;
                                                                        break;
                                                                    case "H":
                                                                        region.AreaHeight = coordinate;
                                                                        break;
                                                                    case "W":
                                                                        region.AreaWidth = coordinate;
                                                                        break;
                                                                }
                                                            }
                                                            Logger.Debug("Region, " + lastKnownFieldName + ": " + exifToolData.Region + " " + exifToolData.Command + " " + structObject.Value);
                                                            break;
                                                        case "Type":
                                                            region.Type = structObject.Value;
                                                            Logger.Debug("Region, Type: " + exifToolData.Region + " " + exifToolData.Command + " " + structObject.Value);
                                                            break;
                                                        case "Name":
                                                            Logger.Debug("Region, Name: " + exifToolData.Region + " " + exifToolData.Command + " " + structObject.Value);
                                                            region.Name = structObject.Value;
                                                            break;
                                                        #endregion

                                                        default:
                                                            break; //Closing curves
                                                    }
                                                    lastKnownFieldName = "";

                                                    #region Add to Region List
                                                    switch (currentExiftoolRegionStructureType)
                                                    {
                                                        case RegionStructureTypes.WindowsLivePhotoGallery:
                                                            if (structObject.Type == StructTypes.ClosingCurlyBracket)
                                                            {
                                                                if (region != null)
                                                                {
                                                                    if (String.IsNullOrWhiteSpace(region.Type)) region.Type = "Face";

                                                                    if (!readRegionList.Contains(region))
                                                                    {
                                                                        readRegionList.Add(region);
                                                                        Logger.Debug("Region, WindowsLivePhotoGallery (Added): " + exifToolData.Region + " " + exifToolData.Command + " " + region.ToStringDebug());
                                                                    }
                                                                    else
                                                                        Logger.Debug("Region, WindowsLivePhotoGallery (Not added): " + exifToolData.Region + " " + exifToolData.Command + " " + region.ToStringDebug());
                                                                    metadata.PersonalRegionListAddIfNotExists(region);
                                                                    region = null;
                                                                }

                                                            }
                                                            break;
                                                        case RegionStructureTypes.MetadataWorkingGroup:
                                                            if (structObject.Level == 0 &&
                                                                structObject.IsList &&
                                                                structObject.Type == StructTypes.ClosingCurlyBracket)
                                                            {
                                                                if (region != null)
                                                                {
                                                                    if (String.IsNullOrWhiteSpace(region.Type)) region.Type = "Face";

                                                                    if (!readRegionList.Contains(region))
                                                                    {
                                                                        readRegionList.Add(region);
                                                                        Logger.Debug("Region, MetadataWorkingGroup (Added): " + exifToolData.Region + " " + exifToolData.Command + " " + region.ToStringDebug());
                                                                    }
                                                                    else Logger.Debug("Region, MetadataWorkingGroup (Not added): " + exifToolData.Region + " " + exifToolData.Command + " " + region.ToStringDebug());
                                                                    metadata.PersonalRegionListAddIfNotExists(region);
                                                                    region = null;
                                                                }
                                                            }
                                                            break;
                                                        default:
                                                            throw new Exception("Not implemented yet");
                                                    }
                                                    #endregion
                                                    break;
                                                default:
                                                    break;

                                            }

                                        }

                                        if (structDeSerialization.Level != -1) throw new Exception("Error in Exiftool seralization. Missing closing curves");

                                        CheckRegionList(oldRegionList, readRegionList, metadata.MediaSize, exifToolData, oldExifToolRegion, currentRegionCompositeTag, ref metadata.errors);
                                        oldRegionList = readRegionList;
                                        oldExifToolRegion = new ExiftoolData(exifToolData, readRegionList, true);

                                        break;
                                    #endregion

                                    #region De-Serialization structed - PersonalKeywordTags 
                                    case CompositeTags.KeywordsXML:
                                    case "Categories": //XML
                                        #region Read XML Categories
                                        MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, CompositeTags.KeywordsXML);

                                        parameter = parameter.Replace("&", "{andsign37827823}"); //Hack for avoid '&' sign in text when using XmlReader issue
                                        List<string> keywordListXML = new List<string>();
                                        XmlReader xmlReader = XmlReader.Create(new StringReader(
                                            "<?xml version=\"1.0\" encoding=\"utf-8\"?>\r\n" +
                                            parameter
                                            ));

                                        string tagHierarchy = "";
                                        bool assigned = false;
                                        while (xmlReader.Read())
                                        {
                                            switch (xmlReader.NodeType)
                                            {
                                                case XmlNodeType.Element:
                                                    if (xmlReader.GetAttribute("Assigned") == "1") assigned = true;
                                                    else assigned = false;
                                                    break;
                                                case XmlNodeType.Text:
                                                    if (assigned)
                                                    {
                                                        tagHierarchy += xmlReader.Value;
                                                        tagHierarchy = tagHierarchy.Replace("{andsign37827823}", "&"); //Hack for avoid '&' sign in text when using XmlReader issue
                                                        if (!keywordListXML.Contains(tagHierarchy)) keywordListXML.Add(tagHierarchy);
                                                        KeywordTag keywordTag = new KeywordTag(tagHierarchy);
                                                        metadata.PersonalKeywordTagsAddIfNotExists(keywordTag);
                                                        tagHierarchy = "";
                                                    }
                                                    else
                                                    {
                                                        tagHierarchy += xmlReader.Value + "/";
                                                    }
                                                    break;
                                                case XmlNodeType.EndElement:
                                                    break;
                                            }
                                        }


                                        CheckKeywordList(oldKeywordList, keywordListXML, exifToolData, oldExifToolKeywords, CompositeTags.KeywordsXML, ref metadata.errors);
                                        oldKeywordList = keywordListXML;
                                        oldExifToolKeywords = new ExiftoolData(exifToolData, keywordListXML, true);

                                        #endregion
                                        break;

                                    case CompositeTags.KeywordsStruct:
                                    case "Subject":
                                    case "TagsList":
                                    case "LastKeywordIPTC":
                                    case "LastKeywordXMP":
                                    case "HierarchicalSubject":
                                    case "Category":    //Home made for store in QuickTime
                                    case "Keyword":     //Home made for store in QuickTime
                                    case "Keywords":
                                    case "CatalogSets":
                                        MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, CompositeTags.KeywordsStruct);

                                        List<string> keywordListStruct;
                                        if (!parameter.StartsWith("[")) //It's not a structed list, it's just one item
                                        {
                                            keywordListStruct = new List<string>();
                                            keywordListStruct.Add(parameter);
                                        }
                                        else keywordListStruct = StructDeSerialization.GetListOfValues(parameter, false);
                                        foreach (String tag in keywordListStruct)
                                        {

                                            Logger.Debug("Keywords: " + exifToolData.Region + " " + exifToolData.Command + " " + tag);
                                            KeywordTag keywordTag = new KeywordTag(tag);
                                            metadata.PersonalKeywordTagsAddIfNotExists(keywordTag);
                                        }

                                        CheckKeywordList(oldKeywordList, keywordListStruct, exifToolData, oldExifToolKeywords, CompositeTags.KeywordsXML, ref metadata.errors);
                                        oldKeywordList = keywordListStruct;
                                        oldExifToolKeywords = new ExiftoolData(exifToolData, keywordListStruct, true);

                                        break;
                                    case CompositeTags.KeywordsMicrosoft:
                                    case "XPKeywords":
                                        MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, CompositeTags.KeywordsMicrosoft);
                                        List<string> keywordListXP = parameter.Split(';').ToList();

                                        foreach (String tag in keywordListXP)
                                        {
                                            Logger.Debug("Keywords: " + exifToolData.Region + " " + exifToolData.Command + " " + tag);
                                            KeywordTag keywordTag = new KeywordTag(tag);
                                            metadata.PersonalKeywordTagsAddIfNotExists(keywordTag);
                                        }

                                        CheckKeywordList(oldKeywordList, keywordListXP, exifToolData, oldExifToolKeywords, CompositeTags.KeywordsXML, ref metadata.errors);
                                        oldKeywordList = keywordListXP;
                                        oldExifToolKeywords = new ExiftoolData(exifToolData, keywordListXP, true);
                                        break;
                                    #endregion

                                    #region Location
                                    case CompositeTags.LocationStruct:
                                    case "LocationShown":
                                    case "LocationCreated":
                                        #region LocationCreated
                                        if (parameter == "{Sublocation=}") break; //Empty list, skip

                                        StructDeSerialization structDeSerializationLocation = new StructDeSerialization(parameter);
                                        StructObject structObjectLocation;
                                        string lastKnownFieldNameLocation = "";


                                        while (structDeSerializationLocation.Read(out structObjectLocation, true))
                                        {
                                            switch (structObjectLocation.Type)
                                            {
                                                case StructTypes.EOF:
                                                    break;
                                                case StructTypes.OpeningSquareBrackets:
                                                    break;
                                                case StructTypes.ClosingSquareBrackets:
                                                    break;
                                                case StructTypes.FieldName:
                                                    lastKnownFieldNameLocation = structObjectLocation.Value;
                                                    break;
                                                case StructTypes.OpeningCurlyBracket:
                                                    break;
                                                case StructTypes.ClosingCurlyBracket:
                                                    //break;
                                                    goto case StructTypes.Value;
                                                case StructTypes.Value:
                                                    switch (lastKnownFieldNameLocation)
                                                    {
                                                        case "Sublocation":

                                                            exifToolData.Parameter = structObjectLocation.Value;
                                                            metadata.LocationName = ConvertAndCheckStringFromString(exifToolData, oldExifToolLocationName,
                                                                CompositeTags.LocationStruct, ref metadata.errors);
                                                            oldExifToolLocationName = new ExiftoolData(exifToolData, metadata.LocationName, true);
                                                            break;
                                                        case "City":
                                                            exifToolData.Parameter = structObjectLocation.Value;
                                                            metadata.LocationCity = ConvertAndCheckStringFromString(exifToolData, oldExifToolLocationCity,
                                                                CompositeTags.LocationStruct, ref metadata.errors);
                                                            oldExifToolLocationCity = new ExiftoolData(exifToolData, metadata.LocationCity, true);
                                                            break;
                                                        case "CountryName":
                                                            exifToolData.Parameter = structObjectLocation.Value;
                                                            metadata.LocationCountry = ConvertAndCheckStringFromString(exifToolData, oldExifToolLocationCountry,
                                                                CompositeTags.LocationStruct, ref metadata.errors);
                                                            oldExifToolLocationCountry = new ExiftoolData(exifToolData, metadata.LocationCountry, true);
                                                            break;
                                                        case "ProvinceState":
                                                            exifToolData.Parameter = structObjectLocation.Value;
                                                            metadata.LocationState = ConvertAndCheckStringFromString(exifToolData, oldExifToolLocationState,
                                                                CompositeTags.LocationStruct, ref metadata.errors);
                                                            oldExifToolLocationState = new ExiftoolData(exifToolData, metadata.LocationState, true);
                                                            break;

                                                    }
                                                    lastKnownFieldNameLocation = "";
                                                    break;
                                                default:
                                                    break;

                                            }

                                        }

                                        if (structDeSerializationLocation.Level != -1) throw new Exception("Error in Exiftool seralization. Missing closing curves");
                                        #endregion

                                        break;
                                    case CompositeTags.Location:
                                    case "Sub-location":
                                        metadata.LocationName = ConvertAndCheckStringFromString(exifToolData, oldExifToolLocationName,
                                            CompositeTags.Location, ref metadata.errors);
                                        oldExifToolLocationName = new ExiftoolData(exifToolData, metadata.LocationName, true);
                                        break;
                                    case CompositeTags.City:
                                        metadata.LocationCity = ConvertAndCheckStringFromString(exifToolData, oldExifToolLocationCity,
                                            CompositeTags.City, ref metadata.errors);
                                        oldExifToolLocationCity = new ExiftoolData(exifToolData, metadata.LocationCity, true);
                                        break;
                                    case CompositeTags.State:
                                    case "Province-State":
                                        metadata.LocationState = ConvertAndCheckStringFromString(exifToolData, oldExifToolLocationState,
                                            CompositeTags.State, ref metadata.errors);
                                        oldExifToolLocationState = new ExiftoolData(exifToolData, metadata.LocationState, true);
                                        break;
                                    case CompositeTags.Country:
                                    case "Country-PrimaryLocationName":
                                        metadata.LocationCountry = ConvertAndCheckStringFromString(exifToolData, oldExifToolLocationCountry,
                                            CompositeTags.Country, ref metadata.errors);
                                        oldExifToolLocationCountry = new ExiftoolData(exifToolData, metadata.LocationCountry, true);
                                        break;
                                    #endregion

                                    #region GPS Location
                                    case "GPSAltitude":
                                    case CompositeTags.GPSAltitude:
                                        float? newAltitudeValue = ConvertAndCheckNumberFromString(exifToolData, oldExifToolGPSAltitude,
                                            CompositeTags.GPSAltitude, ref metadata.errors);

                                        metadata.LocationAltitude = newAltitudeValue;
                                        oldExifToolGPSAltitude = new ExiftoolData(exifToolData, metadata.LocationAltitude, true);
                                        break;

                                    case "GPSLatitudeRef":
                                        if (exifToolData.Parameter.ToUpper() == "S") isGPSLatitudeRef = LocationTristate.Negative;
                                        else isGPSLatitudeRef = LocationTristate.Posetiv;

                                        if (locationEXIFGPSLatitudeParameterWithoutPrefix != null)
                                        {
                                            ExiftoolData exiftoolDataLatitude = new ExiftoolData(exifToolData);
                                            exiftoolDataLatitude.Parameter = locationEXIFGPSLatitudeParameterWithoutPrefix;
                                            exiftoolDataLatitude.Region = "EXIF:GPS";
                                            exiftoolDataLatitude.Command = "GPSLatitude";

                                            if (isGPSLatitudeRef == LocationTristate.Negative && !exifToolData.Parameter.StartsWith("-"))
                                                exiftoolDataLatitude.Parameter = "-" + exiftoolDataLatitude.Parameter;

                                            float? newLatitudeValue = ConvertAndCheckNumberFromString(exiftoolDataLatitude, oldExifToolGPSLatitude,
                                                CompositeTags.GPSLatitude, ref metadata.errors);
                                            metadata.LocationLatitude = newLatitudeValue;
                                            oldExifToolGPSLatitude = new ExiftoolData(exiftoolDataLatitude, metadata.LocationLatitude, true);
                                            locationEXIFGPSLatitudeParameterWithoutPrefix = null;
                                        }
                                        break;
                                    case CompositeTags.GPSLatitude:
                                    case "GPSLatitude":
                                        //locationEXIFGPSLatitudeParameterWithoutPrefix

                                        bool foundGPSLatitudeFullValue;
                                        if (exifToolData.Region == "EXIF:GPS")
                                        {
                                            if (isGPSLatitudeRef == LocationTristate.NotSet)
                                            {
                                                //The buffer and wait for Ref value
                                                locationEXIFGPSLatitudeParameterWithoutPrefix = exifToolData.Parameter;
                                                foundGPSLatitudeFullValue = false;
                                            }
                                            else if (isGPSLatitudeRef == LocationTristate.Negative && !exifToolData.Parameter.StartsWith("-"))
                                            {
                                                exifToolData.Parameter = "-" + exifToolData.Parameter;
                                                foundGPSLatitudeFullValue = true;
                                            } else foundGPSLatitudeFullValue = true; 

                                        } else foundGPSLatitudeFullValue = true;

                                        if (foundGPSLatitudeFullValue)
                                        {
                                            float? newLatitudeValue = ConvertAndCheckNumberFromString(exifToolData, oldExifToolGPSLatitude,
                                                CompositeTags.GPSLatitude, ref metadata.errors);
                                            metadata.LocationLatitude = newLatitudeValue;
                                            oldExifToolGPSLatitude = new ExiftoolData(exifToolData, metadata.LocationLatitude, true);
                                        }
                                        break;
                                    case "GPSLongitudeRef":
                                        if (exifToolData.Parameter.ToUpper() == "W") isGPSLongitudeRef = LocationTristate.Negative;
                                        else isGPSLongitudeRef = LocationTristate.Posetiv;

                                        if (locationEXIFGPSLongitudeParameterWithoutPrefix != null)
                                        {
                                            ExiftoolData exiftoolDataLongitude = new ExiftoolData(exifToolData);
                                            exiftoolDataLongitude.Parameter = locationEXIFGPSLongitudeParameterWithoutPrefix;
                                            exiftoolDataLongitude.Region = "EXIF:GPS";
                                            exiftoolDataLongitude.Command = "GPSLongitude";

                                            if (isGPSLongitudeRef == LocationTristate.Negative && !exifToolData.Parameter.StartsWith("-"))
                                                exiftoolDataLongitude.Parameter = "-" + exiftoolDataLongitude.Parameter;
                                            
                                            float? newLongitudeValue = ConvertAndCheckNumberFromString(exiftoolDataLongitude, oldExifToolGPSLongitude,
                                                CompositeTags.GPSLongitude, ref metadata.errors);
                                            metadata.LocationLongitude = newLongitudeValue;
                                            oldExifToolGPSLongitude = new ExiftoolData(exiftoolDataLongitude, metadata.LocationLongitude, true);
                                            locationEXIFGPSLatitudeParameterWithoutPrefix = null;
                                        }

                                        break;

                                    case CompositeTags.GPSLongitude:
                                    case "GPSLongitude":
                                        bool foundGPSLongitudeFullValue;
                                        if (exifToolData.Region == "EXIF:GPS")
                                        {
                                            if (isGPSLongitudeRef == LocationTristate.NotSet)
                                            {
                                                //The buffer and wait for Ref value
                                                locationEXIFGPSLongitudeParameterWithoutPrefix = exifToolData.Parameter;
                                                foundGPSLongitudeFullValue = false;
                                            }
                                            else if (isGPSLongitudeRef == LocationTristate.Negative && !exifToolData.Parameter.StartsWith("-"))
                                            {
                                                exifToolData.Parameter = "-" + exifToolData.Parameter;
                                                foundGPSLongitudeFullValue = true;
                                            }
                                            else foundGPSLongitudeFullValue = true;
                                        }
                                        else foundGPSLongitudeFullValue = true;

                                        if (foundGPSLongitudeFullValue)
                                        {
                                            float? newLongitudeValue = ConvertAndCheckNumberFromString(exifToolData, oldExifToolGPSLongitude,
                                                CompositeTags.GPSLongitude, ref metadata.errors);
                                            metadata.LocationLongitude = newLongitudeValue;
                                            oldExifToolGPSLongitude = new ExiftoolData(exifToolData, metadata.LocationLongitude, true);
                                        }
                                        break;

                                    case CompositeTags.GPSCoordinates:
                                    case "GPS Position":
                                    case "GPSPosition":
                                        MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, CompositeTags.GPSCoordinates);

                                        string[] coordinates = exifToolData.Parameter.Split(' ');

                                        #region Latitude
                                        ExiftoolData tempExiftoolDataNewLatitude = new ExiftoolData(exifToolData, metadata.LocationLatitude, true);
                                        tempExiftoolDataNewLatitude.Parameter = coordinates[0];

                                        if (isGPSLatitudeRef == LocationTristate.Negative && !tempExiftoolDataNewLatitude.Parameter.StartsWith("-"))
                                            tempExiftoolDataNewLatitude.Parameter = "-" + tempExiftoolDataNewLatitude.Parameter;

                                        float? newLocationLatitude = ConvertAndCheckNumberFromString(tempExiftoolDataNewLatitude, oldExifToolGPSLatitude,
                                            CompositeTags.GPSLatitude, ref metadata.errors);
                                        metadata.LocationLatitude = newLocationLatitude;
                                        oldExifToolGPSLatitude = new ExiftoolData(tempExiftoolDataNewLatitude, metadata.LocationLatitude, true);
                                        #endregion

                                        #region Longitude
                                        ExiftoolData tempExiftoolDataNewLongitude = new ExiftoolData(exifToolData, metadata.LocationLongitude, true);
                                        tempExiftoolDataNewLongitude.Parameter = coordinates[1];

                                        if (isGPSLongitudeRef == LocationTristate.Negative && !tempExiftoolDataNewLongitude.Parameter.StartsWith("-"))
                                            tempExiftoolDataNewLongitude.Parameter = "-" + tempExiftoolDataNewLongitude.Parameter;

                                        float? newLocationLongitude = ConvertAndCheckNumberFromString(tempExiftoolDataNewLongitude, oldExifToolGPSLongitude,
                                            CompositeTags.GPSLongitude, ref metadata.errors);
                                        metadata.LocationLongitude = newLocationLongitude;
                                        oldExifToolGPSLongitude = new ExiftoolData(tempExiftoolDataNewLongitude, metadata.LocationLongitude, true);
                                        #endregion
                                        
                                        if (isGPSLatitudeRef == LocationTristate.NotSet && isGPSLongitudeRef == LocationTristate.NotSet)
                                        {
                                            //DEBUG
                                            //Exiftool has changed behaviour
                                            Logger.Warn("CoordinateRef not set for Region: " + exifToolData.Region + " Command: " + exifToolData.Command + " Parameter:" + exifToolData.Parameter );
                                        }
                                        break;
                                    case CompositeTags.GPSDateTime:
                                    case "GPSDateTime":
                                        if (!exifToolData.Parameter.EndsWith("Z", true, CultureInfo.InvariantCulture)) exifToolData.Parameter += "Z"; //GPS Time needs to be UTC
                                        metadata.LocationDateTime = ConvertAndCheckDateFromString(
                                            exifToolData, oldExifToolGPSDateTime,
                                            CompositeTags.GPSDateTime, ref metadata.errors);

                                        oldExifToolGPSDateTime = new ExiftoolData(exifToolData, metadata.LocationDateTime, true);
                                        break;
                                        #endregion
                                }
                                MetadataReadPrioity.Add(exifToolData.Region, exifToolData.Command, CompositeTags.NotDefined);
                            }
                        }
                        catch (Exception ex)
                        {
                            Logger.Error(ex, "EXIFTOOL READER ERROR");
                        }
                    });
                        #endregion

                        #region process.ErrorDataReceived
                        process.ErrorDataReceived += new DataReceivedEventHandler((sender, e) =>
                        {
                            try
                            {
                                if (e.Data == null)
                                {
                                    errorWaitHandle.Set();
                                    return;
                                }
                            } catch //Crash often when debug, due to timeout
                            {
                                return;
                            }
                            // Prepend line numbers to each line of the output.
                            if (!String.IsNullOrEmpty(e.Data))
                            {
                                genericExiftoolError += e.Data + "\r\n";
                                Logger.Error("EXIFTOOL READER ERROR: " + e.Data);
                            }
                        });
                        #endregion

                        #region process.Start
                        bool result = process.Start();
                        try
                        {
                           process.PriorityClass = processPriorityClass;
                        }
                        catch { }

                        process.Start();
                        process.BeginErrorReadLine();
                        process.BeginOutputReadLine();

                        int timeout = 1000 * 60 * 5; //5 minutes
                        if (process.WaitForExit(timeout) &&
                            outputWaitHandle.WaitOne(timeout) &&
                            errorWaitHandle.WaitOne(timeout))
                        {
                            // Process completed. Check process.ExitCode here.
                            #region Write last Metadata and trigger Event afterNewMediaFoundEvent
                            if (!didExiftoolTimeout && metadata != null && metadata.FileDateCreated != null && metadata.FileDateModified != null && metadata.FileSize != null) //Save the last one, remover we save everytime, when new file found
                            {
                                if (metadata.Broker != MetadataBrokerType.Empty) //It's empty when rename long filenames and 8.3 filenames still equal
                                {
                                    CheckTimeZone(metadata, (DateTime)exiftoolDataFileDateModified, ref metadata.errors);
                                    metadataReadFromExiftool.Add(metadata);

                                    metadataDatabaseCache.Write(metadata);
                                    
                                    if (afterNewMediaFoundEvent != null) afterNewMediaFoundEvent.Invoke(new FileEntry(Path.Combine(metadata.FileDirectory, metadata.FileName), (DateTime)metadata.FileDateModified));
                                }
                            }
                            else
                            {
                                if (metadata != null)
                                {
                                    string error = "Was not able to read exifdata from file. This often occure when File is corrupt, deleted, locked, renamed or OneDrive download files";
                                    if (!errorMessageOnFiles.ContainsKey(metadata.FileFullPath)) errorMessageOnFiles.Add(metadata.FileFullPath, error);
                                    Logger.Warn("Error on file: " + metadata.FileFullPath + " Error:" + error);

                                }
                            }
                            #endregion
                        }
                        else
                        {
                            // Timed out.
                            genericExiftoolError += (string.IsNullOrWhiteSpace(genericExiftoolError) ? "" : "\r\n") + "Waiting for Exiftool to completed timeouted";
                            didExiftoolTimeout = true;
                            try
                            {
                                if (process != null) process.Kill();
                            }
                            catch { }
                        }
                        #endregion
                    }
                    

                    #region Log error
                    if (didExiftoolTimeout || !string.IsNullOrWhiteSpace(genericExiftoolError) || process.ExitCode != 0)
                    {
                        //Logger.Error("ERROR: " + error);
                        throw new Exception(genericExiftoolError);
                    }
                    while (!process.HasExited) Task.Delay(100).Wait();
                    #endregion

                } //Using process
            }
            catch (Exception ex)
            {
                Logger.Error("Exiftool.Read(): " + ex.ToString() + "\r\n" + filesToRead.ToString());
                if (useArguFile) if (File.Exists(exiftoolArgFileFullpath)) FileHandler.Delete(exiftoolArgFileFullpath, false);
                
                try
                {
#if DEBUG
                    if (!didExiftoolTimeout && process != null && !process.HasExited) process.Kill();
#else
                    if (process != null && !process.HasExited) process.Kill();
#endif
                }
                catch { }

                throw ex;
            }

            if (isClosing)
            {
                try
                {
                    if (process != null) process.Kill();
                }
                catch { }
            }

            if (useArguFile) if (File.Exists(exiftoolArgFileFullpath)) FileHandler.Delete(exiftoolArgFileFullpath, false);

            genericExiftoolErrorMessage = genericExiftoolError = "";
            return; // metaDataCollections;
        }
#endregion

        #endregion

    }
}