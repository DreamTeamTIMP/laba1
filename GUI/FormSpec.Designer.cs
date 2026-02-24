namespace Laba1TIMPWinForms
{
    partial class FormSpec
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            components = new System.ComponentModel.Container();
            btnSearch = new Button();
            treeView1 = new TreeView();
            txtComponentName = new TextBox();
            panel1 = new Panel();
            contextMenuStrip = new ContextMenuStrip(components);
            addToolStripMenuItem = new ToolStripMenuItem("Добавить");
            editToolStripMenuItem = new ToolStripMenuItem("Изменить");
            deleteToolStripMenuItem = new ToolStripMenuItem("Удалить");
            panel1.SuspendLayout();
            contextMenuStrip.SuspendLayout();
            SuspendLayout();
            // 
            // btnSearch
            // 
            btnSearch.BackColor = SystemColors.ButtonFace;
            btnSearch.ForeColor = SystemColors.ActiveCaptionText;
            btnSearch.Location = new Point(308, 19);
            btnSearch.Name = "btnSearch";
            btnSearch.Size = new Size(94, 29);
            btnSearch.TabIndex = 2;
            btnSearch.Text = "Найти";
            btnSearch.UseVisualStyleBackColor = false;
            btnSearch.Click += btnSearch_Click;
            // 
            // treeView1
            // 
            treeView1.Dock = DockStyle.Top;
            treeView1.Location = new Point(0, 82);
            treeView1.Name = "treeView1";
            treeView1.Size = new Size(746, 385);
            treeView1.TabIndex = 3;
            treeView1.ContextMenuStrip = contextMenuStrip;
            treeView1.MouseDown += treeView1_MouseDown;
            // 
            // txtComponentName
            // 
            txtComponentName.Location = new Point(26, 19);
            txtComponentName.Name = "txtComponentName";
            txtComponentName.Size = new Size(262, 27);
            txtComponentName.TabIndex = 4;
            txtComponentName.Text = "";
            // 
            // panel1
            // 
            panel1.Controls.Add(btnSearch);
            panel1.Controls.Add(txtComponentName);
            panel1.Dock = DockStyle.Top;
            panel1.Location = new Point(0, 0);
            panel1.Name = "panel1";
            panel1.Size = new Size(746, 82);
            panel1.TabIndex = 5;
            // 
            // contextMenuStrip
            // 
            contextMenuStrip.Items.AddRange(new ToolStripItem[] {
                addToolStripMenuItem,
                editToolStripMenuItem,
                deleteToolStripMenuItem
            });
            contextMenuStrip.Name = "contextMenuStrip";
            contextMenuStrip.Size = new Size(181, 70);
            // 
            // addToolStripMenuItem
            // 
            addToolStripMenuItem.Name = "addToolStripMenuItem";
            addToolStripMenuItem.Size = new Size(180, 22);
            addToolStripMenuItem.Text = "Добавить";
            addToolStripMenuItem.Click += contextMenuAdd_Click;
            // 
            // editToolStripMenuItem
            // 
            editToolStripMenuItem.Name = "editToolStripMenuItem";
            editToolStripMenuItem.Size = new Size(180, 22);
            editToolStripMenuItem.Text = "Изменить";
            editToolStripMenuItem.Click += contextMenuEdit_Click;
            // 
            // deleteToolStripMenuItem
            // 
            deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            deleteToolStripMenuItem.Size = new Size(180, 22);
            deleteToolStripMenuItem.Text = "Удалить";
            deleteToolStripMenuItem.Click += contextMenuDelete_Click;
            // 
            // FormSpec
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(746, 450);
            Controls.Add(treeView1);
            Controls.Add(panel1);
            Name = "FormSpec";
            Text = "Спецификация";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            contextMenuStrip.ResumeLayout(false);
            ResumeLayout(false);
        }

        private Button btnSearch;
        private TreeView treeView1;
        private TextBox txtComponentName;
        private Panel panel1;
        private ContextMenuStrip contextMenuStrip;
        private ToolStripMenuItem addToolStripMenuItem;
        private ToolStripMenuItem editToolStripMenuItem;
        private ToolStripMenuItem deleteToolStripMenuItem;
    }
}