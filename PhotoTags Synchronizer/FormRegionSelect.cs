﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace PhotoTagsSynchronizer
{

    public partial class FormRegionSelect : Form
    {
        public delegate void RegionSelectedEvent(object sender, RegionSelectedEventArgs e);
        public event RegionSelectedEvent OnRegionSelected;
        private int RowIndex { get; set; }
        private int ColumnIndex { get; set; }

        public FormRegionSelect()
        {
            InitializeComponent();
        }

        public void SetImage(Image image, int columnIndex, int rowIndex)
        {
            ColumnIndex = columnIndex;
            RowIndex = rowIndex;
            imageBox1.Image = image;
            imageBox1.ZoomToFit();
        }

        public void SetSelection(RectangleF rectangleF)
        {
            imageBox1.SelectionRegion = rectangleF;
        }

        public void SetSelectionNone()
        {
            imageBox1.SelectNone();
        }

        public void SetImageNone()
        {
            imageBox1.Image = null;
        }

        private void FormRegionSelect_Resize(object sender, EventArgs e)
        {
            imageBox1.ZoomToFit();
        }

        private void FormRegionSelect_ResizeEnd(object sender, EventArgs e)
        {
            imageBox1.ZoomToFit();
        }

        private void imageBox1_RegionChanged(object sender, EventArgs e)
        {
            
        }

        private void imageBox1_Selected(object sender, EventArgs e)
        {
            RegionSelectedEventArgs regionSelectedEventArgs = new RegionSelectedEventArgs();
            regionSelectedEventArgs.ImageSize = imageBox1.Image.Size;
            regionSelectedEventArgs.Selection = imageBox1.SelectionRegion;
            regionSelectedEventArgs.ColumnIndex = ColumnIndex;
            regionSelectedEventArgs.RowIndex = RowIndex;
            if (OnRegionSelected != null) OnRegionSelected(this, regionSelectedEventArgs);
        }

        private void imageBox1_Selecting(object sender, Cyotek.Windows.Forms.ImageBoxCancelEventArgs e)
        {

        }
    }
}