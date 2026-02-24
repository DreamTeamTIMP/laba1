namespace GUI
{
    partial class ComponentListForm
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
            btnAdd = new Button();
            btnEdit = new Button();
            btnCancel = new Button();
            btnSave = new Button();
            btnDelete = new Button();
            dgvComponents = new DataGridView();
            colName = new DataGridViewTextBoxColumn();
            colType = new DataGridViewTextBoxColumn();
            lblName = new Label();
            txtName = new TextBox();
            lblType = new Label();
            cmbType = new ComboBox();
            btnSelect = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvComponents).BeginInit();
            SuspendLayout();
            // 
            // btnAdd
            // 
            btnAdd.Location = new Point(13, 18);
            btnAdd.Margin = new Padding(4, 5, 4, 5);
            btnAdd.Name = "btnAdd";
            btnAdd.Size = new Size(100, 35);
            btnAdd.TabIndex = 0;
            btnAdd.Text = "Добавить";
            btnAdd.UseVisualStyleBackColor = true;
            btnAdd.Click += btnAdd_Click;
            // 
            // btnEdit
            // 
            btnEdit.Location = new Point(124, 18);
            btnEdit.Margin = new Padding(4, 5, 4, 5);
            btnEdit.Name = "btnEdit";
            btnEdit.Size = new Size(100, 35);
            btnEdit.TabIndex = 1;
            btnEdit.Text = "Изменить";
            btnEdit.UseVisualStyleBackColor = true;
            btnEdit.Click += btnEdit_Click;
            // 
            // btnCancel
            // 
            btnCancel.Location = new Point(232, 18);
            btnCancel.Margin = new Padding(4, 5, 4, 5);
            btnCancel.Name = "btnCancel";
            btnCancel.Size = new Size(100, 35);
            btnCancel.TabIndex = 2;
            btnCancel.Text = "Отменить";
            btnCancel.UseVisualStyleBackColor = true;
            btnCancel.Click += btnCancel_Click;
            // 
            // btnSave
            // 
            btnSave.Location = new Point(340, 18);
            btnSave.Margin = new Padding(4, 5, 4, 5);
            btnSave.Name = "btnSave";
            btnSave.Size = new Size(100, 35);
            btnSave.TabIndex = 3;
            btnSave.Text = "Сохранить";
            btnSave.UseVisualStyleBackColor = true;
            btnSave.Click += btnSave_Click;
            // 
            // btnDelete
            // 
            btnDelete.Location = new Point(448, 18);
            btnDelete.Margin = new Padding(4, 5, 4, 5);
            btnDelete.Name = "btnDelete";
            btnDelete.Size = new Size(100, 35);
            btnDelete.TabIndex = 4;
            btnDelete.Text = "Удалить";
            btnDelete.UseVisualStyleBackColor = true;
            btnDelete.Click += btnDelete_Click;
            // 
            // dgvComponents
            // 
            dgvComponents.AllowUserToAddRows = false;
            dgvComponents.AllowUserToDeleteRows = false;
            dgvComponents.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvComponents.Columns.AddRange(new DataGridViewColumn[] { colName, colType });
            dgvComponents.Location = new Point(16, 77);
            dgvComponents.Margin = new Padding(4, 5, 4, 5);
            dgvComponents.Name = "dgvComponents";
            dgvComponents.ReadOnly = true;
            dgvComponents.RowHeadersVisible = false;
            dgvComponents.RowHeadersWidth = 51;
            dgvComponents.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvComponents.Size = new Size(747, 308);
            dgvComponents.TabIndex = 5;
            dgvComponents.CellDoubleClick += dgvComponents_CellDoubleClick;
            // 
            // colName
            // 
            colName.HeaderText = "Наименование";
            colName.MinimumWidth = 6;
            colName.Name = "colName";
            colName.ReadOnly = true;
            colName.Width = 300;
            // 
            // colType
            // 
            colType.HeaderText = "Тип";
            colType.MinimumWidth = 6;
            colType.Name = "colType";
            colType.ReadOnly = true;
            colType.Width = 150;
            // 
            // lblName
            // 
            lblName.AutoSize = true;
            lblName.Location = new Point(16, 415);
            lblName.Margin = new Padding(4, 0, 4, 0);
            lblName.Name = "lblName";
            lblName.Size = new Size(119, 20);
            lblName.TabIndex = 6;
            lblName.Text = "Наименование:";
            // 
            // txtName
            // 
            txtName.Location = new Point(131, 411);
            txtName.Margin = new Padding(4, 5, 4, 5);
            txtName.Name = "txtName";
            txtName.Size = new Size(199, 27);
            txtName.TabIndex = 7;
            // 
            // lblType
            // 
            lblType.AutoSize = true;
            lblType.Location = new Point(347, 415);
            lblType.Margin = new Padding(4, 0, 4, 0);
            lblType.Name = "lblType";
            lblType.Size = new Size(38, 20);
            lblType.TabIndex = 8;
            lblType.Text = "Тип:";
            // 
            // cmbType
            // 
            cmbType.DropDownStyle = ComboBoxStyle.DropDownList;
            cmbType.Items.AddRange(new object[] { "Изделие", "Узел", "Деталь" });
            cmbType.Location = new Point(395, 411);
            cmbType.Margin = new Padding(4, 5, 4, 5);
            cmbType.Name = "cmbType";
            cmbType.Size = new Size(159, 28);
            cmbType.TabIndex = 9;
            // 
            // btnSelect
            // 
            btnSelect.Location = new Point(555, 21);
            btnSelect.Name = "btnSelect";
            btnSelect.Size = new Size(94, 29);
            btnSelect.TabIndex = 10;
            btnSelect.Text = "Выбрать";
            btnSelect.UseVisualStyleBackColor = true;
            btnSelect.Visible = false;
            btnSelect.Click += btnSelect_Click;
            // 
            // ComponentListForm
            // 
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(780, 462);
            Controls.Add(btnSelect);
            Controls.Add(cmbType);
            Controls.Add(lblType);
            Controls.Add(txtName);
            Controls.Add(lblName);
            Controls.Add(dgvComponents);
            Controls.Add(btnDelete);
            Controls.Add(btnSave);
            Controls.Add(btnCancel);
            Controls.Add(btnEdit);
            Controls.Add(btnAdd);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            Margin = new Padding(4, 5, 4, 5);
            MaximizeBox = false;
            Name = "ComponentListForm";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Список компонентов";
            ((System.ComponentModel.ISupportInitialize)dgvComponents).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        private Button btnAdd;
        private Button btnEdit;
        private Button btnCancel;
        private Button btnSave;
        private Button btnDelete;
        private DataGridView dgvComponents;
        private DataGridViewTextBoxColumn colName;
        private DataGridViewTextBoxColumn colType;
        private Label lblName;
        private TextBox txtName;
        private Label lblType;
        private ComboBox cmbType;
        private Button btnSelect;
    }
}