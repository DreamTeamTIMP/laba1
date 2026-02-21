using System.Buffers.Binary;
using System.Text;

namespace laba1New
{
    public class DataManager : IDisposable
    {
        private byte[] _prodData;
        private byte[] _specData;
        private string _prodPath;
        private string _specPath;
        private int _dataSpaceSize;

        public void Create(string name, int dataSize)
        {
            _prodPath = name + ".prd";
            _specPath = name + ".prs";

            _prodData = new byte[ProdHeaderOffset.TotalOffset];
            _specData = new byte[SpecHeaderOffset.TotalOffset];

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

        public void AddProduct(string name)
        {
            int currentFree = ProdHeaderData.FreeSpacePtr(_prodData);
            int recordSize = ProdNodeOffset.TotalOffset(_prodData);
        
            Array.Resize(ref _prodData, currentFree + recordSize);

            var helper = new ProdNodeHelper(_prodData, _specData);
            helper.SetOffset(currentFree);

            helper.CanBeDel = 0;
            helper.SpecNodePtr = -1;
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

            // 2. Создать новую запись в SpecData
            int specFree = SpecHeaderData.FreeSpacePtr(_specData);
            Array.Resize(ref _specData, specFree + SpecNodeOffset.TotalOffset);

            var specHelper = new SpecNodeHelper(_prodData, _specData);
            specHelper.SetOffset(specFree);

            var prodHelper = new ProdNodeHelper(_prodData, _specData)[ownerOffset];

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

        public void Truncate()
        {

        }
        public void Dispose()
        {

        }
    }
}