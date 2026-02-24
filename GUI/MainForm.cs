using System;
using System.Windows.Forms;
using GUI;
using laba1New;

namespace Laba1TIMPWinForms
{
    public partial class MainForm : Form
    {
        private DataManager _dataManager;
        private string _currentFilePath;

        public MainForm()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "PRD files (*.prd)|*.prd|All files (*.*)|*.*";
                openFileDialog.FilterIndex = 1;
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        if (_dataManager != null)
                        {
                            _dataManager.Dispose();
                            _dataManager = null;
                        }

                        _dataManager = new DataManager();
                        _dataManager.Open(openFileDialog.FileName);
                        _currentFilePath = openFileDialog.FileName;

                        this.Text = $"Многосвязные списки - {System.IO.Path.GetFileName(_currentFilePath)}";

                        button2.Enabled = true;
                        button3.Enabled = true;

                        MessageBox.Show("Файл успешно открыт.", "Информация", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка при открытии файла: {ex.Message}", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        button2.Enabled = false;
                        button3.Enabled = false;
                        _dataManager?.Dispose();
                        _dataManager = null;
                    }
                }
            }
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (_dataManager == null)
            {
                MessageBox.Show("Сначала откройте файл.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new ComponentListForm(_dataManager, allowSelection: false))
            {
                form.ShowDialog();
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (_dataManager == null)
            {
                MessageBox.Show("Сначала откройте файл.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new FormSpec(_dataManager))
            {
                form.ShowDialog();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _dataManager?.Dispose();
            base.OnFormClosed(e);
        }
    }
}