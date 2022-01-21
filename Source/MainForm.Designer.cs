namespace eft_dma_radar
{
    partial class MainForm
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
            this.tabControl1 = new System.Windows.Forms.TabControl();
            this.tabPage1 = new System.Windows.Forms.TabPage();
            this.label_Pos = new System.Windows.Forms.Label();
            this.mapCanvas = new System.Windows.Forms.PictureBox();
            this.tabPage2 = new System.Windows.Forms.TabPage();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.checkBox1 = new System.Windows.Forms.CheckBox();
            this.checkBox_Loot = new System.Windows.Forms.CheckBox();
            this.label1 = new System.Windows.Forms.Label();
            this.trackBar_Zoom = new System.Windows.Forms.TrackBar();
            this.label3 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.trackBar_EnemyAim = new System.Windows.Forms.TrackBar();
            this.trackBar_AimLength = new System.Windows.Forms.TrackBar();
            this.label_Map = new System.Windows.Forms.Label();
            this.button_Map = new System.Windows.Forms.Button();
            this.timer1 = new System.Windows.Forms.Timer(this.components);
            this.checkBox2 = new System.Windows.Forms.CheckBox();
            this.timer2 = new System.Windows.Forms.Timer(this.components);
            this.checkBox3 = new System.Windows.Forms.CheckBox();
            this.timer3 = new System.Windows.Forms.Timer(this.components);
            this.tabControl1.SuspendLayout();
            this.tabPage1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mapCanvas)).BeginInit();
            this.tabPage2.SuspendLayout();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_Zoom)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_EnemyAim)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_AimLength)).BeginInit();
            this.SuspendLayout();
            // 
            // tabControl1
            // 
            this.tabControl1.Controls.Add(this.tabPage1);
            this.tabControl1.Controls.Add(this.tabPage2);
            this.tabControl1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl1.Location = new System.Drawing.Point(0, 0);
            this.tabControl1.Name = "tabControl1";
            this.tabControl1.SelectedIndex = 0;
            this.tabControl1.Size = new System.Drawing.Size(1336, 1061);
            this.tabControl1.TabIndex = 8;
            // 
            // tabPage1
            // 
            this.tabPage1.Controls.Add(this.label_Pos);
            this.tabPage1.Controls.Add(this.mapCanvas);
            this.tabPage1.Location = new System.Drawing.Point(4, 24);
            this.tabPage1.Name = "tabPage1";
            this.tabPage1.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage1.Size = new System.Drawing.Size(1328, 1033);
            this.tabPage1.TabIndex = 0;
            this.tabPage1.Text = "Radar";
            this.tabPage1.UseVisualStyleBackColor = true;
            // 
            // label_Pos
            // 
            this.label_Pos.AutoSize = true;
            this.label_Pos.Location = new System.Drawing.Point(9, 3);
            this.label_Pos.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_Pos.Name = "label_Pos";
            this.label_Pos.Size = new System.Drawing.Size(0, 15);
            this.label_Pos.TabIndex = 10;
            // 
            // mapCanvas
            // 
            this.mapCanvas.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapCanvas.ImageLocation = "";
            this.mapCanvas.Location = new System.Drawing.Point(3, 3);
            this.mapCanvas.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.mapCanvas.Name = "mapCanvas";
            this.mapCanvas.Size = new System.Drawing.Size(1322, 1027);
            this.mapCanvas.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.mapCanvas.TabIndex = 2;
            this.mapCanvas.TabStop = false;
            // 
            // tabPage2
            // 
            this.tabPage2.Controls.Add(this.groupBox1);
            this.tabPage2.Location = new System.Drawing.Point(4, 24);
            this.tabPage2.Name = "tabPage2";
            this.tabPage2.Padding = new System.Windows.Forms.Padding(3);
            this.tabPage2.Size = new System.Drawing.Size(1328, 1033);
            this.tabPage2.TabIndex = 1;
            this.tabPage2.Text = "Settings";
            this.tabPage2.UseVisualStyleBackColor = true;
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.checkBox3);
            this.groupBox1.Controls.Add(this.checkBox2);
            this.groupBox1.Controls.Add(this.checkBox1);
            this.groupBox1.Controls.Add(this.checkBox_Loot);
            this.groupBox1.Controls.Add(this.label1);
            this.groupBox1.Controls.Add(this.trackBar_Zoom);
            this.groupBox1.Controls.Add(this.label3);
            this.groupBox1.Controls.Add(this.label2);
            this.groupBox1.Controls.Add(this.trackBar_EnemyAim);
            this.groupBox1.Controls.Add(this.trackBar_AimLength);
            this.groupBox1.Controls.Add(this.label_Map);
            this.groupBox1.Controls.Add(this.button_Map);
            this.groupBox1.Dock = System.Windows.Forms.DockStyle.Left;
            this.groupBox1.Location = new System.Drawing.Point(3, 3);
            this.groupBox1.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Padding = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.groupBox1.Size = new System.Drawing.Size(320, 1027);
            this.groupBox1.TabIndex = 8;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = "Radar Config";
            // 
            // checkBox1
            // 
            this.checkBox1.AutoSize = true;
            this.checkBox1.Location = new System.Drawing.Point(177, 107);
            this.checkBox1.Name = "checkBox1";
            this.checkBox1.Size = new System.Drawing.Size(87, 19);
            this.checkBox1.TabIndex = 18;
            this.checkBox1.Text = "No stamina";
            this.checkBox1.UseVisualStyleBackColor = true;
            this.checkBox1.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
            // 
            // checkBox_Loot
            // 
            this.checkBox_Loot.AutoSize = true;
            this.checkBox_Loot.Location = new System.Drawing.Point(38, 107);
            this.checkBox_Loot.Name = "checkBox_Loot";
            this.checkBox_Loot.Size = new System.Drawing.Size(105, 19);
            this.checkBox_Loot.TabIndex = 17;
            this.checkBox_Loot.Text = "Show Loot (F3)";
            this.checkBox_Loot.UseVisualStyleBackColor = true;
            this.checkBox_Loot.CheckedChanged += new System.EventHandler(this.checkBox_Loot_CheckedChanged);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(201, 166);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(111, 15);
            this.label1.TabIndex = 16;
            this.label1.Text = "Zoom (F1 in F2 out)";
            // 
            // trackBar_Zoom
            // 
            this.trackBar_Zoom.LargeChange = 1;
            this.trackBar_Zoom.Location = new System.Drawing.Point(237, 185);
            this.trackBar_Zoom.Maximum = 100;
            this.trackBar_Zoom.Minimum = 1;
            this.trackBar_Zoom.Name = "trackBar_Zoom";
            this.trackBar_Zoom.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_Zoom.Size = new System.Drawing.Size(45, 403);
            this.trackBar_Zoom.TabIndex = 15;
            this.trackBar_Zoom.Value = 100;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(10, 166);
            this.label3.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(87, 15);
            this.label3.TabIndex = 14;
            this.label3.Text = "Enemy Aimline";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(104, 166);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(83, 15);
            this.label2.TabIndex = 13;
            this.label2.Text = "Player Aimline";
            // 
            // trackBar_EnemyAim
            // 
            this.trackBar_EnemyAim.LargeChange = 50;
            this.trackBar_EnemyAim.Location = new System.Drawing.Point(44, 185);
            this.trackBar_EnemyAim.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.trackBar_EnemyAim.Maximum = 1000;
            this.trackBar_EnemyAim.Minimum = 100;
            this.trackBar_EnemyAim.Name = "trackBar_EnemyAim";
            this.trackBar_EnemyAim.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_EnemyAim.Size = new System.Drawing.Size(45, 403);
            this.trackBar_EnemyAim.SmallChange = 5;
            this.trackBar_EnemyAim.TabIndex = 12;
            this.trackBar_EnemyAim.Value = 150;
            // 
            // trackBar_AimLength
            // 
            this.trackBar_AimLength.LargeChange = 50;
            this.trackBar_AimLength.Location = new System.Drawing.Point(119, 185);
            this.trackBar_AimLength.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.trackBar_AimLength.Maximum = 1000;
            this.trackBar_AimLength.Minimum = 100;
            this.trackBar_AimLength.Name = "trackBar_AimLength";
            this.trackBar_AimLength.Orientation = System.Windows.Forms.Orientation.Vertical;
            this.trackBar_AimLength.Size = new System.Drawing.Size(45, 403);
            this.trackBar_AimLength.SmallChange = 5;
            this.trackBar_AimLength.TabIndex = 11;
            this.trackBar_AimLength.Value = 500;
            // 
            // label_Map
            // 
            this.label_Map.AutoSize = true;
            this.label_Map.Location = new System.Drawing.Point(54, 63);
            this.label_Map.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label_Map.Name = "label_Map";
            this.label_Map.Size = new System.Drawing.Size(79, 15);
            this.label_Map.TabIndex = 8;
            this.label_Map.Text = "DEFAULTMAP";
            // 
            // button_Map
            // 
            this.button_Map.Location = new System.Drawing.Point(44, 33);
            this.button_Map.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.button_Map.Name = "button_Map";
            this.button_Map.Size = new System.Drawing.Size(107, 27);
            this.button_Map.TabIndex = 7;
            this.button_Map.Text = "Toggle Map (F4)";
            this.button_Map.UseVisualStyleBackColor = true;
            this.button_Map.Click += new System.EventHandler(this.button_Map_Click);
            // 
            // timer1
            // 
            this.timer1.Tick += new System.EventHandler(this.timer1_Tick);
            // 
            // checkBox2
            // 
            this.checkBox2.AutoSize = true;
            this.checkBox2.Location = new System.Drawing.Point(177, 82);
            this.checkBox2.Name = "checkBox2";
            this.checkBox2.Size = new System.Drawing.Size(53, 19);
            this.checkBox2.TabIndex = 19;
            this.checkBox2.Text = "Buffs";
            this.checkBox2.UseVisualStyleBackColor = true;
            this.checkBox2.CheckedChanged += new System.EventHandler(this.checkBox2_CheckedChanged);
            // 
            // timer2
            // 
            this.timer2.Tick += new System.EventHandler(this.timer2_Tick);
            // 
            // checkBox3
            // 
            this.checkBox3.AutoSize = true;
            this.checkBox3.Location = new System.Drawing.Point(177, 57);
            this.checkBox3.Name = "checkBox3";
            this.checkBox3.Size = new System.Drawing.Size(53, 19);
            this.checkBox3.TabIndex = 20;
            this.checkBox3.Text = "Glide";
            this.checkBox3.UseVisualStyleBackColor = true;
            this.checkBox3.CheckedChanged += new System.EventHandler(this.checkBox3_CheckedChanged);
            // 
            // timer3
            // 
            this.timer3.Tick += new System.EventHandler(this.timer3_Tick);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1336, 1061);
            this.Controls.Add(this.tabControl1);
            this.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
            this.Name = "MainForm";
            this.Text = "EFT Radar";
            this.tabControl1.ResumeLayout(false);
            this.tabPage1.ResumeLayout(false);
            this.tabPage1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.mapCanvas)).EndInit();
            this.tabPage2.ResumeLayout(false);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_Zoom)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_EnemyAim)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.trackBar_AimLength)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private TabControl tabControl1;
        private TabPage tabPage1;
        private PictureBox mapCanvas;
        private TabPage tabPage2;
        private GroupBox groupBox1;
        private Label label3;
        private Label label2;
        private TrackBar trackBar_EnemyAim;
        private TrackBar trackBar_AimLength;
        private Label label_Map;
        private Button button_Map;
        private Label label_Pos;
        private Label label1;
        private TrackBar trackBar_Zoom;
        private CheckBox checkBox_Loot;
        private System.Windows.Forms.Timer timer1;
        private CheckBox checkBox1;
        private CheckBox checkBox2;
        private System.Windows.Forms.Timer timer2;
        private CheckBox checkBox3;
        private System.Windows.Forms.Timer timer3;
    }
}

