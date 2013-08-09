namespace X_Platform2
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.listBox1 = new System.Windows.Forms.ListBox();
            this.AlertTimer = new System.Windows.Forms.Timer(this.components);
            this.button2 = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.btnON = new System.Windows.Forms.Button();
            this.btnOff = new System.Windows.Forms.Button();
            this.button5 = new System.Windows.Forms.Button();
            this.button4 = new System.Windows.Forms.Button();
            this.nudOffSet = new System.Windows.Forms.NumericUpDown();
            this.label1 = new System.Windows.Forms.Label();
            this.groupBox2 = new System.Windows.Forms.GroupBox();
            this.chkBxOpen = new System.Windows.Forms.CheckBox();
            this.nudOpen = new System.Windows.Forms.NumericUpDown();
            this.MarketTimer = new System.Windows.Forms.Timer(this.components);
            this.ServerTimer = new System.Windows.Forms.Timer(this.components);
            this.groupBox3 = new System.Windows.Forms.GroupBox();
            this.lblOrder = new System.Windows.Forms.Label();
            this.hungTimer = new System.Windows.Forms.Timer(this.components);
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOffSet)).BeginInit();
            this.groupBox2.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.nudOpen)).BeginInit();
            this.groupBox3.SuspendLayout();
            this.SuspendLayout();
            // 
            // listBox1
            // 
            this.listBox1.BackColor = System.Drawing.Color.LightGray;
            this.listBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.listBox1.FormattingEnabled = true;
            this.listBox1.Location = new System.Drawing.Point(0, 41);
            this.listBox1.Name = "listBox1";
            this.listBox1.Size = new System.Drawing.Size(312, 160);
            this.listBox1.TabIndex = 0;
            // 
            // AlertTimer
            // 
            this.AlertTimer.Interval = 3000;
            this.AlertTimer.Tick += new System.EventHandler(this.AlertTimer_Tick);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(12, 19);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(75, 23);
            this.button2.TabIndex = 2;
            this.button2.Text = "Reset Alert";
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.btnON);
            this.groupBox1.Controls.Add(this.btnOff);
            this.groupBox1.Controls.Add(this.button5);
            this.groupBox1.Controls.Add(this.button4);
            this.groupBox1.Controls.Add(this.button2);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox1.Location = new System.Drawing.Point(0, 201);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(312, 84);
            this.groupBox1.TabIndex = 4;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Control Panel";
            // 
            // btnON
            // 
            this.btnON.BackColor = System.Drawing.SystemColors.Control;
            this.btnON.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnON.Location = new System.Drawing.Point(174, 19);
            this.btnON.Name = "btnON";
            this.btnON.Size = new System.Drawing.Size(60, 49);
            this.btnON.TabIndex = 83;
            this.btnON.Text = "ON";
            this.btnON.UseVisualStyleBackColor = false;
            this.btnON.Click += new System.EventHandler(this.btnON_Click);
            // 
            // btnOff
            // 
            this.btnOff.BackColor = System.Drawing.Color.Red;
            this.btnOff.Font = new System.Drawing.Font("Microsoft Sans Serif", 14.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOff.Location = new System.Drawing.Point(240, 19);
            this.btnOff.Name = "btnOff";
            this.btnOff.Size = new System.Drawing.Size(60, 49);
            this.btnOff.TabIndex = 82;
            this.btnOff.Text = "OFF";
            this.btnOff.UseVisualStyleBackColor = false;
            this.btnOff.Click += new System.EventHandler(this.btnOff_Click);
            // 
            // button5
            // 
            this.button5.Enabled = false;
            this.button5.Location = new System.Drawing.Point(12, 48);
            this.button5.Name = "button5";
            this.button5.Size = new System.Drawing.Size(156, 23);
            this.button5.TabIndex = 5;
            this.button5.Text = "Display Parameters";
            this.button5.UseVisualStyleBackColor = true;
            this.button5.Click += new System.EventHandler(this.button5_Click);
            // 
            // button4
            // 
            this.button4.Enabled = false;
            this.button4.Location = new System.Drawing.Point(93, 19);
            this.button4.Name = "button4";
            this.button4.Size = new System.Drawing.Size(75, 23);
            this.button4.TabIndex = 4;
            this.button4.UseVisualStyleBackColor = true;
            this.button4.Click += new System.EventHandler(this.button4_Click);
            // 
            // nudOffSet
            // 
            this.nudOffSet.BackColor = System.Drawing.Color.Yellow;
            this.nudOffSet.DecimalPlaces = 2;
            this.nudOffSet.Increment = new decimal(new int[] {
            1,
            0,
            0,
            131072});
            this.nudOffSet.Location = new System.Drawing.Point(5, 20);
            this.nudOffSet.Maximum = new decimal(new int[] {
            999,
            0,
            0,
            0});
            this.nudOffSet.Minimum = new decimal(new int[] {
            5,
            0,
            0,
            -2147483648});
            this.nudOffSet.Name = "nudOffSet";
            this.nudOffSet.ReadOnly = true;
            this.nudOffSet.Size = new System.Drawing.Size(55, 20);
            this.nudOffSet.TabIndex = 84;
            this.nudOffSet.ValueChanged += new System.EventHandler(this.nudOffSet_ValueChanged);
            // 
            // label1
            // 
            this.label1.BackColor = System.Drawing.SystemColors.ControlDark;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(60, 20);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(75, 20);
            this.label1.TabIndex = 85;
            this.label1.Text = "Limit Offset";
            this.label1.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // groupBox2
            // 
            this.groupBox2.Controls.Add(this.chkBxOpen);
            this.groupBox2.Controls.Add(this.nudOpen);
            this.groupBox2.Controls.Add(this.label1);
            this.groupBox2.Controls.Add(this.nudOffSet);
            this.groupBox2.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox2.Location = new System.Drawing.Point(0, 285);
            this.groupBox2.Name = "groupBox2";
            this.groupBox2.Size = new System.Drawing.Size(312, 56);
            this.groupBox2.TabIndex = 5;
            this.groupBox2.TabStop = false;
            this.groupBox2.Text = "Parameters";
            // 
            // chkBxOpen
            // 
            this.chkBxOpen.BackColor = System.Drawing.SystemColors.ControlDark;
            this.chkBxOpen.Location = new System.Drawing.Point(225, 20);
            this.chkBxOpen.Name = "chkBxOpen";
            this.chkBxOpen.Size = new System.Drawing.Size(80, 20);
            this.chkBxOpen.TabIndex = 87;
            this.chkBxOpen.Text = "Set Open";
            this.chkBxOpen.UseVisualStyleBackColor = false;
            this.chkBxOpen.CheckedChanged += new System.EventHandler(this.chkBxOpen_CheckedChanged);
            // 
            // nudOpen
            // 
            this.nudOpen.BackColor = System.Drawing.Color.Yellow;
            this.nudOpen.Location = new System.Drawing.Point(135, 20);
            this.nudOpen.Maximum = new decimal(new int[] {
            999999,
            0,
            0,
            0});
            this.nudOpen.Name = "nudOpen";
            this.nudOpen.ReadOnly = true;
            this.nudOpen.Size = new System.Drawing.Size(90, 20);
            this.nudOpen.TabIndex = 86;
            this.nudOpen.ValueChanged += new System.EventHandler(this.nudOpen_ValueChanged);
            // 
            // MarketTimer
            // 
            this.MarketTimer.Interval = 1000;
            this.MarketTimer.Tick += new System.EventHandler(this.MarketTimer_Tick);
            // 
            // ServerTimer
            // 
            this.ServerTimer.Interval = 3000;
            this.ServerTimer.Tick += new System.EventHandler(this.ServerTimer_Tick);
            // 
            // groupBox3
            // 
            this.groupBox3.BackColor = System.Drawing.SystemColors.Control;
            this.groupBox3.Controls.Add(this.lblOrder);
            this.groupBox3.Dock = System.Windows.Forms.DockStyle.Top;
            this.groupBox3.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.groupBox3.Location = new System.Drawing.Point(0, 0);
            this.groupBox3.Name = "groupBox3";
            this.groupBox3.Size = new System.Drawing.Size(312, 41);
            this.groupBox3.TabIndex = 6;
            this.groupBox3.TabStop = false;
            this.groupBox3.Text = "Active Order - ";
            // 
            // lblOrder
            // 
            this.lblOrder.BackColor = System.Drawing.SystemColors.Control;
            this.lblOrder.Dock = System.Windows.Forms.DockStyle.Fill;
            this.lblOrder.Font = new System.Drawing.Font("Microsoft Sans Serif", 15F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lblOrder.Location = new System.Drawing.Point(3, 16);
            this.lblOrder.Name = "lblOrder";
            this.lblOrder.Size = new System.Drawing.Size(306, 22);
            this.lblOrder.TabIndex = 0;
            this.lblOrder.Text = "B/S 000 Stop:000000 Limit: 000000";
            this.lblOrder.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // hungTimer
            // 
            this.hungTimer.Tick += new System.EventHandler(this.hungTimer_Tick);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(312, 343);
            this.Controls.Add(this.groupBox2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.listBox1);
            this.Controls.Add(this.groupBox3);
            this.Name = "Form1";
            this.Text = "Automated Execution Platform";
            this.Load += new System.EventHandler(this.Form1_Load);
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.groupBox1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudOffSet)).EndInit();
            this.groupBox2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.nudOpen)).EndInit();
            this.groupBox3.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListBox listBox1;
        private System.Windows.Forms.Timer AlertTimer;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.Button button5;
        private System.Windows.Forms.Button btnON;
        private System.Windows.Forms.Button btnOff;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.NumericUpDown nudOffSet;
        private System.Windows.Forms.GroupBox groupBox2;
        private System.Windows.Forms.Button button4;
        private System.Windows.Forms.CheckBox chkBxOpen;
        private System.Windows.Forms.Timer MarketTimer;
        private System.Windows.Forms.Timer ServerTimer;
        private System.Windows.Forms.GroupBox groupBox3;
        private System.Windows.Forms.Label lblOrder;
        internal System.Windows.Forms.NumericUpDown nudOpen;
        private System.Windows.Forms.Timer hungTimer;
    }
}

