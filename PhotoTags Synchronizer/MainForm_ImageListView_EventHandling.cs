﻿using System;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using Manina.Windows.Forms;
using MetadataLibrary;
using System.Threading;
using ApplicationAssociations;
using System.Collections.Generic;
using static Manina.Windows.Forms.ImageListView;
using FileHandeling;
using Krypton.Toolkit;
using FileDateTime;

namespace PhotoTagsSynchronizer
{

    public partial class MainForm : KryptonForm
    {
        private AutoResetEvent ReadImageOutOfMemoryWillWaitCacheEmpty = null; //When out of memory, then wait for all data ready = new AutoResetEvent(false);
        private AutoResetEvent WaitThread_PopulateTreeViewFolderFilter_Stopped = null;

        #region ImageListView - Event - Retrieve Metadata
        private void imageListView1_RetrieveItemMetadataDetails(object sender, RetrieveItemMetadataDetailsEventArgs e)
        {
            FileEntryBroker fileEntryBroker = new FileEntryBroker(e.FileName, File.GetLastWriteTime(e.FileName), MetadataBrokerType.ExifTool);
            //bool isMetadataInCache = databaseAndCacheMetadataExiftool.IsMetadataInCache(fileEntryBroker);
            Metadata metadata = databaseAndCacheMetadataExiftool.ReadMetadataFromCacheOnly(fileEntryBroker);

            try
            {
                if (metadata == null || metadata.FileName == null)
                {
                    Logger.Debug("imageListView1_RetrieveItemMetadataDetails: Metadata not found. Added to LazyLoading: " + e.FileName);
                    AddQueueLazyLoadingDataGridViewMetadataReadToCacheOrUpdateFromSoruce(fileEntryBroker);

                    e.FileMetadata = new Utility.ShellImageFileInfo(); //Tell that data is create, all is good for internal void UpdateDetailsInternal(Utility.ShellImageFileInfo info)
                    e.FileMetadata.SetPropertyStatusOnAll(PropertyStatus.Requested); //All data will be read, it's in Lazy loading queue

                    //JTN: MediaFileAttributes
                    if (!File.Exists(e.FileName) || FileHandler.IsFileInCloud(e.FileName))
                    {
                        #region Provided by FileInfo                        
                        try
                        {
                            e.FileMetadata.FileDateCreated = File.GetCreationTime(e.FileName);
                            e.FileMetadata.FileDateModified = File.GetLastWriteTime(e.FileName);
                            e.FileMetadata.FileSize = new FileInfo(e.FileName).Length;
                            DateTime? fileSmartDate = fileDateTimeReader.SmartDateTime(e.FileName, e.FileMetadata.FileDateCreated, e.FileMetadata.FileDateModified);
                            e.FileMetadata.FileSmartDate = (fileSmartDate == null ? DateTime.MinValue : (DateTime)fileSmartDate); 

                        } catch
                        {
                            e.FileMetadata.FileSize = 0;
                        }
                        e.FileMetadata.FileMimeType = Path.GetExtension(e.FileName);
                        e.FileMetadata.FileDirectory = Path.GetDirectoryName(e.FileName);
                        #endregion

                        string inCloud;
                        if (!File.Exists(e.FileName)) inCloud = "Not read, file not exists";
                        else inCloud = "Not read, file in cloud";
                        
                        #region Provided by ShellImageFileInfo, MagickImage                                
                        e.FileMetadata.CameraMake = inCloud;
                        e.FileMetadata.CameraModel = inCloud;
                        e.FileMetadata.MediaDimensions = new Size(0, 0);
                        #endregion

                        #region Provided by MagickImage, Exiftool
                        e.FileMetadata.MediaDateTaken = DateTime.MinValue;
                        e.FileMetadata.MediaTitle = inCloud;
                        e.FileMetadata.MediaDescription = inCloud;
                        e.FileMetadata.MediaComment = inCloud;
                        e.FileMetadata.MediaAuthor = inCloud;
                        e.FileMetadata.MediaRating = 0;
                        #endregion

                        #region Provided by Exiftool
                        e.FileMetadata.MediaAlbum = inCloud;
                        e.FileMetadata.LocationDateTime = DateTime.MinValue;
                        e.FileMetadata.LocationTimeZone = inCloud;
                        e.FileMetadata.LocationName = inCloud;
                        e.FileMetadata.LocationRegionState = inCloud;
                        e.FileMetadata.LocationCity = inCloud;
                        e.FileMetadata.LocationCountry = inCloud;
                        #endregion
                    }

                    #region Provided by FileInfo
                    e.FileMetadata.DisplayName = Path.GetFileName(e.FileName);
                    //e.FileMetadata.Name= e.FileName;
                    e.FileMetadata.Extension = Path.GetExtension(e.FileName);
                    e.FileMetadata.FileAttributes = FileAttributes.Normal;
                    #endregion

                     
                }
                else
                {
                    Logger.Debug("imageListView1_RetrieveItemMetadataDetails: Metadata found " + e.FileName); 
                    e.FileMetadata = new Utility.ShellImageFileInfo();

                    #region Provided by FileInfo
                    e.FileMetadata.FileDateCreated = (DateTime)metadata.FileDateCreated;
                    e.FileMetadata.FileDateModified = (DateTime)metadata.FileDateModified;

                    DateTime? fileSmartDate = fileDateTimeReader.SmartDateTime(e.FileName, e.FileMetadata.FileDateCreated, e.FileMetadata.FileDateModified);
                    e.FileMetadata.FileSmartDate = (fileSmartDate == null ? DateTime.MinValue : (DateTime)fileSmartDate);
                    if (metadata.FileSize != null) e.FileMetadata.FileSize = (long)metadata.FileSize;
                    else
                    {
                        try
                        {
                            if (File.Exists(e.FileName)) e.FileMetadata.FileSize = new System.IO.FileInfo(e.FileName).Length;
                        } catch
                        {
                            e.FileMetadata.FileSize = long.MinValue;
                        }
                    }
                    e.FileMetadata.FileMimeType = metadata.FileMimeType;
                    e.FileMetadata.FileDirectory = metadata.FileDirectory;
                    #endregion

                    #region Provided by ShellImageFileInfo, MagickImage                                
                    e.FileMetadata.CameraMake = metadata.CameraMake;
                    e.FileMetadata.CameraModel = metadata.CameraModel;
                    if (metadata.MediaWidth != null && metadata.MediaHeight != null) e.FileMetadata.MediaDimensions = new Size((int)metadata.MediaWidth, (int)metadata.MediaHeight);
                    else e.FileMetadata.MediaDimensions = new Size(0, 0);
                    #endregion

                    #region Provided by MagickImage, Exiftool
                    if (metadata.MediaDateTaken != null) e.FileMetadata.MediaDateTaken = (DateTime)metadata.MediaDateTaken;
                    else e.FileMetadata.MediaDateTaken = DateTime.MinValue;
                    e.FileMetadata.MediaTitle = metadata.PersonalTitle;
                    e.FileMetadata.MediaDescription = metadata.PersonalDescription;
                    e.FileMetadata.MediaComment = metadata.PersonalComments;
                    e.FileMetadata.MediaAuthor = metadata.PersonalAuthor;
                    e.FileMetadata.MediaRating = (byte)(metadata.PersonalRating == null ? 0 : metadata.PersonalRating);
                    #endregion

                    #region Provided by Exiftool
                    e.FileMetadata.MediaAlbum = metadata.PersonalAlbum;
                    if (metadata.LocationDateTime != null) e.FileMetadata.LocationDateTime = (DateTime)metadata.LocationDateTime;
                    else e.FileMetadata.LocationDateTime = DateTime.MinValue;
                    e.FileMetadata.LocationTimeZone = metadata.LocationTimeZoneDescription;
                    e.FileMetadata.LocationName = metadata.LocationName;
                    e.FileMetadata.LocationRegionState = metadata.LocationState;
                    e.FileMetadata.LocationCity = metadata.LocationCity;
                    e.FileMetadata.LocationCountry = metadata.LocationCountry;
                    #endregion
                }
            } catch (Exception ex)
            {
                Logger.Error(ex, "imageListView1_RetrieveItemMetadataDetails");
                if (e.FileMetadata == null) e.FileMetadata = new Utility.ShellImageFileInfo();
                e.FileMetadata.DisplayName = Path.GetFileName(e.FileName);
                e.FileMetadata.FileDirectory = Path.GetDirectoryName(e.FileName);
            }

            ((ImageListView)sender).RefreshDelay();
        }
        #endregion

        #region ImageListView - Event - Retrieve Thumbnail 
        /// <summary>
        /// Occures when ImageListView need to "load" thumbnail
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void imageListView1_RetrieveItemThumbnail(object sender, RetrieveItemThumbnailEventArgs e)
        {
            if (GlobalData.IsApplicationClosing) return;
            if (GlobalData.DoNotRefreshImageListView) return;
            if (imageListView1.IsDisposed) return;

            try
            {
                if (File.Exists(e.FileName))
                {
                    FileEntry fileEntry = new FileEntry(e.FileName, File.GetLastWriteTime(e.FileName));
                    bool isFileInCloud = FileHandler.IsFileInCloud(fileEntry.FileFullPath); 
                    bool dontReadFileFromCloud = Properties.Settings.Default.AvoidOfflineMediaFiles;

                    lock (GlobalData.ReloadAllowedFromCloudLock)
                    {
                        if (GlobalData.ReloadAllowedFromCloud != null && GlobalData.ReloadAllowedFromCloud.Contains(fileEntry))
                        {
                            GlobalData.ReloadAllowedFromCloud.Remove(fileEntry);
                            dontReadFileFromCloud = false;
                        }
                    }

                    try
                    {
                        Image thumbnail = GetThumbnailFromDatabaseUpdatedDatabaseIfNotExist(fileEntry, dontReadFileFromCloud, isFileInCloud);
                        
                        if (thumbnail != null) //Add cloud icon if needed
                        {
                            Image thumbnailWithCloudIfFromCloud = Utility.ThumbnailFromImage(thumbnail, ThumbnailMaxUpsize, Color.White, true);
                            if (isFileInCloud) //If Media is in cloud, show Icon
                            {
                                using (Graphics g = Graphics.FromImage(thumbnailWithCloudIfFromCloud)) { g.DrawImage(Properties.Resources.ImageListViewStatusFileInCloud, 0, 0); }
                            }
                            e.Thumbnail = thumbnailWithCloudIfFromCloud;
                        }
                        else
                        {
                            if (FileHandler.IsFileVirtual(fileEntry.FileFullPath)) e.Thumbnail = (Image)Properties.Resources.ImageListViewLoadErrorOneDriveNotRunning;
                            else if (isFileInCloud) e.Thumbnail = (Image)Properties.Resources.ImageListViewLoadErrorFileInCloud;
                            else e.Thumbnail = (Image)Properties.Resources.ImageListViewLoadErrorNoThumbnail;
                        }
                    }
                    catch (IOException ioe)
                    {
                        Logger.Error(ioe, "Load image error, OneDrive issues");
                        e.Thumbnail = (Image)Properties.Resources.ImageListViewLoadErrorOneDriveNotRunning;
                    }
                    catch (Exception ex)
                    {
                        Logger.Warn(ex, "Load image error");
                        e.Thumbnail = (Image)Properties.Resources.ImageListViewLoadErrorGeneral;
                    }
                }
                else
                {
                    Logger.Warn("File not exist: " + e.FileName);
                    e.Thumbnail = (Image)Properties.Resources.ImageListViewLoadErrorFileNotExist;
                }
            } catch (Exception ex)
            {
                Logger.Warn(ex, "imageListView1_RetrieveItemThumbnail failed on: " + e.FileName);
                e.Thumbnail = (Image)Properties.Resources.ImageListViewLoadErrorGeneral;
            }
        }
        #endregion

        #region ImageListView - Event - Retrieve Image 
        /// <summary>
        /// RetrieveImage occures when ImageListView will show bigger picture than Thumbnail
        /// </summary>
        /// <param name="sender">ImageListView sender</param>
        /// <param name="e">ImageListView events parameter</param>
        private void imageListView1_RetrieveImage(object sender, RetrieveItemImageEventArgs e)
        {
            if (GlobalData.IsApplicationClosing) return;
            if (imageListView1.IsDisposed) return;

            bool retry = false;
            int retryCount = 3; //In case of waiting for OneDrive to load and timeout 

            #region For 32-bit OS, where very little memory, and need to clean up
            do
            {
                try
                {
                    Image fullSizeImage = LoadMediaCoverArtPoster(e.FullFilePath);
                    e.LoadedImage = fullSizeImage;
                    e.WasImageReadFromFile = true;
                    e.DidErrorOccourLoadMedia = false;
                }
                #region OutOfMemory, IOException (OneDrive issues) - Error handling
                //This is only error handling
                //1. When I was using 32bit version, I got lot of our of memory
                //2. When OneDrive had chrased, lot of stranger errors occured
                catch (OutOfMemoryException)
                {
                    e.LoadedImage = (Image)Properties.Resources.ImageListViewLoadErrorOutOfMemory;
                    e.WasImageReadFromFile = false;
                    e.DidErrorOccourLoadMedia = true;
                    
                    ReadImageOutOfMemoryWillWaitCacheEmpty = new AutoResetEvent(false);
                    ReadImageOutOfMemoryWillWaitCacheEmpty.WaitOne(10000);
                    lock (ReadImageOutOfMemoryWillWaitCacheEmpty)
                    {
                        ReadImageOutOfMemoryWillWaitCacheEmpty = null;
                    }
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    
                    if (retryCount-- > 0) 
                        retry = true; 
                    else 
                        retry = false;
                }
                catch (IOException) //Set an error picture, OneDrive problems
                {
                    e.LoadedImage = (Image)Properties.Resources.ImageListViewLoadErrorOneDriveNotRunning;
                    e.WasImageReadFromFile = false;
                    e.DidErrorOccourLoadMedia = true;
                }
                catch (Exception)
                {
                    e.LoadedImage = (Image)Properties.Resources.ImageListViewLoadErrorGeneral;
                    e.WasImageReadFromFile = false;
                    e.DidErrorOccourLoadMedia = true;
                }
                if (e.LoadedImage == null && FileHandler.IsFileInCloud(e.FullFilePath))
                {
                    e.LoadedImage = (Image)Properties.Resources.ImageListViewLoadErrorOneDriveNotRunning;
                    e.WasImageReadFromFile = false;
                    e.DidErrorOccourLoadMedia = true;
                }
                #endregion
            } while (retry);
            #endregion 

            try
            {
                foreach (ImageListViewItem imageListViewItem in imageListView1.Items)
                {
                    if (imageListViewItem.FileFullPath == e.FullFilePath) //Only to find DateTime Modified in the stored in the ImageListView
                    {
                        UpdateImageOnFileEntryAttributeOnSelectedGrivViewInvoke(new FileEntryAttribute(e.FullFilePath, imageListViewItem.DateModified, FileEntryVersion.Current), e.LoadedImage); //Also show error thumbnail
                        break;
                    }
                }
            } catch (Exception ex)
            {
                Logger.Error(ex, "imageListView1_RetrieveImage failed on: " + e.FullFilePath);
            }

        }
        #endregion 

        #region ImageListView - Event - SelectionChanged -> FileSelected
        private void imageListView1_SelectionChanged(object sender, EventArgs e)
        {
            //if (GlobalData.IsPopulatingAnything()) return; //E.g. Populate FolderSelect
            if (GlobalData.DoNotRefreshDataGridViewWhileFileSelect) return;
            if (!GlobalData.IsPopulatingFolderSelected) SaveBeforeContinue(false);
            GroupSelectionClear();
            imageListView1.Enabled = false; //When Enabled = true, slection was cancelled during Updating the grid
            FilesSelected();
            imageListView1.Enabled = true;
            imageListView1.Focus();
            MaximizeOrRestoreWorkspaceMainCellAndChilds();
        }
        #endregion

        #region ImageListView - ClearAll
        private void ImageListViewClearAll(ImageListView imageListeView)
        {
            imageListeView.ClearSelection();
            imageListeView.Items.Clear();
            imageListeView.ClearThumbnailCache();
            imageListeView.Refresh();
        }
        #endregion

        #region ImageListView - ClearThumbnailCache
        private void ImageListViewClearThumbnailCache(ImageListView imageListeView)
        {
            imageListeView.ClearThumbnailCache();
        }
        #endregion

        #region ImageListView - FindItem
        private ImageListViewItem FindItemInImageListView(ImageListViewItemCollection imageListViewItemCollection, string fullFilename)
        {
            ImageListViewItem foundItem = null;

            foreach (ImageListViewItem item in imageListViewItemCollection)
            {
                if (item.FileFullPath == fullFilename)
                {
                    foundItem = item;
                    break;
                }
            }
            return foundItem;
        }
        #endregion

        #region ImageListView - SetItemDirty
        public void ImageListViewSetItemDirty(string fullfilename)
        {
            ImageListViewItem imageListViewItem = FindItemInImageListView(imageListView1.Items, fullfilename);
            if (imageListViewItem != null)
            {
                imageListViewItem.Dirty();
                
            }
            imageListView1.Refresh();
        }
        #endregion

        #region ImageListView - ReloadThumbnail - Filename - Invoke
        private void ImageListViewReloadThumbnailAndMetadataInvoke(ImageListView imageListView, string fullFileName)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<ImageListView, string>(ImageListViewReloadThumbnailAndMetadataInvoke), imageListView, fullFileName);
                return;
            }

            GlobalData.DoNotRefreshDataGridViewWhileFileSelect = true;
            try
            {
                ImageListViewItem item = FindItemInImageListView(imageListView1.Items, fullFileName);
                if (item != null) ImageListViewReloadThumbnailInvoke(item);
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "ImageListViewReloadThumbnailAndMetadataInvoke");
                //DID ImageListe update failed, because of thread???
            }
            GlobalData.DoNotRefreshDataGridViewWhileFileSelect = false;
            //return existAndUpdated;
        }
        #endregion

        #region ImageListView - ReloadThumbnail - ImageListViewItem - Invoke
        private void ImageListViewReloadThumbnailInvoke(ImageListViewItem imageListViewItem)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<ImageListViewItem>(ImageListViewReloadThumbnailInvoke), imageListViewItem);
                return;
            }

            imageListViewItem.BeginEdit();
            imageListViewItem.Update();
            imageListViewItem.EndEdit();
        }
        #endregion

        #region ImageListView - SuspendLayout - Invoke
        private void ImageListViewSuspendLayoutInvoke(ImageListView imageListView)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<ImageListView>(ImageListViewSuspendLayoutInvoke), imageListView);
                return;
            }

            imageListView.SuspendLayout(); 
        }
        #endregion

        #region ImageListView - ResumeLayout - Invoke
        private void ImageListViewResumeLayoutInvoke(ImageListView imageListView)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<ImageListView>(ImageListViewResumeLayoutInvoke), imageListView);
                return;
            }
            imageListView.ResumeLayout(); 
        }
        #endregion

        #region ImageListView - Populate - OpenWith - Thread
        private void PopulateImageListViewOpenWithToolStripThread(ImageListViewSelectedItemCollection imageListViewSelectedItems)
        {
            
            List<FileEntry> imageListViewFileEntryCopy = new List<FileEntry>();
            try
            {
                foreach (ImageListViewItem imageListViewItem in imageListViewSelectedItems)
                {
                    imageListViewFileEntryCopy.Add(new FileEntry(imageListViewItem.FileFullPath, imageListViewItem.DateModified)); //Avoid crash when items gets updated
                }
            }
            catch { }

            Thread threadPopulateOpenWith = new Thread(() => { PopulateImageListViewOpenWithToolStripInvoke(imageListViewFileEntryCopy); });
            threadPopulateOpenWith.Start();
        }
        #endregion 

        #region ImageListView - Populate - OpenWith - Invoke
        private void PopulateImageListViewOpenWithToolStripInvoke(List<FileEntry> imageListViewSelectedFileEntryItems)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<List<FileEntry>>(PopulateImageListViewOpenWithToolStripInvoke), imageListViewSelectedFileEntryItems);
                return;
            }

            List<string> extentions = new List<string>();

            foreach (FileEntry fileEntry in imageListViewSelectedFileEntryItems)
            {
                string extention = Path.GetExtension(fileEntry.FileFullPath).ToLower();
                if (!extentions.Contains(extention)) extentions.Add(extention);
            }

            ApplicationAssociationsHandler applicationAssociationsHandler = new ApplicationAssociationsHandler();
            List<ApplicationData> listOfCommonOpenWith = applicationAssociationsHandler.OpenWithInCommon(extentions);

            KryptonContextMenu kryptonContextMenu = new KryptonContextMenu();
            KryptonContextMenuItems kryptonContextMenuItems = new KryptonContextMenuItems();
            kryptonContextMenu.Items.Add(kryptonContextMenuItems);

            kryptonContextMenuItemsGenericOpenWithAppList.Items.Clear();
            kryptonRibbonGroupButtonHomeFileSystemOpenWith.KryptonContextMenu = null;

            if (listOfCommonOpenWith != null && listOfCommonOpenWith.Count > 0)
            {
                foreach (ApplicationData data in listOfCommonOpenWith)
                {
                    foreach (VerbLink verbLink in data.VerbLinks)
                    {
                        ApplicationData singelVerbApplicationData = new ApplicationData();
                        singelVerbApplicationData.AppIconReference = data.AppIconReference;
                        singelVerbApplicationData.ApplicationId = data.ApplicationId;
                        singelVerbApplicationData.Command = data.Command;
                        singelVerbApplicationData.FriendlyAppName = data.FriendlyAppName;
                        singelVerbApplicationData.Icon = data.Icon;
                        singelVerbApplicationData.ProgId = data.ProgId;
                        singelVerbApplicationData.AddVerb(verbLink.Verb, verbLink.Command);

                        Krypton.Toolkit.KryptonContextMenuItem kryptonContextMenuItem = new Krypton.Toolkit.KryptonContextMenuItem();
                        kryptonContextMenuItem.Text = singelVerbApplicationData.FriendlyAppName.Replace("&", "&&") + " - " + verbLink.Verb;
                        kryptonContextMenuItem.Image = new Bitmap(singelVerbApplicationData.Icon.ToBitmap(), new Size(32,32));
                        kryptonContextMenuItem.Tag = singelVerbApplicationData;
                        kryptonContextMenuItem.Click += KryptonContextMenuItemOpenWithSelectedVerb_Click;
                        kryptonContextMenuItemsGenericOpenWithAppList.Items.Add(kryptonContextMenuItem);
                        kryptonContextMenuItems.Items.Add(kryptonContextMenuItem);
                    }
                }
                kryptonRibbonGroupButtonHomeFileSystemOpenWith.KryptonContextMenu = kryptonContextMenu;
            }
        }

        
        #endregion

        #region ImageListView - Add - Item
        private void ImageListViewAddItem(string fullFilename)
        {
            imageListView1.Items.Add(fullFilename);
        }
        #endregion

        #region ImageListView - Aggregate - FromSearchFilter
        private void ImageListViewAggregateWithMediaFiles(HashSet<FileEntry> fileEntries)
        {
            //if (cacheFolderThumbnails || cacheFolderMetadatas || cacheFolderWebScraperDataSets) CacheFolder(selectedFolder, filesFoundInDirectory, recursive);
            if (Properties.Settings.Default.ImageViewLoadThumbnailOnDemandMode) imageListView1.CacheMode = CacheMode.OnDemand;
            else imageListView1.CacheMode = CacheMode.Continuous;

            ImageListViewClearAll(imageListView1);

            imageListView1.Enabled = false;
            ImageListViewSuspendLayoutInvoke(imageListView1);

            FilterVerifyer filterVerifyerFolder = new FilterVerifyer();
            int valuesCountAdded = filterVerifyerFolder.ReadValuesFromRootNodesWithChilds(treeViewFilter, FilterVerifyer.Root);

            foreach (FileEntry fileEntry in fileEntries)
            {
                if (File.Exists(fileEntry.FileFullPath))
                {
                    if (valuesCountAdded > 0) // no filter values added, no need read from database, this fjust for optimize speed
                    {
                        Metadata metadata = databaseAndCacheMetadataExiftool.ReadMetadataFromCacheOrDatabase(new FileEntryBroker(fileEntry, MetadataBrokerType.ExifTool));
                        if (filterVerifyerFolder.VerifyMetadata(metadata)) imageListView1.Items.Add(fileEntry.FileFullPath);
                    }
                    else imageListView1.Items.Add(fileEntry.FileFullPath);
                }
            }

            imageListView1.Enabled = true;
            ImageListViewResumeLayoutInvoke(imageListView1);
            
            StartThreads();
        }
        #endregion
        
        #region ImageListView - Aggregate - Rename Items
        private void UpdateImageViewListeAfterRename(ImageListView imageListView, Dictionary<string, string> renameSuccess, Dictionary<string, string> renameFailed, bool onlyRenameAddbackToListView)
        {
            //GlobalData.DoNotRefreshImageListView = true;
            GlobalData.DoNotRefreshDataGridViewWhileFileSelect = true;
            ImageListViewSuspendLayoutInvoke(imageListView);

            #region Remove items with old names
            foreach (string filename in renameSuccess.Keys)
            {
                ImageListViewItem foundItem = FindItemInImageListView(imageListView.Items, filename);
                if (foundItem != null) imageListView.Items.Remove(foundItem);
            }
            #endregion

            #region Add new renames back to list
            if (onlyRenameAddbackToListView)
            {
                foreach (string filename in renameSuccess.Values) imageListView.Items.Add(filename);
            }
            #endregion 

            #region AddErrors to Error Queue - Also Select all previous selected Items 
            foreach (string filename in renameFailed.Keys)
            {
                DateTime dateTimeLastWriteTime = DateTime.Now;
                try
                {
                    dateTimeLastWriteTime = File.GetLastWriteTime(filename);
                }
                catch { }

                AddError(
                        Path.GetDirectoryName(filename),
                        Path.GetFileName(filename),
                        dateTimeLastWriteTime,
                        AddErrorFileSystemRegion, AddErrorFileSystemMove, filename, renameFailed[filename],
                        "Failed rename " + filename + " to : " + renameFailed[filename]);

                ImageListViewItem foundItem = FindItemInImageListView(imageListView.Items, filename);
                if (foundItem != null) foundItem.Selected = true; 
            }
            #endregion 

            #region Select back all Items renamed
            if (onlyRenameAddbackToListView)
            {
                foreach (string filename in renameSuccess.Values)
                {
                    ImageListViewItem foundItem = FindItemInImageListView(imageListView.Items, filename);
                    if (foundItem != null) foundItem.Selected = true;
                }
            }
            #endregion 

            ImageListViewResumeLayoutInvoke(imageListView);
            GlobalData.DoNotRefreshDataGridViewWhileFileSelect = false;
        }
    
        #endregion 
    }
}