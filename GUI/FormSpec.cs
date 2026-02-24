using GUI;
using laba1.Helpers;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Laba1TIMPWinForms
{
    public partial class FormSpec : Form
    {
        private readonly DataManager _dataManager;
        private int _currentProdOffset = -1;
        private string _currentProdName = "";
        private TreeNode _rightClickedNode;

        public FormSpec(DataManager dataManager)
        {
            InitializeComponent();
            _dataManager = dataManager ?? throw new ArgumentNullException(nameof(dataManager));
        }

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
                var rootNode = _dataManager.GetSpecificationTree(name);
                if (rootNode == null)
                {
                    MessageBox.Show("Компонент не найден или не имеет спецификации.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _currentProdOffset = rootNode.ProdOffset;
                _currentProdName = rootNode.Name;
                Text = $"Спецификация: {_currentProdName}";
                DisplayTree(rootNode);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void DisplayTree(SpecTreeNode rootNode)
        {
            treeView1.Nodes.Clear();
            TreeNode treeRoot = new TreeNode(rootNode.Text);
            treeRoot.Tag = rootNode;
            AddChildNodes(treeRoot, rootNode.Children);
            treeView1.Nodes.Add(treeRoot);
            treeView1.ExpandAll();
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

        // Остальные методы (контекстное меню) используют _dataManager для операций
        private void contextMenuEdit_Click(object sender, EventArgs e)
        {
            if (_rightClickedNode == null) return;
            var nodeData = _rightClickedNode.Tag as SpecTreeNode;
            if (nodeData == null || nodeData.SpecOffset == 0) return;

            string input = Microsoft.VisualBasic.Interaction.InputBox("Введите новую кратность:", "Изменение количества", nodeData.Mentions.ToString());
            if (ushort.TryParse(input, out ushort newMentions) && newMentions > 0)
            {
                try
                {
                    _dataManager.UpdateMentions(nodeData.SpecOffset, newMentions);
                    RefreshTree();
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
                    _dataManager.DeleteRelation(nodeData.SpecOffset);
                    RefreshTree();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private void contextMenuAdd_Click(object sender, EventArgs e)
        {
            if (_rightClickedNode == null) return;
            var nodeData = _rightClickedNode.Tag as SpecTreeNode;
            if (nodeData == null) return;

            if (nodeData.Type == ComponentTypes.Detail)
            {
                MessageBox.Show("Деталь не может иметь спецификацию.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            using (var form = new ComponentListForm(_dataManager, allowSelection: true))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    string input = Microsoft.VisualBasic.Interaction.InputBox("Введите кратность:", "Добавление компонента", "1");
                    if (ushort.TryParse(input, out ushort count) && count > 0)
                    {
                        try
                        {
                            _dataManager.AddRelation(nodeData.ProdOffset, form.SelectedComponentOffset, count);
                            RefreshTree();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при добавлении: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Некорректное значение кратности.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _rightClickedNode = treeView1.GetNodeAt(e.Location);
            }
        }

        private void RefreshTree()
        {
            if (_currentProdOffset == -1) return;
            var rootNode = _dataManager.GetSpecificationTree(_currentProdName);
            if (rootNode != null)
                DisplayTree(rootNode);
        }
    }
}