// Copyright � 2009 by Christoph Richner. All rights are reserved.
// 
//   Licensed under the Apache License, Version 2.0 (the "License");
//   you may not use this file except in compliance with the License.
//   You may obtain a copy of the License at
//
//      http://www.apache.org/licenses/LICENSE-2.0
//
//   Unless required by applicable law or agreed to in writing, software
//   distributed under the License is distributed on an "AS IS" BASIS,
//   WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//   See the License for the specific language governing permissions and
//   limitations under the License.
//
// website http://www.raccoom.net, email support@raccoom.net, msn chrisdarebell@msn.com

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;

namespace Raccoom.Windows.Forms
{
    /// <summary>
    /// <c>TreeStrategyFolderBrowserProvider</c> is the shell32 interop data provider for <see cref="TreeViewFolderBrowser"/> which is based on <see cref="ROOT.CIMV2.Win32.Logicaldisk"/>, <c>Shell32</c> Interop and <see cref="Raccoom.Win32.SystemImageList"/>
    /// <seealso cref="TreeStrategyFolderBrowserProvider"/>
    /// </summary>
    /// <remarks>
    /// Shell32 does not support the .NET System.Security.Permissions system. There is no code access permission, only FileSystem ACL.
    /// </remarks>
    [System.ComponentModel.DefaultProperty("ShowAllShellObjects"), System.Drawing.ToolboxBitmap(typeof(System.Data.SqlClient.SqlDataAdapter))]
    public class TreeStrategyShell32Provider : TreeStrategyFolderBrowserProvider
    {
        #region fields

        /// <summary>Shell32 Com Object</summary>
        private Raccoom.Win32.ShellBrowser _shell = new Raccoom.Win32.ShellBrowser();

        /// <summary>drive tree node (mycomputer) root collection</summary>
        private System.Windows.Forms.TreeNodeCollection _rootCollection = null;

        /// <summary>show only filesystem</summary>
        private bool _showAllShellObjects = false;

        /// <summary>enable shell context menu</summary>
        private bool _enableContextMenu = false;

        #endregion

        #region constructors

        #endregion

        #region public interface
        new public Raccoom.Win32.ShellAPI.CSIDL RootFolder { get; set; }

        /// <summary>
        /// Enables or disables the context menu which show's the folder item's shell verbs.
        /// </summary>
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Shell32"), System.ComponentModel.Description("Specifies if the context menu is enabled."), System.ComponentModel.DefaultValue(false)]
        public bool EnableContextMenu
        {
            get { return _enableContextMenu; }
            set { _enableContextMenu = value; }
        }

        /// <summary>
        /// Gets or sets if virtual shell folders are displayed or not. virtual shell folders are system folders like control panel.
        /// </summary>
        [System.ComponentModel.Browsable(true), System.ComponentModel.Category("Shell32"), System.ComponentModel.Description("Display file system and virtual shell folders."), System.ComponentModel.DefaultValue(false)]
        public bool ShowAllShellObjects
        {
            get { return _showAllShellObjects; }
            set { _showAllShellObjects = value; }
        }

        #endregion

        #region ITreeViewFolderBrowserDataProvider Members
        public override void QueryContextMenuItems(TreeNodePath node)
        {
            if (!EnableContextMenu) return;
            //
            Raccoom.Win32.ShellItem fi = node.Tag as Raccoom.Win32.ShellItem;
            if (fi == null) return;
            ////
            //foreach (.FolderItemVerb verb in fi..Verbs())
            //{
            //    if (verb.Name.Length == 0) continue;
            //    //
            //    MenuItemShellVerb item = new MenuItemShellVerb(verb);
            //    Helper.TreeView.ContextMenu.MenuItems.Add(item);
            //}
        }

        protected override void SetIcon(TreeViewFolderBrowser treeView, TreeNodePath node)
        {
            //base.SetIcon(treeView, node);
            //Console.WriteLine("SetIcon {0} - {1}/{2} vs {3}/{4}", node.Text, node.ImageIndex, node.SelectedImageIndex, node.ImageKey, node.SelectedImageKey);
        }

        private void AddImageListImage(TreeViewFolderBrowser treeView, TreeNodePath node)
        {
            //Console.WriteLine("AddImage: " + node.Text + " " + node.ImageIndex + " " + node.SelectedImageIndex);
            if (treeView.ImageList == null) treeView.ImageList = new System.Windows.Forms.ImageList();

            if (node.ImageIndex != -1 && !node.TreeView.ImageList.Images.ContainsKey(node.ImageIndex.ToString()))
            {                
                node.TreeView.ImageList.Images.Add(node.ImageIndex.ToString(), Raccoom.Win32.ShellImageList.GetIcon(node.ImageIndex, true).ToBitmap());
            }
            if (node.ImageIndex != -1) node.ImageKey = node.ImageIndex.ToString();

            if (node.SelectedImageIndex != -1 && !node.TreeView.ImageList.Images.ContainsKey(node.SelectedImageIndex.ToString()))
            {
                node.TreeView.ImageList.Images.Add(node.SelectedImageIndex.ToString(), Raccoom.Win32.ShellImageList.GetIcon(node.SelectedImageIndex, true).ToBitmap());
            }
            if (node.SelectedImageIndex != -1) node.SelectedImageKey = node.SelectedImageIndex.ToString();
        }

        private void AddImageListResourceImages(TreeView treeView, TreeNodePath node, string imageKey, Bitmap image, string selectedImageKey, Bitmap selectedImage)
        {
            if (treeView.ImageList == null) treeView.ImageList = new System.Windows.Forms.ImageList();
            if (!node.TreeView.ImageList.Images.ContainsKey(imageKey)) node.TreeView.ImageList.Images.Add(imageKey, image);            
            node.ImageKey = imageKey;
            if (!node.TreeView.ImageList.Images.ContainsKey(selectedImageKey)) node.TreeView.ImageList.Images.Add(selectedImageKey, selectedImage);
            node.SelectedImageKey = selectedImageKey;
        }

        public override void RequestRootNode()
        {
            // do not call base class here
            // base.RequestRootNode();

            AttachSystemImageList(Helper);
            // setup up root node collection
            switch (RootFolder)
            {
                case Raccoom.Win32.ShellAPI.CSIDL.DESKTOP :
                    // create root node <Desktop>
                    TreeNodePath desktopNode = CreateTreeNode(Helper.TreeView.Nodes, null, _shell.DesktopItem);
                    AddImageListImage(Helper.TreeView, desktopNode);
                    _rootCollection = desktopNode.Nodes;
                    // enable shell objects always to fill desktop level
                    bool settingBackup = _showAllShellObjects;
                    _showAllShellObjects = true;
                    // set setting back to original value
                    _showAllShellObjects = settingBackup;

                    break;
                case  Raccoom.Win32.ShellAPI.CSIDL.DRIVES:
                    this.FillMyComputer(_shell.MyComputerItem, Helper.TreeView.Nodes, Helper);
                    break;
                default:
                    //
                    TreeNodePath rootNode = CreateTreeNode(Helper.TreeView.Nodes, null, _shell.GetSpecialFolderShellItem(RootFolder));
                    AddImageListImage(Helper.TreeView, rootNode);
                    if (!rootNode.HasDummyNode) rootNode.AddDummyNode();
                    _rootCollection = rootNode.Nodes;
                    break;
            }
        }

        


        

        const string ScannerNetworkTag = "NetworkScanner";
        const string ScannerComputerTag = "ComputerScanner";
        const string ScannerComputerShareTag = "ComputerShares";
        const string ScannerFolderTag = "FolderScanner";
        public override void RequestChildNodes(TreeNodePath parent, System.Windows.Forms.TreeViewCancelEventArgs e)
        {


            #region Add Computers
            if (parent.Tag is string && (string)parent.Tag == ScannerNetworkTag)
            {
                Trinet.Networking.NetworkCompuersAndSharesHandler networkCompuersAndSharesHandler = new Trinet.Networking.NetworkCompuersAndSharesHandler();
                networkCompuersAndSharesHandler.ScanForComputers();
                foreach (string computerName in networkCompuersAndSharesHandler.ComputerNames)
                {
                    TreeNodePath childNode = CreateTreeNode(computerName, "\\\\" + computerName, true, true);
                    parent.Nodes.Add(childNode);
                    childNode.Tag = ScannerComputerTag;
                    AddImageListResourceImages(parent.TreeView, childNode,
                        "NetworkImage", Raccoom.TreeViewFolderBrowser.DataProviders.Properties.Resources.SharedComputer,
                        "NetworkImageSelected", Raccoom.TreeViewFolderBrowser.DataProviders.Properties.Resources.SharedComputer);
                }
            }
            #endregion

            #region Add Shares
            if (parent.Tag is string && (string)parent.Tag == ScannerComputerTag)
            {
                Trinet.Networking.NetworkCompuersAndSharesHandler networkCompuersAndSharesHandler = new Trinet.Networking.NetworkCompuersAndSharesHandler();
                List<DirectoryInfo> directoryInfoList = networkCompuersAndSharesHandler.GetComputerShares(parent.Text);

                foreach (DirectoryInfo directoryInfo in directoryInfoList)
                {
                    TreeNodePath childNode = CreateTreeNode(directoryInfo.Name, directoryInfo.FullName, true, true);
                    parent.Nodes.Add(childNode);
                    childNode.Tag = ScannerComputerShareTag;
                    AddImageListResourceImages(parent.TreeView, childNode,
                        "FolderImage", Raccoom.TreeViewFolderBrowser.DataProviders.Properties.Resources.SharedFolder,
                        "FolderImageSelected", Raccoom.TreeViewFolderBrowser.DataProviders.Properties.Resources.SharedFolder);
                }
            }
            #endregion

            #region Add Folders
            if (parent.Tag is string && ((string)parent.Tag == ScannerComputerShareTag || (string)parent.Tag == ScannerFolderTag))
            {
                try
                {
                    System.IO.DirectoryInfo directoryInfoOnShare = new DirectoryInfo(parent.Path);
                    System.IO.DirectoryInfo[] directoryInfoList = directoryInfoOnShare.GetDirectories();

                    foreach (DirectoryInfo directoryInfo in directoryInfoList)
                    {
                        TreeNodePath childNode = CreateTreeNode(directoryInfo.Name, directoryInfo.FullName, true, true);
                        parent.Nodes.Add(childNode);
                        childNode.Tag = ScannerFolderTag;
                        AddImageListResourceImages(parent.TreeView, childNode,
                            "FolderImage", Raccoom.TreeViewFolderBrowser.DataProviders.Properties.Resources.SharedFolder,
                            "FolderImageSelected", Raccoom.TreeViewFolderBrowser.DataProviders.Properties.Resources.SharedFolder);
                    }
                }
                catch { }
            }
            #endregion

            
            if (parent.Tag is Raccoom.Win32.ShellItem)
            {
                #region Add ShellItems
                Raccoom.Win32.ShellItem folderItem = ((Raccoom.Win32.ShellItem)parent.Tag);
                folderItem.Expand(this.ShowFiles, true, System.IntPtr.Zero);
                
                //
                TreeNodePath node = null;
                System.IO.DriveInfo driveInfo;
                //
                foreach (Raccoom.Win32.ShellItem childFolder in folderItem.SubFolders)
                {
                    if (!_showAllShellObjects && !childFolder.IsFileSystem) continue;
                    //
                    if (DriveTypes != DriveTypes.All && childFolder.IsDisk)
                    {
                        driveInfo = new System.IO.DriveInfo(childFolder.Path);
                        //                                       
                        switch (driveInfo.DriveType)
                        {
                            case System.IO.DriveType.CDRom:
                                if ((DriveTypes & DriveTypes.CompactDisc) == 0) continue;
                                break;
                            case System.IO.DriveType.Fixed:
                                if ((DriveTypes & DriveTypes.LocalDisk) == 0) continue;
                                break;
                            case System.IO.DriveType.Network:
                                if ((DriveTypes & DriveTypes.NetworkDrive) == 0) continue;
                                break;
                            case System.IO.DriveType.NoRootDirectory:
                                if ((DriveTypes & DriveTypes.NoRootDirectory) == 0) continue;
                                break;
                            case System.IO.DriveType.Ram:
                                if ((DriveTypes & DriveTypes.RAMDisk) == 0) continue;
                                break;
                            case System.IO.DriveType.Removable:
                                if ((DriveTypes & DriveTypes.RemovableDisk) == 0) continue;
                                break;
                            case System.IO.DriveType.Unknown:
                                if ((DriveTypes & DriveTypes.NoRootDirectory) == 0) continue;
                                break;
                        }
                    }
                    //			
                    node = CreateTreeNode(null, parent, childFolder);
                    AddImageListImage(Helper.TreeView, node);
                }

                #endregion

                #region  Add Network Scanner
                if (parent.Parent == null)
                {
                    TreeNodePath childNode = CreateTreeNode("Network scanner", "Network scanner", true, true);
                    parent.Nodes.Add(childNode);
                    childNode.Tag = ScannerNetworkTag;

                    AddImageListResourceImages(parent.TreeView, childNode,
                        "ComputerImage", Raccoom.TreeViewFolderBrowser.DataProviders.Properties.Resources.SharedNetwork,
                        "ComputerImageSelected", Raccoom.TreeViewFolderBrowser.DataProviders.Properties.Resources.SharedNetwork);
                }
                #endregion

                if (!ShowFiles) return;
                //
                foreach (Raccoom.Win32.ShellItem fileItem in folderItem.SubFiles)
                {
                    node = CreateTreeNode(null, parent, fileItem);
                    AddImageListImage(Helper.TreeView, node);
                }                
            }


        }

        private System.Windows.Forms.TreeNodeCollection RequestDriveCollection()
        {
            return _rootCollection;

        }

        #endregion

        #region CreateTreeNode
        private TreeNodePath CreateTreeNode(string text, string path, bool addDummyNode, bool isSpecialFolder)
        {
            //
            if (text == "Pictures" || text == "Videos")
            {

            }
            TreeNodePath newNode = new TreeNodePath(text, isSpecialFolder);
            newNode.Path = path;
            //						            
            if (addDummyNode)
            {
                // add dummy node, otherwise there is no + sign
                newNode.AddDummyNode();
            }
            //
            return newNode;
        }
        #endregion

        #region CreateTreeNode
        protected virtual TreeNodePath CreateTreeNode(System.Windows.Forms.TreeNodeCollection parentCollection, TreeNodePath parentNode, Raccoom.Win32.ShellItem shellItem)
        {
            if (shellItem == null) throw new ArgumentNullException("shellItem");
            //
            TreeNodePath node = CreateTreeNode(parentCollection, parentNode, shellItem.Text, shellItem.Path, !shellItem.IsFolder, shellItem.HasSubfolder, !shellItem.IsFileSystem);
            node.ImageIndex = shellItem.ImageIndex;
            node.SelectedImageIndex = shellItem.SelectedImageIndex;
            node.Tag = shellItem;
            //
            shellItem.ShellItemUpdated += delegate(object sender, EventArgs e)
            {
                node.Text = shellItem.Text;
                node.ImageIndex = shellItem.ImageIndex;
                node.SelectedImageIndex = shellItem.SelectedImageIndex;                
            };
            return node;
        }
        #endregion

        #region FillMyComputer
        /// <summary>
        /// Popluates the MyComputer node
        /// </summary>
        /// <param name="folderItem"></param>
        /// <param name="parentCollection"></param>
        /// <param name="helper"></param>
        protected virtual void FillMyComputer(Raccoom.Win32.ShellItem folderItem, System.Windows.Forms.TreeNodeCollection parentCollection, TreeViewFolderBrowserNodeFactory helper)
        {
            _rootCollection = parentCollection;
            // get wmi logical disk's if we have to 			
            System.IO.DriveInfo driveInfo;
            //
            folderItem.Expand(true, true, System.IntPtr.Zero);
            //
            foreach (Raccoom.Win32.ShellItem fi in folderItem.SubFolders)
            {
                // only File System shell objects ?
                if (!_showAllShellObjects && !fi.IsFileSystem) continue;
                //
                if (DriveTypes != DriveTypes.All && fi.IsDisk)
                {
                    driveInfo = new System.IO.DriveInfo(fi.Path);
                    //                                       
                    switch (driveInfo.DriveType)
                    {
                        case System.IO.DriveType.CDRom:
                            if ((DriveTypes & DriveTypes.CompactDisc) == 0) continue;
                            break;
                        case System.IO.DriveType.Fixed:
                            if ((DriveTypes & DriveTypes.LocalDisk) == 0) continue;
                            break;
                        case System.IO.DriveType.Network:
                            if ((DriveTypes & DriveTypes.NetworkDrive) == 0) continue;
                            break;
                        case System.IO.DriveType.NoRootDirectory:
                            if ((DriveTypes & DriveTypes.NoRootDirectory) == 0) continue;
                            break;
                        case System.IO.DriveType.Ram:
                            if ((DriveTypes & DriveTypes.RAMDisk) == 0) continue;
                            break;
                        case System.IO.DriveType.Removable:
                            if ((DriveTypes & DriveTypes.RemovableDisk) == 0) continue;
                            break;
                        case System.IO.DriveType.Unknown:
                            if ((DriveTypes & DriveTypes.NoRootDirectory) == 0) continue;
                            break;
                    }
                }
                // create new node
                TreeNodePath node = CreateTreeNode(parentCollection, null, fi);
            }
        }
        #endregion


        /// <summary>
        /// Do we have to add a dummy node (+ sign)
        /// </summary>
        protected virtual bool IsFolderWithChilds(Raccoom.Win32.ShellItem fi)
        {
            return _showAllShellObjects || (fi.IsFileSystem && fi.IsFolder && !fi.IsBrowsable);
        }

        public override string ToString()
        {
            return "Shell32 Provider";
        }
    }

    /// <summary>
    /// Extends the <c>MenuItem</c> class with a Shell32.FolderItemVerb.
    /// </summary>
    public class MenuItemShellVerb : System.Windows.Forms.MenuItem
    {

    }
}