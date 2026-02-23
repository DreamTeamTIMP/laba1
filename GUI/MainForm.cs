using System;
using System.Windows.Forms;
using GUI;
using laba1New; // или соответствующий namespace, где лежит DataManager

namespace Laba1TIMPWinForms
{
    public partial class MainForm : Form
    {
        private DataManager _dataManager;
        private string _currentFilePath;

        public MainForm()
        {
            InitializeComponent();
            // Изначально кнопки неактивны, пока файл не открыт
            button2.Enabled = false;
            button3.Enabled = false;
        }

        // Обработчик загрузки формы (можно оставить пустым или добавить инициализацию)
        private void Form1_Load(object sender, EventArgs e)
        {
            // Можно попытаться открыть файл, переданный в аргументах командной строки, но пока не требуется
        }

        // Открыть файл
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
                        // Если предыдущий менеджер был открыт, закрываем его
                        if (_dataManager != null)
                        {
                            _dataManager.Dispose();
                            _dataManager = null;
                        }

                        _dataManager = new DataManager();
                        _dataManager.Open(openFileDialog.FileName);
                        _currentFilePath = openFileDialog.FileName;

                        // Обновляем заголовок формы
                        this.Text = $"Многосвязные списки - {System.IO.Path.GetFileName(_currentFilePath)}";

                        // Активируем кнопки
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

        // Открыть форму списка компонентов
        private void button2_Click(object sender, EventArgs e)
        {
            if (_dataManager == null)
            {
                MessageBox.Show("Сначала откройте файл.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Открываем форму списка компонентов в режиме редактирования (не выбора)
            using (var form = new ComponentListForm(_dataManager, allowSelection: false))
            {
                form.ShowDialog();
            }
        }

        // Открыть форму спецификации
        private void button3_Click(object sender, EventArgs e)
        {
            if (_dataManager == null)
            {
                MessageBox.Show("Сначала откройте файл.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var form = new Form2(_dataManager))
            {
                form.ShowDialog();
            }
        }

        // При закрытии главной формы освобождаем ресурсы DataManager
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _dataManager?.Dispose();
            base.OnFormClosed(e);
        }
    }
}