﻿using System;
using System.Windows.Forms;
using System.IO;
using Krypton.Toolkit;
using Raccoom.Windows.Forms;

namespace PhotoTagsSynchronizer
{

    public partial class MainForm : KryptonForm
    {
        #region TreeViewFolderBrowser - BeforeSelect - Click
        private void treeViewFolderBrowser1_BeforeSelect(object sender, TreeViewCancelEventArgs e)
        {
            try
            {
                if (GlobalData.IsApplicationClosing) e.Cancel = true;
                if (!GlobalData.DoNotTrigger_TreeViewFolder_BeforeAndAfterSelect)
                {
                    if (GlobalData.DoNotTrigger_TreeViewFilter_BeforeAndAfterCheck) e.Cancel = true;
                    if (GlobalData.IsPopulatingImageListViewFromFolderOrDatabaseList) e.Cancel = true;
                    else if (IsPopulatingAnything("Select Items")) e.Cancel = true;
                    else if (SaveBeforeContinue(true) == DialogResult.Cancel) e.Cancel = true;
                }
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                KryptonMessageBox.Show(ex.Message, "Syntax error...", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Error, showCtrlCopy: true);
            }
        }
        #endregion

        #region TreeViewFolderBrowser - AfterSelect - Click
        private void treeViewFolderBrowser1_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (GlobalData.DoNotTrigger_TreeViewFolder_BeforeAndAfterSelect) return;
            if (IsDragAndDropActive()) return;
            
            try
            {
                GlobalData.SearchFolder = true;
                ImageListView_FetchListOfMediaFiles_FromFolder_and_Aggregate(false, true);

                imageListView1.Focus();
            }
            catch (Exception ex)
            {
                Logger.Error(ex, "");
                KryptonMessageBox.Show("Following error occured: \r\n" + ex.Message, "Was not able to complete operation", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Error, showCtrlCopy: true);
            }
        }
        #endregion

        #region TreeViewFolderBrowser - GetNodeFolderRealPath
        private string GetNodeFolderRealPath(TreeNodePath treeNodePath)
        {
            try
            {
                if (treeNodePath == null) return "";
                string folder = treeNodePath?.Path == null ? "" : treeNodePath?.Path; //"C:\\Users\\nordl\\OneDrive\\Skrivebord"
                if (!Directory.Exists(folder))
                {
                    try
                    {
                        if (treeNodePath.Tag is Raccoom.Win32.ShellItem shellItem) folder = (shellItem == null ? "" : Raccoom.Win32.ShellItem.GetRealPath(shellItem));
                        if (folder.StartsWith("::{")) folder = "";
                    }
                    catch { }
                }
                return Directory.Exists(folder) ? folder : "";
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                KryptonMessageBox.Show(ex.Message, "Syntax error...", (KryptonMessageBoxButtons)MessageBoxButtons.OK, KryptonMessageBoxIcon.Error, showCtrlCopy: true);
                return "";
            }
        }
        #endregion

        #region TreeViewFolderBrowser - GetSelectedNodeFullRealPath() 
        private string GetSelectedNodeFullRealPath()
        {
            return GetNodeFolderRealPath(treeViewFolderBrowser1.SelectedNode as TreeNodePath);
        }
        #endregion

        #region TreeViewFolderBrowser - GetNodeFolderFullLinkPath 
        private string GetNodeFolderFullLinkPath(TreeNodePath treeNodePath)
        {
            return treeNodePath?.FullPath == null ? "" : treeNodePath?.FullPath; //"Desktop"
            //Path     "C:\\Users\\nordl\\OneDrive\\Pictures JTNs OneDrive\\a-- PhotoTags Synchronizer --a"
            //FullPath "Desktop\\This PC\\Pictures\\a-- PhotoTags Synchronizer --a"
        }
        #endregion

        #region TreeViewFolderBrowser - GetSelectedNodeFullLinkPath 
        private string GetSelectedNodeFullLinkPath()
        {
            return GetNodeFolderFullLinkPath(treeViewFolderBrowser1.SelectedNode as TreeNodePath);
        }
        #endregion 


    }
}

