namespace Chart
{
    partial class Quotes
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.GroupAddressBox = new System.Windows.Forms.TextBox();
            this.PortBox = new System.Windows.Forms.TextBox();
            this.TtlBox = new System.Windows.Forms.TextBox();
            this.MedianeIntervalBox = new System.Windows.Forms.TextBox();
            this.ModeStepBox = new System.Windows.Forms.TextBox();
            this.StartButton = new System.Windows.Forms.Button();
            this.StopButton = new System.Windows.Forms.Button();
            this.Result = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // GroupAddressBox
            // 
            this.GroupAddressBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.GroupAddressBox.Location = new System.Drawing.Point(525, 11);
            this.GroupAddressBox.Name = "GroupAddressBox";
            this.GroupAddressBox.PlaceholderText = "Group address";
            this.GroupAddressBox.Size = new System.Drawing.Size(263, 23);
            this.GroupAddressBox.TabIndex = 0;
            this.GroupAddressBox.Leave += new System.EventHandler(this.GroupAddressBox_Leave);
            // 
            // PortBox
            // 
            this.PortBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.PortBox.Location = new System.Drawing.Point(525, 40);
            this.PortBox.Name = "PortBox";
            this.PortBox.PlaceholderText = "Port";
            this.PortBox.Size = new System.Drawing.Size(263, 23);
            this.PortBox.TabIndex = 1;
            this.PortBox.Leave += new System.EventHandler(this.PortBox_Leave);
            // 
            // TtlBox
            // 
            this.TtlBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.TtlBox.Location = new System.Drawing.Point(525, 69);
            this.TtlBox.Name = "TtlBox";
            this.TtlBox.PlaceholderText = "TTL";
            this.TtlBox.Size = new System.Drawing.Size(263, 23);
            this.TtlBox.TabIndex = 2;
            this.TtlBox.Leave += new System.EventHandler(this.TtlBox_Leave);
            // 
            // MedianeIntervalBox
            // 
            this.MedianeIntervalBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.MedianeIntervalBox.Location = new System.Drawing.Point(525, 98);
            this.MedianeIntervalBox.Name = "MedianeIntervalBox";
            this.MedianeIntervalBox.PlaceholderText = "Mediane interval";
            this.MedianeIntervalBox.Size = new System.Drawing.Size(263, 23);
            this.MedianeIntervalBox.TabIndex = 3;
            this.MedianeIntervalBox.Leave += new System.EventHandler(this.MedianeIntervalBox_Leave);
            // 
            // ModeStepBox
            // 
            this.ModeStepBox.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.ModeStepBox.Location = new System.Drawing.Point(525, 127);
            this.ModeStepBox.Name = "ModeStepBox";
            this.ModeStepBox.PlaceholderText = "Mode step";
            this.ModeStepBox.Size = new System.Drawing.Size(263, 23);
            this.ModeStepBox.TabIndex = 4;
            this.ModeStepBox.Leave += new System.EventHandler(this.ModeStepBox_Leave);
            // 
            // StartButton
            // 
            this.StartButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.StartButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.StartButton.Location = new System.Drawing.Point(525, 157);
            this.StartButton.Name = "StartButton";
            this.StartButton.Size = new System.Drawing.Size(263, 23);
            this.StartButton.TabIndex = 5;
            this.StartButton.Text = "Start";
            this.StartButton.UseVisualStyleBackColor = true;
            this.StartButton.Click += new System.EventHandler(this.StartButton_Click);
            // 
            // StopButton
            // 
            this.StopButton.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.StopButton.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point);
            this.StopButton.Location = new System.Drawing.Point(525, 186);
            this.StopButton.Name = "StopButton";
            this.StopButton.Size = new System.Drawing.Size(263, 23);
            this.StopButton.TabIndex = 6;
            this.StopButton.Text = "Stop";
            this.StopButton.UseVisualStyleBackColor = true;
            this.StopButton.Click += new System.EventHandler(this.StopButton_Click);
            // 
            // Result
            // 
            this.Result.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.Result.Font = new System.Drawing.Font("Courier New", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point);
            this.Result.Location = new System.Drawing.Point(525, 215);
            this.Result.Multiline = true;
            this.Result.Name = "Result";
            this.Result.ReadOnly = true;
            this.Result.Size = new System.Drawing.Size(263, 195);
            this.Result.TabIndex = 7;
            // 
            // Quotes
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Controls.Add(this.Result);
            this.Controls.Add(this.StopButton);
            this.Controls.Add(this.StartButton);
            this.Controls.Add(this.ModeStepBox);
            this.Controls.Add(this.MedianeIntervalBox);
            this.Controls.Add(this.TtlBox);
            this.Controls.Add(this.PortBox);
            this.Controls.Add(this.GroupAddressBox);
            this.Name = "Quotes";
            this.Text = "Quotes";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Quotes_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private TextBox GroupAddressBox;
        private TextBox PortBox;
        private TextBox TtlBox;
        private TextBox MedianeIntervalBox;
        private TextBox ModeStepBox;
        private Button StartButton;
        private Button StopButton;
        private TextBox Result;
    }
}