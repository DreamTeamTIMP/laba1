using GUI;
using laba1New.Helpers;
using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Laba1TIMPWinForms
{
    public partial class FormSpec : Form
    {
        private DataManager _dataManager;
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
                Text = $"Спецификация: {_currentProdName}";
                LoadSpecificationTree();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

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
        }

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
                                childNode.SpecOffset = currSpec;
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

        private void treeView1_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                _rightClickedNode = treeView1.GetNodeAt(e.Location);
            }
        }

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
                    LoadSpecificationTree();
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
                    LoadSpecificationTree();
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
                            LoadSpecificationTree();
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
    }
}