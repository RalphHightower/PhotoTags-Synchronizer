﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Windows.Forms;
using Exiftool;
using Furty.Windows.Forms;
using Manina.Windows.Forms;
using static Manina.Windows.Forms.ImageListView;

namespace PhotoTagsSynchronizer
{

    public partial class MainForm : Form
    {
        #region Copy files
        private void CopyFiles(FolderTreeView folderTreeView, StringCollection files, string targetNodeDirectory)
        {
            foreach (string oldPath in files) //Move all files to target directory 
            {
                string sourceFullFilename = oldPath;
                string filename = Path.GetFileName(sourceFullFilename);
                string targetFullFilename = Path.Combine(targetNodeDirectory, filename);

                try
                {
                    filesCutCopyPasteDrag.CopyFile(sourceFullFilename, targetFullFilename);
                }
                catch (Exception ex)
                {
                    DateTime dateTimeLastWriteTime = DateTime.Now;
                    try
                    {
                        dateTimeLastWriteTime = File.GetLastWriteTime(sourceFullFilename);
                    }
                    catch { }

                    AddError(
                        Path.GetDirectoryName(sourceFullFilename),
                        Path.GetFileName(sourceFullFilename),
                        dateTimeLastWriteTime, sourceFullFilename, targetFullFilename,
                        AddErrorFileSystemRegion, AddErrorFileSystemCopy,
                        "Failed copying file.\r\n\r\n" +
                        "Error copy file from: " + sourceFullFilename + "\r\n\r\n" +
                        "To file: " + targetFullFilename + "\r\n\r\n" +
                        "Error message: " + ex.Message + "\r\n");
                    Logger.Error("Error when copy file." + ex.Message);
                }
            }

            GlobalData.DoNotRefreshImageListView = true;
            folderTreeView.SelectedNode = currentNodeWhenStartDragging;
            GlobalData.DoNotRefreshImageListView = false;

            //FolderSelected();
            FilesSelected();
        }
        #endregion

        #region MoveFile
        private void MoveFile(FolderTreeView folderTreeView, ImageListView imageListView, string sourceFullFilename, string targetFullFilename)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<FolderTreeView, ImageListView, string, string>(MoveFile), folderTreeView, imageListView, sourceFullFilename, targetFullFilename);
                return;
            }

            GlobalData.DoNotRefreshDataGridViewWhileFileSelect = true;
            imageListView.SuspendLayout();

            try
            {
                ExiftoolWriter.WaitLockedFileToBecomeUnlocked(sourceFullFilename);
                filesCutCopyPasteDrag.MoveFile(sourceFullFilename, targetFullFilename);

                ImageListViewItem foundItem = FindItemInImageListView(imageListView.Items, sourceFullFilename);
                if (foundItem != null) imageListView.Items.Remove(foundItem);
            }
            catch (Exception ex)
            {

                DateTime dateTimeLastWriteTime = DateTime.Now;
                try
                {
                    dateTimeLastWriteTime = File.GetLastWriteTime(sourceFullFilename);
                }
                catch { }
                AddError(
                    Path.GetDirectoryName(sourceFullFilename),
                    Path.GetFileName(sourceFullFilename),
                    dateTimeLastWriteTime,
                    AddErrorFileSystemRegion, AddErrorFileSystemMove, sourceFullFilename, targetFullFilename,
                    "Failed moving file.\r\n\r\n" +
                    "From:" + sourceFullFilename + "\r\n\r\n" +
                    "To: " + targetFullFilename + "\r\n\r\n" +
                    "Error message: " + ex.Message + "\r\n");
                Logger.Error("Error when move file." + ex.Message);
            }

            imageListView.ResumeLayout();
            GlobalData.DoNotRefreshDataGridViewWhileFileSelect = false;

            GlobalData.DoNotRefreshImageListView = true;
            folderTreeView.SelectedNode = currentNodeWhenStartDragging;
            GlobalData.DoNotRefreshImageListView = false;

            //FolderSelected();
            FilesSelected();
        }
        #endregion 

        #region Move Files to target folder
        private void MoveFiles(FolderTreeView folderTreeView, ImageListView imageListView, StringCollection files, string targetNodeDirectory)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<FolderTreeView, ImageListView, StringCollection, string>(MoveFiles), folderTreeView, imageListView, files, targetNodeDirectory);
                return;
            }

            GlobalData.DoNotRefreshDataGridViewWhileFileSelect = true;
            imageListView.SuspendLayout();

            foreach (string oldPath in files) //Move all files to target directory 
            {
                string sourceFullFilename = oldPath;
                string filename = Path.GetFileName(sourceFullFilename);
                string targetFullFilename = Path.Combine(targetNodeDirectory, filename);
                try
                {
                    filesCutCopyPasteDrag.MoveFile(sourceFullFilename, targetFullFilename);

                    ImageListViewItem foundItem = FindItemInImageListView(imageListView.Items, sourceFullFilename);
                    if (foundItem != null) imageListView.Items.Remove(foundItem);
                }
                catch (Exception ex)
                {

                    DateTime dateTimeLastWriteTime = DateTime.Now;
                    try
                    {
                        dateTimeLastWriteTime = File.GetLastWriteTime(sourceFullFilename);
                    }
                    catch { }
                    AddError(
                        Path.GetDirectoryName(sourceFullFilename),
                        Path.GetFileName(sourceFullFilename),
                        dateTimeLastWriteTime,
                        AddErrorFileSystemRegion, AddErrorFileSystemMove, sourceFullFilename, targetFullFilename,
                        "Failed moving file.\r\n\r\n" +
                        "From:" + sourceFullFilename + "\r\n\r\n" +
                        "To: " + targetFullFilename + "\r\n\r\n" +
                        "Error message: " + ex.Message + "\r\n");
                    Logger.Error("Error when move file." + ex.Message);
                }
            }
            imageListView.ResumeLayout();
            GlobalData.DoNotRefreshDataGridViewWhileFileSelect = false;

            GlobalData.DoNotRefreshImageListView = true;
            folderTreeView.SelectedNode = currentNodeWhenStartDragging;
            GlobalData.DoNotRefreshImageListView = false;

            //FolderSelected();
            FilesSelected();
        }
        #endregion

        #region Move Folder
        private void MoveFolder(FolderTreeView folderTreeView, TreeNode sourceNode, TreeNode targetNode, string sourceDirectory, string targetDirectory)
        {
            if (sourceDirectory == targetDirectory) return; //Can't move into itself. No need for error message

            try
            {
                string[] allSourceFullFilenames = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);

                //----- Move all folder and files -----
                Logger.Trace("Move folder from:" + sourceDirectory + " to: " + targetDirectory);
                System.IO.Directory.Move(sourceDirectory, targetDirectory);

                //------ Clear ImageListView -----
                ImageListViewClearAll(imageListView1);

                //------ Update node tree -----
                GlobalData.DoNotRefreshImageListView = true;
                if (sourceNode != null) sourceNode.Remove();
                filesCutCopyPasteDrag.RefeshFolderTree(folderTreeView, targetNode);
                GlobalData.DoNotRefreshImageListView = false;

                //------ Update database -----
                foreach (string oldFullFilename in allSourceFullFilenames)
                {
                    string oldFilename = Path.GetFileName(oldFullFilename);
                    string newFullFilename = Path.Combine(targetDirectory, oldFilename);
                    Logger.Trace("Rename from:" + oldFullFilename + " to: " + newFullFilename);
                    databaseAndCacheMetadataExiftool.Move(Path.GetDirectoryName(oldFullFilename), Path.GetFileName(oldFullFilename), Path.GetDirectoryName(newFullFilename), Path.GetFileName(newFullFilename));
                }

                //----- Updated ImageListView with files ------
                PopulateImageListViewBasedOnSelectedFolderAndOrFilter(false, true);

            }
            catch (Exception ex)
            {
                Logger.Error("Error when move folder." + ex.Message);

                AddError(
                    sourceDirectory,
                    AddErrorFileSystemRegion, AddErrorFileSystemMoveFolder, sourceDirectory, targetDirectory,
                    "Failed moving directory.\r\n\r\n" +
                    "From: " + sourceDirectory + "\r\n\r\n" +
                    "To: " + targetDirectory + "\r\n\r\n" +
                    "Error message:\r\n" + ex.Message);

            }
        }
        #endregion

        #region Copy Folder
        private void CopyFolder(FolderTreeView folderTreeView, TreeNode targetNode, string sourceDirectory, string tagretDirectory)
        {

            string[] allSourceFullFilenames = Directory.GetFiles(sourceDirectory, "*.*", SearchOption.AllDirectories);

            //----- Create directories and sub-directories
            Directory.CreateDirectory(tagretDirectory);
            foreach (string dirPath in Directory.GetDirectories(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                try
                {
                    Directory.CreateDirectory(dirPath.Replace(sourceDirectory, tagretDirectory));
                }
                catch (SystemException ex)
                {
                    Logger.Error("Error when create directory when copy all files from folder:" + ex.Message);
                    AddError(dirPath,
                        AddErrorFileSystemRegion, AddErrorFileSystemCreateFolder, dirPath.Replace(sourceDirectory, tagretDirectory), dirPath.Replace(sourceDirectory, tagretDirectory),
                        "Failed create directory\r\n\r\n" +
                        "Directory: " + dirPath + "\r\n\r\n" +
                        "Error message: " + ex.Message + "\r\n");
                }
            }

            //Copy all the files & Replaces any files with the same name
            foreach (string sourceFullFilename in allSourceFullFilenames)
            {
                string sourceFilename = Path.GetFileName(sourceFullFilename);
                string targetFullFilename = Path.Combine(tagretDirectory, sourceFilename);
                try
                {
                    Logger.Trace("Copy from:" + sourceFullFilename + " to: " + targetFullFilename);
                    File.Copy(sourceFullFilename, sourceFullFilename.Replace(sourceDirectory, tagretDirectory), false);

                    databaseAndCacheMetadataExiftool.Copy(
                        Path.GetDirectoryName(sourceFullFilename), Path.GetFileName(sourceFullFilename),
                        Path.GetDirectoryName(targetFullFilename), Path.GetFileName(targetFullFilename));
                }
                catch (SystemException ex)
                {
                    DateTime dateTimeLastWriteTime = DateTime.Now;
                    try
                    {
                        dateTimeLastWriteTime = File.GetLastWriteTime(sourceFullFilename);
                    }
                    catch { }
                    AddError(
                        Path.GetDirectoryName(sourceFullFilename),
                        Path.GetFileName(sourceFullFilename),
                        dateTimeLastWriteTime,
                        AddErrorFileSystemRegion, AddErrorFileSystemCopy, sourceFullFilename, targetFullFilename,
                        "Failed copying file.\r\n\r\n" +
                        "Error copy file from: " + sourceFullFilename + "\r\n\r\n" +
                        "To file: " + targetFullFilename + "\r\n\r\n" +
                        "Error message: " + ex.Message + "\r\n");
                }
            }

            //------ Update node tree -----
            GlobalData.DoNotRefreshImageListView = true;
            //if (sourceNode != null) sourceNode.Remove();
            filesCutCopyPasteDrag.RefeshFolderTree(folderTreeView, targetNode);
            GlobalData.DoNotRefreshImageListView = false;
        }
        #endregion

    }
}