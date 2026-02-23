using System;
using System.Collections.Generic;
using System.Windows.Forms;
using laba1New.Helpers; // для ComponentTypes

namespace GUI
{
    public partial class ComponentListForm : Form
    {
        private DataManager _dataManager;
        private bool _isAdding = false;
        private bool _isEditing = false;
        private int _editingOffset = -1; // смещение редактируемой записи
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

                // Добавляем обработчик для кнопки "Выбрать"
                btnSelect.Click += (s, e) => SelectCurrentRow();
                // Двойной клик по строке тоже выбирает
                dgvComponents.CellDoubleClick += (s, e) => SelectCurrentRow();
            }
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
            SelectedComponentName = row.Cells["Name"].Value.ToString();
            string typeStr = row.Cells["Type"].Value.ToString();
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

        public ComponentListForm(DataManager dataManager)
        {
            InitializeComponent();
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            ConfigureDataGridView();
            LoadComponents();
            SetViewMode();
        }

        private void ConfigureDataGridView()
        {
            // Настройка колонок, если они не заданы в дизайнере
            dgvComponents.AutoGenerateColumns = false;
            dgvComponents.Columns.Clear();

            DataGridViewTextBoxColumn colName = new DataGridViewTextBoxColumn();
            colName.Name = "Name";
            colName.HeaderText = "Наименование";
            colName.DataPropertyName = "Name"; // если будем использовать binding, но мы будем заполнять вручную
            colName.Width = 300;

            DataGridViewTextBoxColumn colType = new DataGridViewTextBoxColumn();
            colType.Name = "Type";
            colType.HeaderText = "Тип";
            colType.DataPropertyName = "Type";
            colType.Width = 150;

            dgvComponents.Columns.Add(colName);
            dgvComponents.Columns.Add(colType);
        }

        private void LoadComponents()
        {
            dgvComponents.Rows.Clear();
            var components = _dataManager.GetActiveComponents(); // нужно реализовать
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
                dgvComponents.Rows[rowIndex].Tag = comp.Offset; // сохраняем смещение
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
                // Заполнить поля из выбранной строки
                var row = GetSelectedRow();
                if (row != null)
                {
                    txtName.Text = row.Cells["Name"].Value.ToString();
                    string typeStr = row.Cells["Type"].Value.ToString();
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

            string name = row.Cells["Name"].Value.ToString();
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
                    _dataManager.AddComponent(name, typeStr); // используем существующий метод
                }
                else if (_isEditing)
                {
                    // Редактирование: нужно обновить имя и тип
                    _dataManager.UpdateComponent(_editingOffset, name, type); // нужно реализовать
                }

                // После успешного сохранения перезагружаем список и возвращаемся в режим просмотра
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

        private void dgvComponents_SelectionChanged(object sender, EventArgs e)
        {
            // Можно заполнять поля при выборе, если не в режиме редактирования/добавления
            if (!_isAdding && !_isEditing)
            {
                var row = GetSelectedRow();
                if (row != null)
                {
                    txtName.Text = row.Cells["Name"].Value.ToString();
                    cmbType.SelectedItem = row.Cells["Type"].Value.ToString();
                }
            }
        }
    }
}