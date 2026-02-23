namespace Laba1TIMPWinForms
{
    partial class Form2
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
            TreeNode treeNode1 = new TreeNode("Деталь 1");
            TreeNode treeNode2 = new TreeNode("Узел1", new TreeNode[] { treeNode1 });
            TreeNode treeNode3 = new TreeNode("Узел2");
            TreeNode treeNode4 = new TreeNode("Изделие 1", new TreeNode[] { treeNode2, treeNode3 });
            btnSearch = new Button();
            treeView1 = new TreeView();
            txtComponentName = new TextBox();
            panel1 = new Panel();
            panel1.SuspendLayout();
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
            treeNode1.Name = "";
            treeNode1.Text = "Деталь 1";
            treeNode2.Name = "Узел1";
            treeNode2.Text = "Узел1";
            treeNode3.Name = "Узел2";
            treeNode3.Text = "Узел2";
            treeNode4.Name = "Узел0";
            treeNode4.Text = "Изделие 1";
            treeView1.Nodes.AddRange(new TreeNode[] { treeNode4 });
            treeView1.Size = new Size(746, 385);
            treeView1.TabIndex = 3;
            // 
            // txtComponentName
            // 
            txtComponentName.Location = new Point(26, 19);
            txtComponentName.Name = "txtComponentName";
            txtComponentName.Size = new Size(262, 27);
            txtComponentName.TabIndex = 4;
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
            // Form2
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(746, 450);
            Controls.Add(treeView1);
            Controls.Add(panel1);
            Name = "Form2";
            Text = "Спецификация";
            panel1.ResumeLayout(false);
            panel1.PerformLayout();
            ResumeLayout(false);
        }

        #endregion
        private Button btnSearch;
        private TreeView treeView1;
        private TextBox txtComponentName;
        private Panel panel1;
    }
}