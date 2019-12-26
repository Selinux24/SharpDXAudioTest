﻿namespace SharpDXAudioTest
{
    partial class FormMain
    {
        /// <summary>
        /// Variable del diseñador necesaria.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Limpiar los recursos que se estén usando.
        /// </summary>
        /// <param name="disposing">true si los recursos administrados se deben desechar; false en caso contrario.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Código generado por el Diseñador de Windows Forms

        /// <summary>
        /// Método necesario para admitir el Diseñador. No se puede modificar
        /// el contenido de este método con el editor de código.
        /// </summary>
        private void InitializeComponent()
        {
            this.butPauseResume = new System.Windows.Forms.Button();
            this.cmbEffects = new System.Windows.Forms.ComboBox();
            this.butLeft = new System.Windows.Forms.Button();
            this.butRight = new System.Windows.Forms.Button();
            this.butUp = new System.Windows.Forms.Button();
            this.butDown = new System.Windows.Forms.Button();
            this.chkMove = new System.Windows.Forms.CheckBox();
            this.chkListenerCone = new System.Windows.Forms.CheckBox();
            this.chkListenerInnerRadius = new System.Windows.Forms.CheckBox();
            this.txtData = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // butPauseResume
            // 
            this.butPauseResume.Location = new System.Drawing.Point(646, 415);
            this.butPauseResume.Name = "butPauseResume";
            this.butPauseResume.Size = new System.Drawing.Size(142, 23);
            this.butPauseResume.TabIndex = 0;
            this.butPauseResume.Text = "Pause / Resume";
            this.butPauseResume.UseVisualStyleBackColor = true;
            this.butPauseResume.Click += new System.EventHandler(this.ButPauseResume_Click);
            // 
            // cmbEffects
            // 
            this.cmbEffects.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbEffects.FormattingEnabled = true;
            this.cmbEffects.Location = new System.Drawing.Point(646, 12);
            this.cmbEffects.Name = "cmbEffects";
            this.cmbEffects.Size = new System.Drawing.Size(142, 21);
            this.cmbEffects.TabIndex = 1;
            this.cmbEffects.SelectedValueChanged += new System.EventHandler(this.CmbEffects_SelectedValueChanged);
            // 
            // butLeft
            // 
            this.butLeft.Location = new System.Drawing.Point(330, 180);
            this.butLeft.Name = "butLeft";
            this.butLeft.Size = new System.Drawing.Size(75, 23);
            this.butLeft.TabIndex = 2;
            this.butLeft.Text = "Left";
            this.butLeft.UseVisualStyleBackColor = true;
            this.butLeft.Click += new System.EventHandler(this.ButLeft_Click);
            // 
            // butRight
            // 
            this.butRight.Location = new System.Drawing.Point(514, 180);
            this.butRight.Name = "butRight";
            this.butRight.Size = new System.Drawing.Size(75, 23);
            this.butRight.TabIndex = 2;
            this.butRight.Text = "Right";
            this.butRight.UseVisualStyleBackColor = true;
            this.butRight.Click += new System.EventHandler(this.ButRight_Click);
            // 
            // butUp
            // 
            this.butUp.Location = new System.Drawing.Point(425, 143);
            this.butUp.Name = "butUp";
            this.butUp.Size = new System.Drawing.Size(75, 23);
            this.butUp.TabIndex = 2;
            this.butUp.Text = "Up";
            this.butUp.UseVisualStyleBackColor = true;
            this.butUp.Click += new System.EventHandler(this.ButUp_Click);
            // 
            // butDown
            // 
            this.butDown.Location = new System.Drawing.Point(425, 214);
            this.butDown.Name = "butDown";
            this.butDown.Size = new System.Drawing.Size(75, 23);
            this.butDown.TabIndex = 2;
            this.butDown.Text = "Down";
            this.butDown.UseVisualStyleBackColor = true;
            this.butDown.Click += new System.EventHandler(this.ButDown_Click);
            // 
            // chkMove
            // 
            this.chkMove.AutoSize = true;
            this.chkMove.Location = new System.Drawing.Point(664, 53);
            this.chkMove.Name = "chkMove";
            this.chkMove.Size = new System.Drawing.Size(93, 17);
            this.chkMove.TabIndex = 5;
            this.chkMove.Text = "Move Listener";
            this.chkMove.UseVisualStyleBackColor = true;
            // 
            // chkListenerCone
            // 
            this.chkListenerCone.AutoSize = true;
            this.chkListenerCone.Location = new System.Drawing.Point(664, 79);
            this.chkListenerCone.Name = "chkListenerCone";
            this.chkListenerCone.Size = new System.Drawing.Size(127, 17);
            this.chkListenerCone.TabIndex = 5;
            this.chkListenerCone.Text = "Toggle Listener Cone";
            this.chkListenerCone.UseVisualStyleBackColor = true;
            this.chkListenerCone.CheckedChanged += new System.EventHandler(this.ChkListenerCone_CheckedChanged);
            // 
            // chkListenerInnerRadius
            // 
            this.chkListenerInnerRadius.AutoSize = true;
            this.chkListenerInnerRadius.Location = new System.Drawing.Point(664, 102);
            this.chkListenerInnerRadius.Name = "chkListenerInnerRadius";
            this.chkListenerInnerRadius.Size = new System.Drawing.Size(122, 17);
            this.chkListenerInnerRadius.TabIndex = 5;
            this.chkListenerInnerRadius.Text = "Toggle Inner Radius";
            this.chkListenerInnerRadius.UseVisualStyleBackColor = true;
            this.chkListenerInnerRadius.CheckedChanged += new System.EventHandler(this.ChkListenerInnerRadius_CheckedChanged);
            // 
            // txtData
            // 
            this.txtData.Font = new System.Drawing.Font("Lucida Console", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtData.Location = new System.Drawing.Point(10, 12);
            this.txtData.Multiline = true;
            this.txtData.Name = "txtData";
            this.txtData.ReadOnly = true;
            this.txtData.Size = new System.Drawing.Size(271, 425);
            this.txtData.TabIndex = 6;
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.txtData);
            this.Controls.Add(this.chkListenerInnerRadius);
            this.Controls.Add(this.chkListenerCone);
            this.Controls.Add(this.chkMove);
            this.Controls.Add(this.butDown);
            this.Controls.Add(this.butUp);
            this.Controls.Add(this.butRight);
            this.Controls.Add(this.butLeft);
            this.Controls.Add(this.cmbEffects);
            this.Controls.Add(this.butPauseResume);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FormMain";
            this.Text = "SharpDX.XAudio2";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button butPauseResume;
        private System.Windows.Forms.ComboBox cmbEffects;
        private System.Windows.Forms.Button butLeft;
        private System.Windows.Forms.Button butRight;
        private System.Windows.Forms.Button butUp;
        private System.Windows.Forms.Button butDown;
        private System.Windows.Forms.CheckBox chkMove;
        private System.Windows.Forms.CheckBox chkListenerCone;
        private System.Windows.Forms.CheckBox chkListenerInnerRadius;
        private System.Windows.Forms.TextBox txtData;
    }
}
