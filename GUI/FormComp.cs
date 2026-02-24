using System;
using System.Collections.Generic;
using System.Windows.Forms;
using laba1New.Helpers;

namespace GUI
{
    public partial class ComponentListForm : Form
    {
        private DataManager _dataManager;
        private bool _isAdding = false;
        private bool _isEditing = false;
        private int _editingOffset = -1;
        private bool _allowSelection;
        public int SelectedComponentOffset { get; private set; }
        public string SelectedComponentName { get; private set; }
        public byte SelectedComponentType { get; private set; }

        public ComponentListForm(DataManager dataManager, bool allowSelection = false) : this(dataManager)
        {
            _allowSelection = allowSelection;
            if (_allowSelection)
            {
                btnSelect.Visible = true;
                btnAdd.Visible = false;
                btnEdit.Visible = false;
                btnDelete.Visible = false;
                btnSave.Visible = false;
                btnCancel.Visible = false;
            }
        }

        public ComponentListForm(DataManager dataManager)
        {
            InitializeComponent();
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            LoadComponents();
            SetViewMode();
        }

        private void LoadComponents()
        {
            dgvComponents.Rows.Clear();
            var components = _dataManager.GetActiveComponents();
            foreach (var comp in components)
            {
                string typeStr = comp.Type switch
                {
                    ComponentTypes.Product => "Изделие",
                    ComponentTypes.Node => "Узел",
                    ComponentTypes.Detail => "Деталь",
                    _ => "Неизвестно"
                };
                int rowIndex = dgvComponents.Rows.Add(comp.Name, typeStr);
                dgvComponents.Rows[rowIndex].Tag = comp.Offset;
            }
        }

        private void SetViewMode()
        {
            _isAdding = false;
            _isEditing = false;
            _editingOffset = -1;

            btnAdd.Enabled = true;
            btnEdit.Enabled = true;
            btnDelete.Enabled = true;
            btnSave.Enabled = false;
            btnCancel.Enabled = false;

            txtName.Clear();
            cmbType.SelectedIndex = -1;
            dgvComponents.Enabled = true;
        }

        private void SetEditMode(bool isAdding, int? offset = null)
        {
            _isAdding = isAdding;
            _isEditing = !isAdding && offset.HasValue;
            _editingOffset = offset ?? -1;

            btnAdd.Enabled = false;
            btnEdit.Enabled = false;
            btnDelete.Enabled = false;
            btnSave.Enabled = true;
            btnCancel.Enabled = true;

            dgvComponents.Enabled = false;

            if (_isEditing && offset.HasValue)
            {
                var row = GetSelectedRow();
                if (row != null)
                {
                    txtName.Text = row.Cells["colName"].Value.ToString();
                    string typeStr = row.Cells["colType"].Value.ToString();
                    cmbType.SelectedItem = typeStr;
                }
            }
            else if (isAdding)
            {
                txtName.Clear();
                cmbType.SelectedIndex = -1;
            }
        }

        private DataGridViewRow GetSelectedRow()
        {
            if (dgvComponents.SelectedRows.Count > 0)
                return dgvComponents.SelectedRows[0];
            return null;
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            SetEditMode(true);
        }

        private void btnEdit_Click(object sender, EventArgs e)
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите компонент для редактирования.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            int offset = (int)row.Tag;
            SetEditMode(false, offset);
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите компонент для удаления.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string name = row.Cells["colName"].Value.ToString();
            var result = MessageBox.Show($"Удалить компонент '{name}'? (Логическое удаление)", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                try
                {
                    int offset = (int)row.Tag;
                    _dataManager.DeleteComponent(offset);
                    LoadComponents();
                    SetViewMode();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            string name = txtName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите наименование компонента.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (cmbType.SelectedItem == null)
            {
                MessageBox.Show("Выберите тип компонента.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string typeStr = cmbType.SelectedItem.ToString();
            byte type = typeStr switch
            {
                "Изделие" => ComponentTypes.Product,
                "Узел" => ComponentTypes.Node,
                "Деталь" => ComponentTypes.Detail,
                _ => throw new InvalidOperationException("Неизвестный тип")
            };

            try
            {
                if (_isAdding)
                {
                    _dataManager.AddComponent(name, typeStr);
                }
                else if (_isEditing)
                {
                    _dataManager.UpdateComponent(_editingOffset, name, type);
                }

                LoadComponents();
                SetViewMode();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при сохранении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            SetViewMode();
        }

        private void SelectCurrentRow()
        {
            var row = GetSelectedRow();
            if (row == null)
            {
                MessageBox.Show("Выберите компонент из списка.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            SelectedComponentOffset = (int)row.Tag;
            SelectedComponentName = row.Cells["colName"].Value.ToString();
            string typeStr = row.Cells["colType"].Value.ToString();
            SelectedComponentType = typeStr switch
            {
                "Изделие" => ComponentTypes.Product,
                "Узел" => ComponentTypes.Node,
                "Деталь" => ComponentTypes.Detail,
                _ => throw new InvalidOperationException()
            };
            DialogResult = DialogResult.OK;
            Close();
        }

        private void btnSelect_Click(object sender, EventArgs e)
        {
            SelectCurrentRow();
        }

        private void dgvComponents_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if (_allowSelection && e.RowIndex >= 0)
            {
                SelectCurrentRow();
            }
        }
    }
}