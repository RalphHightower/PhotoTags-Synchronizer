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

namespace Manina.Windows.Forms
{
    public enum ExiftoolProcessStatus
    {
        WaitAction, 
        InExiftoolReadQueue,
        WaitOfflineBecomeLocal,
        ExiftoolProcessing,
        ExiftoolWillNotProcessingFileInCloud,
        FileInaccessibleOrError,
        DoNotUpdate
    }
    //JTN Added - Item File status
    public class FileStatus //: IComparer
    {
        #region Exists
        public bool FileExists { get; set; } = true;
        public bool FileInaccessibleOrError { get; set; } = false;
        public string FileErrorMessage { get; set; } = null;
        public bool IsDirty { get; set; } = true;
        #endregion

        #region Access        
        public bool IsFileLockedReadAndWrite { get; set; } = false;
        public bool IsFileLockedForRead { get; set; } = false;
        public bool HasAnyLocks
        {
            get { return IsFileLockedForRead || IsFileLockedReadAndWrite; }
        }

        public bool IsReadOnly { get; set; } = false;
        #endregion

        #region Located
        public bool IsInCloud { get; set; } = false;
        public bool IsVirtual { get; set; } = false;
        public bool IsOffline { get; set; } = false;

        public bool IsInCloudOrVirtualOrOffline
        {
            get
            {
                if (IsInCloud != IsVirtual != IsOffline)
                {
                    //DEBUG
                }
                return IsInCloud || IsVirtual || IsOffline;
            }
        }
        #endregion

        #region FileProcessStatus
        public ExiftoolProcessStatus ExiftoolProcessStatus { get; set; } = ExiftoolProcessStatus.WaitAction;
        #endregion

        #region SortValue
        private int SortValue
        {
            get 
            {
                return
                    #region Exists
                    (FileExists ? 128 : 0) +
                    (FileInaccessibleOrError ? 64 : 0) +
                    #endregion

                    #region Access
                    (IsFileLockedReadAndWrite ? 32 : 0) +
                    (IsFileLockedForRead ? 16 : 0) +
                    (IsReadOnly ? 8 : 0) +
                    #endregion

                    #region Located
                    (IsInCloud ? 4 : 0) +
                    (IsVirtual ? 4 : 0) +
                    (IsOffline ? 4 : 0) +
                    #endregion

                    #region Processes
                    (ExiftoolProcessStatus == ExiftoolProcessStatus.ExiftoolProcessing ? 2 : 0) +
                    (ExiftoolProcessStatus == ExiftoolProcessStatus.InExiftoolReadQueue ? 1 : 0);
                    #endregion
            }
        }
        #endregion

        #region Compare
        public static int Compare(FileStatus x, FileStatus y)
        {
            return x.SortValue.CompareTo(y.SortValue);
        }
        #endregion

        #region ToString
        public override string ToString()
        {
            
            string status = "";
            if (!FileExists) status = "File not exists";
            else if (FileInaccessibleOrError) status = "File is inaccessible";
            else if (IsDirty) status = "Status sill unknown";
            else if (ExiftoolProcessStatus == ExiftoolProcessStatus.WaitAction) status = "";
            else if (ExiftoolProcessStatus == ExiftoolProcessStatus.ExiftoolProcessing) status = "Exiftool processing";
            else if (ExiftoolProcessStatus == ExiftoolProcessStatus.ExiftoolWillNotProcessingFileInCloud) status = "Files is keeped offline";
            else if (ExiftoolProcessStatus == ExiftoolProcessStatus.FileInaccessibleOrError) status = "File is inaccessible";
            else if (ExiftoolProcessStatus == ExiftoolProcessStatus.InExiftoolReadQueue) status = "Wait Exiftool";
            else if (ExiftoolProcessStatus == ExiftoolProcessStatus.WaitOfflineBecomeLocal) status = "Downloading";
            else if (ExiftoolProcessStatus == ExiftoolProcessStatus.DoNotUpdate) status = "Status requested";
            
            if (IsInCloudOrVirtualOrOffline) status += (string.IsNullOrWhiteSpace(status) ? "" : ", ") + "File is offline (" + (IsInCloud ? "C":"") + (IsVirtual ? "V" : "") + (IsOffline ? "O" :"") + ")";
            else if (HasAnyLocks) status += (string.IsNullOrWhiteSpace(status) ? "" : ", ") + "File is locked (" + (IsFileLockedForRead ? "R":"") + (IsFileLockedReadAndWrite ? "W" : "")  + ")";
            if (IsReadOnly) status += (string.IsNullOrWhiteSpace(status) ? "" : ", ") + "ReadOnly";
            return status;
        }
        #endregion


    }
}