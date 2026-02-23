using System;
using System.Collections.Generic;
using System.Windows.Forms;
using GUI;
using laba1New.Helpers;

namespace Laba1TIMPWinForms
{
    public partial class Form2 : Form
    {
        private DataManager _dataManager;
        private int _currentProdOffset = -1;
        private string _currentProdName = "";

        // Для контекстного меню
        private TreeNode _rightClickedNode;

        public Form2(DataManager dataManager)
        {
            InitializeComponent();
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
            ConfigureControls();
        }

        private void ConfigureControls()
        {
            // Настройка элементов
            txtComponentName.Text = "";
            btnSearch.Text = "Найти";
            treeView1.Nodes.Clear();

            // Настройка контекстного меню
            var contextMenu = new ContextMenuStrip();
            var addItem = new ToolStripMenuItem("Добавить");
            var editItem = new ToolStripMenuItem("Изменить");
            var deleteItem = new ToolStripMenuItem("Удалить");
            addItem.Click += contextMenuAdd_Click;
            editItem.Click += contextMenuEdit_Click;
            deleteItem.Click += contextMenuDelete_Click;
            contextMenu.Items.AddRange(new ToolStripItem[] { addItem, editItem, deleteItem });
            treeView1.ContextMenuStrip = contextMenu;

            // Подписка на событие клика правой кнопкой для запоминания узла
            treeView1.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Right)
                {
                    _rightClickedNode = treeView1.GetNodeAt(e.Location);
                }
            };
        }

        // Поиск компонента по имени и загрузка его спецификации
        private void btnSearch_Click(object sender, EventArgs e)
        {
            string name = txtComponentName.Text.Trim();
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show("Введите наименование компонента.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var node = _dataManager.FindNode(name);
                if (node == null)
                {
                    MessageBox.Show("Компонент не найден.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (node.Type == ComponentTypes.Detail)
                {
                    MessageBox.Show("Деталь не имеет спецификации.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }

                _currentProdOffset = node.Offset;
                _currentProdName = node.Name;
                this.Text = $"Спецификация: {_currentProdName}";
                LoadSpecificationTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        // Асинхронная загрузка дерева
        private async void LoadSpecificationTree()
        {
            try
            {
                treeView1.Nodes.Clear();

                var rootNode = await Task.Run(() => BuildSpecificationTree(_currentProdOffset));

                if (rootNode == null)
                {
                    MessageBox.Show("Не удалось загрузить спецификацию.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                TreeNode treeRoot = new TreeNode(rootNode.Text);
                treeRoot.Tag = rootNode;
                AddChildNodes(treeRoot, rootNode.Children);
                treeView1.Nodes.Add(treeRoot);
                treeView1.ExpandAll();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки спецификации: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
            }
        }

        // Рекурсивное построение дерева данных
        private SpecTreeNode BuildSpecificationTree(int prodOffset)
        {
            var prodNode = new ProdNodeHelper(_dataManager.GetProdStream(), prodOffset, _dataManager.NameSize);
            if (prodNode.CanBeDel != 0) return null;

            string typeStr = prodNode.Type switch
            {
                ComponentTypes.Product => "Изделие",
                ComponentTypes.Node => "Узел",
                ComponentTypes.Detail => "Деталь",
                _ => "Неизвестно"
            };
            var root = new SpecTreeNode
            {
                ProdOffset = prodOffset,
                Name = prodNode.Name,
                Type = prodNode.Type,
                Mentions = 1,
                Text = $"{prodNode.Name} ({typeStr})"
            };

            if (prodNode.SpecNodePtr != -1)
            {
                int currSpec = prodNode.SpecNodePtr;
                while (currSpec != -1)
                {
                    var spec = new SpecNodeHelper(_dataManager.GetSpecStream(), currSpec);
                    if (spec.CanBeDel == 0)
                    {
                        int childProdOffset = spec.ProdNodePtr;
                        var childProd = new ProdNodeHelper(_dataManager.GetProdStream(), childProdOffset, _dataManager.NameSize);
                        if (childProd.CanBeDel == 0)
                        {
                            var childNode = BuildSpecificationTree(childProdOffset);
                            if (childNode != null)
                            {
                                childNode.Mentions = spec.Mentions;
                                childNode.SpecOffset = currSpec; // сохраняем смещение записи спецификации для редактирования/удаления
                                childNode.Text = $"{childProd.Name} (x{spec.Mentions})";
                                root.Children.Add(childNode);
                            }
                        }
                    }
                    currSpec = spec.NextNodePtr;
                }
            }
            return root;
        }

        private void AddChildNodes(TreeNode parentNode, List<SpecTreeNode> children)
        {
            foreach (var child in children)
            {
                TreeNode childNode = new TreeNode(child.Text);
                childNode.Tag = child;
                parentNode.Nodes.Add(childNode);
                AddChildNodes(childNode, child.Children);
            }
        }

        // --- Обработчики контекстного меню ---
        private void contextMenuAdd_Click(object sender, EventArgs e)
        {
            if (_rightClickedNode == null) return;

            var nodeData = _rightClickedNode.Tag as SpecTreeNode;
            if (nodeData == null) return;

            // Нельзя добавлять к детали
            if (nodeData.Type == ComponentTypes.Detail)
            {
                MessageBox.Show("Деталь не может иметь спецификацию.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Открываем форму выбора компонента из списка (только изделия и узлы? можно и детали, но добавлять деталь допустимо)
            // В реальности можно добавить любой существующий компонент
            using (var form = new ComponentListForm(_dataManager, allowSelection: true))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        // Добавляем связь: родитель = nodeData.ProdOffset, ребёнок = form.SelectedComponentOffset
                        _dataManager.AddRelation(nodeData.ProdOffset, form.SelectedComponentOffset, 1);
                        LoadSpecificationTree(); // перезагружаем
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void contextMenuEdit_Click(object sender, EventArgs e)
        {
            if (_rightClickedNode == null) return;
            var nodeData = _rightClickedNode.Tag as SpecTreeNode;
            if (nodeData == null || nodeData.SpecOffset == 0) return; // для корня нет записи спецификации

            // Запрашиваем новую кратность
            string input = Microsoft.VisualBasic.Interaction.InputBox("Введите новую кратность:", "Изменение количества", nodeData.Mentions.ToString());
            if (ushort.TryParse(input, out ushort newMentions) && newMentions > 0)
            {
                try
                {
                    // Обновляем запись в файле спецификации
                    var spec = new SpecNodeHelper(_dataManager.GetSpecStream(), nodeData.SpecOffset);
                    spec.Mentions = newMentions;
                    LoadSpecificationTree(); // перезагружаем
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при изменении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show("Некорректное значение кратности.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void contextMenuDelete_Click(object sender, EventArgs e)
        {
            if (_rightClickedNode == null) return;
            var nodeData = _rightClickedNode.Tag as SpecTreeNode;
            if (nodeData == null || nodeData.SpecOffset == 0) return;

            var confirm = MessageBox.Show($"Удалить связь с '{nodeData.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (confirm == DialogResult.Yes)
            {
                try
                {
                    // Логическое удаление записи спецификации
                    var spec = new SpecNodeHelper(_dataManager.GetSpecStream(), nodeData.SpecOffset);
                    spec.CanBeDel = -1;
                    LoadSpecificationTree(); // перезагружаем
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private string TypeToString(byte type)
        {
            return type switch
            {
                ComponentTypes.Product => "Изделие",
                ComponentTypes.Node => "Узел",
                ComponentTypes.Detail => "Деталь",
                _ => "Неизвестно"
            };
        }
    }

    // Класс для хранения данных узла
    public class SpecTreeNode
    {
        public int ProdOffset { get; set; }        // смещение компонента в .prd
        public int SpecOffset { get; set; }        // смещение записи в .prs (0 для корня)
        public string Name { get; set; }
        public byte Type { get; set; }
        public ushort Mentions { get; set; }
        public string Text { get; set; }
        public List<SpecTreeNode> Children { get; set; } = new List<SpecTreeNode>();
    }
}