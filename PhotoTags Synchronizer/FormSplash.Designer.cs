﻿namespace PhotoTagsSynchronizer
{
    partial class FormSplash
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.labelStatus = new System.Windows.Forms.Label();
            this.progressBar = new System.Windows.Forms.ProgressBar();
            this.textBoxWarning = new System.Windows.Forms.TextBox();
            this.labelWarnings = new System.Windows.Forms.Label();
            this.checkBoxCloseWarning = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // labelStatus
            // 
            this.labelStatus.Location = new System.Drawing.Point(92, 44);
            this.labelStatus.Name = "labelStatus";
            this.labelStatus.Size = new System.Drawing.Size(80, 20);
            this.labelStatus.TabIndex = 0;
            this.labelStatus.Text = "Processing...";
            this.labelStatus.UseWaitCursor = true;
            // 
            // progressBar
            // 
            this.progressBar.Location = new System.Drawing.Point(92, 73);
            this.progressBar.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.progressBar.Name = "progressBar";
            this.progressBar.Size = new System.Drawing.Size(399, 23);
            this.progressBar.TabIndex = 1;
            this.progressBar.UseWaitCursor = true;
            // 
            // textBoxWarning
            // 
            this.textBoxWarning.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
            | System.Windows.Forms.AnchorStyles.Left)
            | System.Windows.Forms.AnchorStyles.Right)));
            this.textBoxWarning.BackColor = System.Drawing.Color.FromArgb(((int)(((byte)(192)))), ((int)(((byte)(0)))), ((int)(((byte)(0)))));
            this.textBoxWarning.ForeColor = System.Drawing.Color.White;
            this.textBoxWarning.Location = new System.Drawing.Point(11, 125);
            this.textBoxWarning.Margin = new System.Windows.Forms.Padding(4);
            this.textBoxWarning.Multiline = true;
            this.textBoxWarning.Name = "textBoxWarning";
            this.textBoxWarning.ScrollBars = System.Windows.Forms.ScrollBars.Both;
            this.textBoxWarning.Size = new System.Drawing.Size(564, 141);
            this.textBoxWarning.TabIndex = 2;
            this.textBoxWarning.Visible = false;
            // 
            // labelWarnings
            // 
            this.labelWarnings.Location = new System.Drawing.Point(11, 97);
            this.labelWarnings.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.labelWarnings.Name = "labelWarnings";
            this.labelWarnings.Size = new System.Drawing.Size(64, 20);
            this.labelWarnings.TabIndex = 3;
            this.labelWarnings.Text = "Warnings:";
            this.labelWarnings.Visible = false;
            // 
            // checkBoxCloseWarning
            // 
            this.checkBoxCloseWarning.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.checkBoxCloseWarning.Location = new System.Drawing.Point(359, 9);
            this.checkBoxCloseWarning.Margin = new System.Windows.Forms.Padding(4);
            this.checkBoxCloseWarning.Name = "checkBoxCloseWarning";
            this.checkBoxCloseWarning.Size = new System.Drawing.Size(231, 24);
            this.checkBoxCloseWarning.TabIndex = 4;
            this.checkBoxCloseWarning.Text = "Close warning window automatically";
            this.checkBoxCloseWarning.Visible = false;
            this.checkBoxCloseWarning.CheckedChanged += new System.EventHandler(this.checkBoxCloseWarning_CheckedChanged);
            // 
            // SplashForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Font = new System.Drawing.Font("Tahoma", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));

            this.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.CausesValidation = false;
            this.ClientSize = new System.Drawing.Size(593, 272);
            this.ControlBox = false;
            this.Controls.Add(this.checkBoxCloseWarning);
            this.Controls.Add(this.labelWarnings);
            this.Controls.Add(this.textBoxWarning);
            this.Controls.Add(this.progressBar);
            this.Controls.Add(this.labelStatus);
            this.Cursor = System.Windows.Forms.Cursors.Arrow;
            this.DoubleBuffered = true;
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
            this.HelpButton = true;
            this.Margin = new System.Windows.Forms.Padding(3, 2, 3, 2);
            this.MaximizeBox = false;
            this.Name = "SplashForm";
            this.ShowIcon = false;
            this.ShowInTaskbar = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "PhotoTags Synchronizer...";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SplashForm_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label labelStatus;
        private System.Windows.Forms.ProgressBar progressBar;
        private System.Windows.Forms.TextBox textBoxWarning;
        private System.Windows.Forms.Label labelWarnings;
        private System.Windows.Forms.CheckBox checkBoxCloseWarning;
    }
}