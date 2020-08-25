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
            this.components = new System.ComponentModel.Container();
            this.butPauseResume = new System.Windows.Forms.Button();
            this.cbEffects = new System.Windows.Forms.ComboBox();
            this.butLeft = new System.Windows.Forms.Button();
            this.butRight = new System.Windows.Forms.Button();
            this.butUp = new System.Windows.Forms.Button();
            this.butDown = new System.Windows.Forms.Button();
            this.chkListenerCone = new System.Windows.Forms.CheckBox();
            this.chkListenerInnerRadius = new System.Windows.Forms.CheckBox();
            this.txtData = new System.Windows.Forms.TextBox();
            this.tbMasterVolume = new System.Windows.Forms.TrackBar();
            this.tbHelicopter = new System.Windows.Forms.TrackBar();
            this.tbMusic = new System.Windows.Forms.TrackBar();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.cbAgent = new System.Windows.Forms.ComboBox();
            this.label4 = new System.Windows.Forms.Label();
            this.label5 = new System.Windows.Forms.Label();
            this.timerUpdate = new System.Windows.Forms.Timer(this.components);
            ((System.ComponentModel.ISupportInitialize)(this.tbMasterVolume)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbHelicopter)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbMusic)).BeginInit();
            this.SuspendLayout();
            // 
            // butPauseResume
            // 
            this.butPauseResume.Location = new System.Drawing.Point(114, 307);
            this.butPauseResume.Name = "butPauseResume";
            this.butPauseResume.Size = new System.Drawing.Size(142, 23);
            this.butPauseResume.TabIndex = 0;
            this.butPauseResume.Text = "Pause / Resume";
            this.butPauseResume.UseVisualStyleBackColor = true;
            this.butPauseResume.Click += new System.EventHandler(this.ButPauseResume_Click);
            // 
            // cbEffects
            // 
            this.cbEffects.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbEffects.FormattingEnabled = true;
            this.cbEffects.Location = new System.Drawing.Point(580, 12);
            this.cbEffects.Name = "cbEffects";
            this.cbEffects.Size = new System.Drawing.Size(142, 21);
            this.cbEffects.TabIndex = 1;
            this.cbEffects.SelectedValueChanged += new System.EventHandler(this.CmbEffects_SelectedValueChanged);
            // 
            // butLeft
            // 
            this.butLeft.Location = new System.Drawing.Point(318, 103);
            this.butLeft.Name = "butLeft";
            this.butLeft.Size = new System.Drawing.Size(45, 23);
            this.butLeft.TabIndex = 2;
            this.butLeft.Text = "Left";
            this.butLeft.UseVisualStyleBackColor = true;
            this.butLeft.Click += new System.EventHandler(this.ButLeft_Click);
            // 
            // butRight
            // 
            this.butRight.Location = new System.Drawing.Point(418, 103);
            this.butRight.Name = "butRight";
            this.butRight.Size = new System.Drawing.Size(45, 23);
            this.butRight.TabIndex = 2;
            this.butRight.Text = "Right";
            this.butRight.UseVisualStyleBackColor = true;
            this.butRight.Click += new System.EventHandler(this.ButRight_Click);
            // 
            // butUp
            // 
            this.butUp.Location = new System.Drawing.Point(369, 66);
            this.butUp.Name = "butUp";
            this.butUp.Size = new System.Drawing.Size(45, 23);
            this.butUp.TabIndex = 2;
            this.butUp.Text = "Up";
            this.butUp.UseVisualStyleBackColor = true;
            this.butUp.Click += new System.EventHandler(this.ButUp_Click);
            // 
            // butDown
            // 
            this.butDown.Location = new System.Drawing.Point(369, 137);
            this.butDown.Name = "butDown";
            this.butDown.Size = new System.Drawing.Size(45, 23);
            this.butDown.TabIndex = 2;
            this.butDown.Text = "Down";
            this.butDown.UseVisualStyleBackColor = true;
            this.butDown.Click += new System.EventHandler(this.ButDown_Click);
            // 
            // chkListenerCone
            // 
            this.chkListenerCone.AutoSize = true;
            this.chkListenerCone.Location = new System.Drawing.Point(562, 66);
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
            this.chkListenerInnerRadius.Location = new System.Drawing.Point(562, 89);
            this.chkListenerInnerRadius.Name = "chkListenerInnerRadius";
            this.chkListenerInnerRadius.Size = new System.Drawing.Size(162, 17);
            this.chkListenerInnerRadius.TabIndex = 5;
            this.chkListenerInnerRadius.Text = "Toggle Listener Inner Radius";
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
            this.txtData.Size = new System.Drawing.Size(271, 216);
            this.txtData.TabIndex = 6;
            // 
            // tbMasterVolume
            // 
            this.tbMasterVolume.Location = new System.Drawing.Point(10, 234);
            this.tbMasterVolume.Maximum = 100;
            this.tbMasterVolume.Name = "tbMasterVolume";
            this.tbMasterVolume.Size = new System.Drawing.Size(353, 45);
            this.tbMasterVolume.TabIndex = 7;
            this.tbMasterVolume.TickFrequency = 5;
            this.tbMasterVolume.Scroll += new System.EventHandler(this.TbMasterVolume_Scroll);
            // 
            // tbHelicopter
            // 
            this.tbHelicopter.Location = new System.Drawing.Point(369, 285);
            this.tbHelicopter.Maximum = 100;
            this.tbHelicopter.Name = "tbHelicopter";
            this.tbHelicopter.Size = new System.Drawing.Size(353, 45);
            this.tbHelicopter.TabIndex = 8;
            this.tbHelicopter.TickFrequency = 5;
            this.tbHelicopter.Scroll += new System.EventHandler(this.TbHelicopter_Scroll);
            // 
            // tbMusic
            // 
            this.tbMusic.Location = new System.Drawing.Point(369, 234);
            this.tbMusic.Maximum = 100;
            this.tbMusic.Name = "tbMusic";
            this.tbMusic.Size = new System.Drawing.Size(353, 45);
            this.tbMusic.TabIndex = 9;
            this.tbMusic.TickFrequency = 5;
            this.tbMusic.Scroll += new System.EventHandler(this.TbMusic_Scroll);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(137, 266);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(90, 13);
            this.label1.TabIndex = 10;
            this.label1.Text = "Master Volume";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(495, 317);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(93, 13);
            this.label2.TabIndex = 10;
            this.label2.Text = "Helicopter Volume";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(505, 266);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(73, 13);
            this.label3.TabIndex = 10;
            this.label3.Text = "Music Volume";
            // 
            // cbAgent
            // 
            this.cbAgent.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbAgent.FormattingEnabled = true;
            this.cbAgent.Location = new System.Drawing.Point(580, 39);
            this.cbAgent.Name = "cbAgent";
            this.cbAgent.Size = new System.Drawing.Size(142, 21);
            this.cbAgent.TabIndex = 11;
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(501, 15);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(73, 13);
            this.label4.TabIndex = 12;
            this.label4.Text = "Reverb Effect";
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(539, 42);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(35, 13);
            this.label5.TabIndex = 12;
            this.label5.Text = "Agent";
            // 
            // timerUpdate
            // 
            this.timerUpdate.Enabled = true;
            this.timerUpdate.Tick += new System.EventHandler(this.TimerUpdate_Tick);
            // 
            // FormMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(728, 344);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.cbAgent);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.tbMusic);
            this.Controls.Add(this.tbHelicopter);
            this.Controls.Add(this.tbMasterVolume);
            this.Controls.Add(this.txtData);
            this.Controls.Add(this.chkListenerInnerRadius);
            this.Controls.Add(this.chkListenerCone);
            this.Controls.Add(this.butDown);
            this.Controls.Add(this.butUp);
            this.Controls.Add(this.butRight);
            this.Controls.Add(this.butLeft);
            this.Controls.Add(this.cbEffects);
            this.Controls.Add(this.butPauseResume);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.Name = "FormMain";
            this.Text = "SharpDX.XAudio2";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.FormMain_FormClosing);
            this.Load += new System.EventHandler(this.FormMain_Load);
            ((System.ComponentModel.ISupportInitialize)(this.tbMasterVolume)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbHelicopter)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.tbMusic)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button butPauseResume;
        private System.Windows.Forms.ComboBox cbEffects;
        private System.Windows.Forms.Button butLeft;
        private System.Windows.Forms.Button butRight;
        private System.Windows.Forms.Button butUp;
        private System.Windows.Forms.Button butDown;
        private System.Windows.Forms.CheckBox chkListenerCone;
        private System.Windows.Forms.CheckBox chkListenerInnerRadius;
        private System.Windows.Forms.TextBox txtData;
        private System.Windows.Forms.TrackBar tbMasterVolume;
        private System.Windows.Forms.TrackBar tbHelicopter;
        private System.Windows.Forms.TrackBar tbMusic;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.ComboBox cbAgent;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Timer timerUpdate;
    }
}

