using System;
using System.IO;
using System.Linq;
using System.Text;
using laba1New;
using laba1New.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace DataManagerTests
{
    [TestClass]
    public class DataManagerTests
    {
        private string _testDir;
        private string _prodPath;
        private string _specPath;
        private DataManager _manager;

        [TestInitialize]
        public void Setup()
        {
            // Регистрируем кодировку 1251 (необходимо для работы с русскими именами)
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Создаём уникальную временную папку для тестов
            _testDir = Path.Combine(Path.GetTempPath(), "DataManagerTests_" + Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDir);
            Environment.CurrentDirectory = _testDir; // чтобы файлы создавались в этой папке

            _prodPath = Path.Combine(_testDir, "Testing.prd");
            _specPath = Path.Combine(_testDir, "Testing.prs");

            _manager = new DataManager();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _manager?.Dispose();
            try
            {
                if (Directory.Exists(_testDir))
                    Directory.Delete(_testDir, true);
            }
            catch { }
        }

        // Вспомогательный метод для получения списка активных компонентов в виде кортежей (имя, тип)
        private List<(string Name, byte Type)> GetActiveComponentsList()
        {
            return _manager.GetActiveComponents()
                .Select(c => (c.Name, c.Type))
                .OrderBy(c => c.Name)
                .ToList();
        }

        [TestMethod]
        public void Create_ShouldCreateFilesWithCorrectHeaders()
        {
            // Arrange
            ushort nameSize = 30;
            string specName = "custom.prs";

            // Act
            _manager.Create("Testing", nameSize, specName);

            // Assert
            Assert.IsTrue(File.Exists(_prodPath), "Файл .prd не создан");
            Assert.IsTrue(File.Exists(Path.Combine(_testDir, specName)), "Файл .prs не создан");

            // Проверим заголовок .prd
            using (var fs = new FileStream(_prodPath, FileMode.Open, FileAccess.Read))
            {
                byte[] sig = new byte[2];
                fs.Read(sig, 0, 2);
                Assert.AreEqual("PS", Encoding.ASCII.GetString(sig), "Неверная сигнатура");

                byte[] dsBuf = new byte[2];
                fs.Read(dsBuf, 0, 2);
                Assert.AreEqual(nameSize, BitConverter.ToUInt16(dsBuf, 0), "Неверный размер имени");

                fs.Seek(4, SeekOrigin.Current); // пропускаем FirstNode и FreeSpace
                byte[] nameBuf = new byte[16];
                fs.Read(nameBuf, 0, 16);
                string storedSpecName = Encoding.ASCII.GetString(nameBuf).TrimEnd('\0', ' ');
                Assert.AreEqual(specName, storedSpecName, "Имя файла спецификации в заголовке не совпадает");
            }

            // Проверим заголовок .prs
            using (var fs = new FileStream(Path.Combine(_testDir, specName), FileMode.Open, FileAccess.Read))
            {
                byte[] firstNode = new byte[4];
                fs.Read(firstNode, 0, 4);
                Assert.AreEqual(-1, BitConverter.ToInt32(firstNode, 0), "FirstNode должен быть -1");

                byte[] freeSpace = new byte[4];
                fs.Read(freeSpace, 0, 4);
                Assert.AreEqual(8, BitConverter.ToInt32(freeSpace, 0), "FreeSpace должен быть 8");
            }
        }

        [TestMethod]
        public void AddComponent_ShouldAddComponentAndMaintainAlphabeticalOrder()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("Бета", "Узел");
            _manager.AddComponent("Альфа", "Изделие");
            _manager.AddComponent("Гамма", "Деталь");

            // Act
            var components = GetActiveComponentsList();

            // Assert
            Assert.AreEqual(3, components.Count);
            Assert.AreEqual("Альфа", components[0].Name);
            Assert.AreEqual(ComponentTypes.Product, components[0].Type);
            Assert.AreEqual("Бета", components[1].Name);
            Assert.AreEqual(ComponentTypes.Node, components[1].Type);
            Assert.AreEqual("Гамма", components[2].Name);
            Assert.AreEqual(ComponentTypes.Detail, components[2].Type);
        }

        [TestMethod]
        public void AddComponent_DuplicateName_ShouldThrow()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("Тест", "Узел");

            // Act & Assert
            Assert.ThrowsException<Exception>(() => _manager.AddComponent("Тест", "Деталь"),
                "Должно быть исключение о дубликате имени");
        }

        [TestMethod]
        public void AddRelation_ShouldCreateRelationAndUpdateMentions()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("Изделие", "Изделие");
            _manager.AddComponent("Узел", "Узел");
            _manager.AddComponent("Деталь", "Деталь");

            // Act
            _manager.AddRelation("Изделие", "Узел");
            _manager.AddRelation("Узел", "Деталь", 3);

            // Assert: проверим дерево через Print (но проще через внутренние структуры)
            var parent = _manager.FindNode("Изделие");
            Assert.IsNotNull(parent);
            Assert.AreNotEqual(-1, parent.SpecNodePtr, "У изделия должна быть спецификация");

            // Проверим кратность через поиск связи (придётся заглянуть в спецификацию)
            // Используем рефлексию или добавим публичный метод для тестов? Лучше добавить вспомогательный метод в тестах.
            // Для простоты проверим, что при повторном добавлении той же связи кратность увеличивается.
            _manager.AddRelation("Узел", "Деталь", 2); // должно стать 3+2=5

            // Теперь прочитаем запись спецификации узла
            var nodeUzel = _manager.FindNode("Узел");
            var specHelper = new SpecNodeHelper(_manager.GetSpecStream(), nodeUzel.SpecNodePtr);
            Assert.AreEqual(5, specHelper.Mentions, "Кратность должна быть 5");
        }

        [TestMethod]
        public void AddRelation_ProductAsChild_ShouldThrow()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("ИзделиеРодитель", "Изделие");
            _manager.AddComponent("ИзделиеРебенок", "Изделие");

            // Act & Assert
            Assert.ThrowsException<Exception>(() => _manager.AddRelation("ИзделиеРодитель", "ИзделиеРебенок"),
                "Нельзя добавить изделие как комплектующее");
        }

        [TestMethod]
        public void AddRelation_DetailAsParent_ShouldThrow()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("Деталь", "Деталь");
            _manager.AddComponent("Узел", "Узел");

            // Act & Assert
            Assert.ThrowsException<Exception>(() => _manager.AddRelation("Деталь", "Узел"),
                "Деталь не может иметь спецификацию");
        }

        [TestMethod]
        public void AddRelation_CyclicReference_ShouldThrow()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("A", "Узел");
            _manager.AddComponent("B", "Узел");
            _manager.AddComponent("C", "Узел");

            _manager.AddRelation("A", "B");
            _manager.AddRelation("B", "C");

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => _manager.AddRelation("C", "A"),
                "Циклическая ссылка должна быть запрещена");
        }

        [TestMethod]
        public void DeleteComponent_LogicalDelete_ShouldHideComponent()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("Комп", "Узел");
            Assert.AreEqual(1, GetActiveComponentsList().Count);

            // Act
            _manager.DeleteComponent("Комп");
            var active = GetActiveComponentsList();

            // Assert
            Assert.AreEqual(0, active.Count, "Компонент должен исчезнуть из списка активных");
        }

        [TestMethod]
        public void DeleteComponent_WithReferences_ShouldThrow()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("Родитель", "Узел");
            _manager.AddComponent("Ребенок", "Деталь");
            _manager.AddRelation("Родитель", "Ребенок");

            // Act & Assert
            Assert.ThrowsException<Exception>(() => _manager.DeleteComponent("Ребенок"),
                "Нельзя удалить компонент, на который есть ссылки");
        }

        [TestMethod]
        public void RestoreComponent_ShouldBringBackComponentAndItsRelations()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("Родитель", "Узел");
            _manager.AddComponent("Ребенок", "Деталь");
            _manager.AddRelation("Родитель", "Ребенок");
            _manager.DeleteComponent("Ребенок");
            Assert.AreEqual(1, GetActiveComponentsList().Count); // только родитель

            // Act
            _manager.Restore("Ребенок");
            var active = GetActiveComponentsList();

            // Assert
            Assert.AreEqual(2, active.Count);
            Assert.IsTrue(active.Any(c => c.Name == "Ребенок"), "Ребенок должен быть восстановлен");

            // Проверим, что связь тоже восстановлена (родитель снова ссылается на ребенка)
            var parent = _manager.FindNode("Родитель");
            var child = _manager.FindNode("Ребенок");
            Assert.IsNotNull(parent);
            Assert.IsNotNull(child);
            // Проверим наличие связи (через обход спецификации родителя)
            bool found = false;
            int curSpec = parent.SpecNodePtr;
            while (curSpec != -1)
            {
                var spec = new SpecNodeHelper(_manager.GetSpecStream(), curSpec);
                if (spec.ProdNodePtr == child.Offset)
                {
                    found = true;
                    break;
                }
                curSpec = spec.NextNodePtr;
            }
            Assert.IsTrue(found, "Связь не восстановлена");
        }

        [TestMethod]
        public void RestoreAll_ShouldRestoreAllDeleted()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("A", "Узел");
            _manager.AddComponent("B", "Деталь");
            _manager.AddRelation("A", "B");
            _manager.DeleteComponent("B");
            _manager.DeleteComponent("A"); // теперь оба удалены
            Assert.AreEqual(0, GetActiveComponentsList().Count);

            // Act
            _manager.RestoreAll();
            var active = GetActiveComponentsList();

            // Assert
            Assert.AreEqual(2, active.Count);
            Assert.IsTrue(active.Any(c => c.Name == "A"));
            Assert.IsTrue(active.Any(c => c.Name == "B"));
        }

        [TestMethod]
        public void Truncate_ShouldPhysicallyRemoveDeletedRecords()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("Keep", "Узел");
            _manager.AddComponent("Remove", "Деталь");
            _manager.AddRelation("Keep", "Remove");
            _manager.DeleteComponent("Remove");

            long oldProdSize = new FileInfo(_prodPath).Length;
            long oldSpecSize = new FileInfo(_specPath).Length;

            // Act
            _manager.Truncate();

            // Assert
            long newProdSize = new FileInfo(_prodPath).Length;
            long newSpecSize = new FileInfo(_specPath).Length;
            Assert.IsTrue(newProdSize < oldProdSize, "Размер .prd должен уменьшиться");
            Assert.IsTrue(newSpecSize < oldSpecSize, "Размер .prs должен уменьшиться");

            // Проверим, что Keep остался, а Remove нет
            var active = GetActiveComponentsList();
            Assert.AreEqual(1, active.Count);
            Assert.AreEqual("Keep", active[0].Name);
        }

        [TestMethod]
        public void UpdateComponent_ChangeTypeToProductWhenUsedAsChild_ShouldThrow()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("Родитель", "Узел");
            _manager.AddComponent("Ребенок", "Деталь"); // сейчас деталь
            _manager.AddRelation("Родитель", "Ребенок");
            var childNode = _manager.FindNode("Ребенок");

            // Act & Assert
            Assert.ThrowsException<Exception>(() =>
                _manager.UpdateComponent(childNode.Offset, "Ребенок", ComponentTypes.Product),
                "Нельзя сменить тип на Изделие, если компонент используется как комплектующий");
        }

        [TestMethod]
        public void UpdateComponent_ChangeTypeToDetailWhenHasSpec_ShouldThrow()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("Узел", "Узел");
            _manager.AddComponent("Деталь", "Деталь");
            _manager.AddRelation("Узел", "Деталь");
            var node = _manager.FindNode("Узел");

            // Act & Assert
            Assert.ThrowsException<Exception>(() =>
                _manager.UpdateComponent(node.Offset, "Узел", ComponentTypes.Detail),
                "Нельзя сменить тип на Деталь, если у компонента есть спецификация");
        }

        [TestMethod]
        public void Open_ShouldOpenExistingDatabase()
        {
            // Arrange
            _manager.Create("Testing", 20);
            _manager.AddComponent("Тест", "Узел");
            _manager.Dispose(); // закрываем файлы

            // Act
            var newManager = new DataManager();
            newManager.Open("Testing.prd");
            var components = newManager.GetActiveComponents().Select(c => c.Name).ToList();

            // Assert
            Assert.AreEqual(1, components.Count);
            Assert.AreEqual("Тест", components[0]);

            newManager.Dispose();
        }

        [TestMethod]
        public void Open_WhenFileAlreadyOpened_ShouldThrow()
        {
            // Arrange
            _manager.Create("Testing", 20);
            // Пытаемся открыть тот же файл через другой менеджер (файл уже открыт текущим менеджером)
            using var secondManager = new DataManager();

            // Act & Assert
            Assert.ThrowsException<InvalidOperationException>(() => secondManager.Open("Testing.prd"),
                "Должно быть исключение о том, что файл уже открыт");
        }
    }
}