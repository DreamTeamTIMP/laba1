using System.Buffers.Binary;
using System.Text;

namespace laba1New
{
    public class DataManager : IDisposable
    {
        private byte[] _prodData;
        private byte[] _specData;
        private int _dataSpaceSize;
        private string _prodPath;
        private string _specPath;

        public void Create(string name, int dataSize)
        {
            _prodPath = name + ".prd";
            _specPath = name + ".prs";

            if (File.Exists(_prodPath))
            {
                // Проверка сигнатуры перед перезаписью
                byte[] existing = File.ReadAllBytes(_prodPath);
                if (existing.Length >= 2 && (existing[0] != 'P' || existing[1] != 'S'))
                    throw new Exception("Файл существует, но имеет неверную сигнатуру!");

                Console.Write($"Файл {name} уже существует. Перезаписать? (y/n): ");
                if (Console.ReadLine()?.ToLower() != "y") return;
            }

            _prodData = new byte[ProdHeaderOffset.TotalOffset];
            _specData = new byte[SpecHeaderOffset.TotalOffset];

            _prodData[ProdHeaderOffset.Signature] = (byte)'P';
            _prodData[ProdHeaderOffset.Signature + 1] = (byte)'S';

            _specData[SpecHeaderOffset.Signature] = (byte)'P';
            _specData[SpecHeaderOffset.Signature + 1] = (byte)'S';

            BinaryPrimitives.WriteInt16LittleEndian(_prodData.AsSpan(ProdHeaderOffset.CompDataSize), (short)dataSize);
            BinaryPrimitives.WriteInt32LittleEndian(_prodData.AsSpan(ProdHeaderOffset.FirstNodePtr), -1);
            BinaryPrimitives.WriteInt32LittleEndian(_prodData.AsSpan(ProdHeaderOffset.FreeSpacePtr), ProdHeaderOffset.TotalOffset);

            BinaryPrimitives.WriteInt32LittleEndian(_specData.AsSpan(SpecHeaderOffset.FirstNodePtr), -1);
            BinaryPrimitives.WriteInt32LittleEndian(_specData.AsSpan(SpecHeaderOffset.FreeSpacePtr), SpecHeaderOffset.TotalOffset);

            Save();
        }

        public void Save()
        {
            File.WriteAllBytes(_prodPath, _prodData);
            File.WriteAllBytes(_specPath, _specData);
        }

        public string DetermineType(int prodOffset)
        {
            var helper = new ProdNodeHelper(_prodData, _specData)[prodOffset];

            // 1. Простейшая проверка: если состава нет — это точно Деталь
            if (helper.SpecNodePtr == -1)
            {
                return "Деталь";
            }

            // 2. Если состав есть, нужно проверить, входит ли этот компонент в кого-то другого
            bool isReferenced = false;
            int currentSpecPtr = BinaryPrimitives.ReadInt32LittleEndian(_specData.AsSpan(SpecHeaderOffset.FirstNodePtr));

            while (currentSpecPtr != -1)
            {
                var specH = new SpecNodeHelper(_prodData, _specData)[currentSpecPtr];
                // Если кто-то в спецификациях ссылается на наш текущий prodOffset
                if (specH.CanBeDel == 0 && specH.ProdNodePtr == prodOffset)
                {
                    isReferenced = true;
                    break;
                }
                currentSpecPtr = specH.NextNodePtr;
            }

            // Если есть состав и на него ссылаются — это Узел
            // Если есть состав и на него никто не ссылается — это Изделие
            return isReferenced ? "Узел" : "Изделие";
        }

        public void AddProduct(string name, string type)
        {
            int currentFree = ProdHeaderData.FreeSpacePtr(_prodData);
            int recordSize = ProdNodeOffset.TotalOffset(_prodData);
        
            Array.Resize(ref _prodData, currentFree + recordSize);

            var helper = new ProdNodeHelper(_prodData, _specData);
            helper.SetOffset(currentFree);

            helper.CanBeDel = 0;
            if (type.ToLower() == "изделие" || type.ToLower() == "узел")
            {
                int specFree = SpecHeaderData.FreeSpacePtr(_specData);
                Array.Resize(ref _specData, specFree + SpecNodeOffset.TotalOffset);

                var sH = new SpecNodeHelper(_prodData, _specData).SetOffset(specFree);
                sH.CanBeDel = 0;
                sH.ProdNodePtr = -1; // Пустой указатель (резерв)
                sH.NextNodePtr = -1;

                helper.SpecNodePtr = specFree; // Ссылка на резерв

                // Обновляем свободное место в .prs
                BinaryPrimitives.WriteInt32LittleEndian(_specData.AsSpan(SpecHeaderOffset.FreeSpacePtr), specFree + SpecNodeOffset.TotalOffset);
            }
            else helper.SpecNodePtr = -1;
            int oldHead = BinaryPrimitives.ReadInt32LittleEndian(_prodData.AsSpan(ProdHeaderOffset.FirstNodePtr));
            helper.NextNodePtr = oldHead;

            var nameBytes = System.Text.Encoding.Default.GetBytes(name);
            helper.Data = nameBytes;

            BinaryPrimitives.WriteInt32LittleEndian(_prodData.AsSpan(ProdHeaderOffset.FirstNodePtr), currentFree);
            BinaryPrimitives.WriteInt32LittleEndian(_prodData.AsSpan(ProdHeaderOffset.FreeSpacePtr), currentFree + recordSize);

            Save();
        }
        public void AddRelation(string ownerName, string partName, ushort count)
        {
            // 1. Найти оффсеты владельца и детали (перебором по NextNodePtr)
            int ownerOffset = FindProductOffset(ownerName);
            int partOffset = FindProductOffset(partName);

            var prodHelper = new ProdNodeHelper(_prodData, _specData)[ownerOffset];

            if (prodHelper.SpecNodePtr == -1)
                throw new Exception("Ошибка: Нельзя добавить комплектующее в Деталь!");

            // 2. Создать новую запись в SpecData
            int specFree = SpecHeaderData.FreeSpacePtr(_specData);
            Array.Resize(ref _specData, specFree + SpecNodeOffset.TotalOffset);

            var specHelper = new SpecNodeHelper(_prodData, _specData);
            specHelper.SetOffset(specFree);

            // 3. Настройка связей
            specHelper.CanBeDel = 0;
            specHelper.ProdNodePtr = partOffset;
            specHelper.Mentions = count;
            specHelper.NextNodePtr = prodHelper.SpecNodePtr; // В начало списка спецификаций изделия

            prodHelper.SpecNodePtr = specFree; // Узел теперь ссылается на новую запись в .prs

            // 4. Обновить заголовок .prs
            BinaryPrimitives.WriteInt32LittleEndian(_specData.AsSpan(SpecHeaderOffset.FreeSpacePtr), specFree + SpecNodeOffset.TotalOffset);

            Save();
        }
        public void PrintTree(string name)
        {
            int offset = FindProductOffset(name);
            if (offset == -1) return;

            PrintRecursive(offset, 0);
        }

        private int FindProductOffset(string name)
        {
            int currentPtr = BinaryPrimitives.ReadInt32LittleEndian(_prodData.AsSpan(ProdHeaderOffset.FirstNodePtr));

            while (currentPtr != -1)
            {
                var helper = new ProdNodeHelper(_prodData, _specData)[currentPtr];
                string currentName = Encoding.Default.GetString(helper.Data.ToArray()).Trim('\0', ' ');
                if (currentName == name && helper.CanBeDel == 0) return currentPtr;

                currentPtr = helper.NextNodePtr;
            }
            return -1;
        }

        private void PrintRecursive(int prodOffset, int level)
        {
            var prod = new ProdNodeHelper(_prodData, _specData)[prodOffset];
            Console.WriteLine(new string(' ', level * 4) + System.Text.Encoding.Default.GetString(prod.Data.ToArray()).Trim());

            // Идем по цепочке спецификации
            var currentSpec = prod.Spec;
            while (currentSpec != null)
            {
                PrintRecursive(currentSpec.ProdNodePtr, level + 1);
                currentSpec = currentSpec.Next;
            }
        }

        public void Open(string name)
        {
            _prodPath = name + ".prd";
            _specPath = name + ".prs";

            if (!File.Exists(_prodPath) || !File.Exists(_specPath))
                throw new FileNotFoundException("Файлы при открытии были не найдены");

            _prodData = File.ReadAllBytes(_prodPath);
            _specData = File.ReadAllBytes(_specPath);
            _dataSpaceSize = ProdHeaderData.DataSpaceSize(_prodData);
            if (_prodData[0] != 'P' || _prodData[1] != 'S') throw new Exception("Неверная сигнатура!");
            Console.WriteLine($"{name} успешно открыта");
        }

        public void PrintAll()
        {
            int currentPtr = BinaryPrimitives.ReadInt32LittleEndian(_prodData.AsSpan(ProdHeaderOffset.FirstNodePtr));
            Console.WriteLine(string.Format("\n{0,-20} | {1}", "Наименование", "Адрес (смещение)"));
            while (currentPtr != -1)
            {
                var helper = new ProdNodeHelper(_prodData, _specData)[currentPtr];
                if (helper.CanBeDel == 0)
                {
                    string name = Encoding.Default.GetString(helper.Data.ToArray()).Trim('\0', ' ');
                    Console.WriteLine(string.Format("{0,-20} | {1}", name, currentPtr));
                }
                currentPtr = helper.NextNodePtr;
            }
            Console.WriteLine();
        }
        public void DeleteProduct(string name)
        {
            int targetOffset = FindProductOffset(name);
            if (targetOffset == -1)
            {
                Console.WriteLine("Ошибка: Компонент не найден.");
                return;
            }
            int currentSpecPtr = BinaryPrimitives.ReadInt32LittleEndian(_specData.AsSpan(SpecHeaderOffset.FirstNodePtr));
            while (currentSpecPtr != -1)
            {
                var specHelper = new SpecNodeHelper(_prodData,_specData)[currentSpecPtr];
                if (specHelper.CanBeDel == 0 && specHelper.ProdNodePtr == targetOffset)
                {
                    Console.WriteLine($"Компонент '{name}' используется в других изделиях! Удаление запрещено.");
                    return;
                }
                currentSpecPtr = specHelper.NextNodePtr;
            }

            var prodHelper = new ProdNodeHelper(_prodData, _specData)[targetOffset];
            prodHelper.CanBeDel = -1;
            Save();
            Console.WriteLine($"Компонент '{name}' логически удален.");
        }

        public void DeleteRelation(string ownerName, string partName)
        {
            int ownerOff = FindProductOffset(ownerName);
            if (ownerOff == -1) throw new Exception("Компонент не найден.");

            var prodH = new ProdNodeHelper(_prodData, _specData)[ownerOff];
            var specH = prodH.Spec;

            while (specH != null)
            {
                var partH = specH.Prod;
                string currentPartName = Encoding.UTF8.GetString(partH.Data.ToArray()).Trim('\0', ' ');

                if (currentPartName == partName)
                {
                    specH.CanBeDel = -1; // Логическое удаление из спецификации
                    Save();
                    Console.WriteLine("Связь удалена.");
                    return;
                }
                specH = specH.Next;
            }
            throw new Exception("Такая деталь не найдена в составе данного узла.");
        }

        public void RestoreAll()
        {
            // Восстановление в .prd
            int ptr = ProdHeaderOffset.TotalOffset;
            int recordSize = ProdNodeOffset.TotalOffset(_prodData);
            while (ptr < _prodData.Length)
            {
                _prodData[ptr + ProdNodeOffset.CanBeDel] = 0;
                ptr += recordSize;
            }

            // Восстановление в .prs
            ptr = SpecHeaderOffset.TotalOffset;
            while (ptr < _specData.Length)
            {
                _specData[ptr + SpecNodeOffset.CanBeDel] = 0;
                ptr += SpecNodeOffset.TotalOffset;
            }
            Save();
            Console.WriteLine("Все записи восстановлены.");
        }
        public void Truncate()
        {

        }
        public void Dispose()
        {

        }
    }
}