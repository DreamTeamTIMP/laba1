using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace laba1.Helpers
{
    // Класс помощник для физического удаления
    public class TruncateHelper
    {
        private readonly FileStream _prodFs;
        private readonly FileStream _specFs;
        private readonly ushort _nameSize;
        private readonly FileHeaderHelper _headerHelper;

        public TruncateHelper(FileStream prodFs, FileStream specFs, ushort nameSize, FileHeaderHelper headerHelper)
        {
            _prodFs = prodFs;
            _specFs = specFs;
            _nameSize = nameSize;
            _headerHelper = headerHelper;
        }

        /// <summary>
        /// Физическое удаление помеченных записей (перепаковка файлов)
        /// </summary>
        public void Truncate()
        {
            // 1. Сохраняем старые границы файлов
            int oldFreeProd = _headerHelper.GetFreeProd();
            int oldFreeSpec = _headerHelper.GetFreeSpec();

            // 2. Собираем все активные компоненты из .prd
            var activeComps = new List<(int oldOffset, byte type, int oldSpecPtr, string name)>();
            int curr = 28;
            while (curr < oldFreeProd)
            {
                var node = new ProdNodeHelper(_prodFs, curr, _nameSize);
                if (node.CanBeDel == 0)
                    activeComps.Add((curr, node.Type, node.SpecNodePtr, node.Name));
                curr += node.TotalSize;
            }

            // 3. Маппинг старых смещений компонентов на новые
            var oldToNewComp = new Dictionary<int, int>();
            int newCompOffset = 28;
            int compSize = 10 + _nameSize;
            foreach (var comp in activeComps)
            {
                oldToNewComp[comp.oldOffset] = newCompOffset;
                newCompOffset += compSize;
            }

            // 4. Читаем все записи спецификаций из .prs
            var allSpecs = new Dictionary<int, (sbyte canBeDel, int prodPtr, ushort mentions, int nextPtr)>();
            curr = 8;
            while (curr < oldFreeSpec)
            {
                var spec = new SpecNodeHelper(_specFs, curr);
                allSpecs[curr] = (spec.CanBeDel, spec.ProdNodePtr, spec.Mentions, spec.NextNodePtr);
                curr += 11;
            }

            // 5. Для каждого активного компонента собираем цепочки активных записей спецификации,
            //    которые ссылаются на активные компоненты
            var compToSpecs = new Dictionary<int, List<int>>(); // ключ – старый offset компонента
            foreach (var comp in activeComps)
            {
                var specOffsets = new List<int>();
                int curSpec = comp.oldSpecPtr;
                while (curSpec != -1)
                {
                    if (allSpecs.TryGetValue(curSpec, out var specData) &&
                        specData.canBeDel == 0 &&
                        oldToNewComp.ContainsKey(specData.prodPtr))
                    {
                        specOffsets.Add(curSpec);
                    }
                    curSpec = allSpecs.ContainsKey(curSpec) ? allSpecs[curSpec].nextPtr : -1;
                }
                if (specOffsets.Count > 0)
                    compToSpecs[comp.oldOffset] = specOffsets;
            }

            // 6. Определяем новые смещения для всех активных записей спецификации,
            //    сохраняя порядок обхода компонентов (это станет глобальным порядком)
            var oldToNewSpec = new Dictionary<int, int>();
            int newSpecOffset = 8;
            foreach (var comp in activeComps)
            {
                if (compToSpecs.TryGetValue(comp.oldOffset, out var specList))
                {
                    foreach (var oldSpec in specList)
                    {
                        oldToNewSpec[oldSpec] = newSpecOffset;
                        newSpecOffset += 11;
                    }
                }
            }
            int totalActiveSpecs = oldToNewSpec.Count;

            // 8. Записываем новый файл спецификаций
            string tempSpecPath = _specFs.Name + ".tmp";
            using (var newSpecFs = new FileStream(tempSpecPath, FileMode.Create))
            {
                // Заголовок: FirstSpec = -1 (не используется), FreeSpace
                WriteInt(newSpecFs, -1);
                WriteInt(newSpecFs, 8 + totalActiveSpecs * 11);

                // Записываем записи группами по родителям
                foreach (var comp in activeComps)
                {
                    if (compToSpecs.TryGetValue(comp.oldOffset, out var specList))
                    {
                        for (int i = 0; i < specList.Count; i++)
                        {
                            int oldSpec = specList[i];
                            var data = allSpecs[oldSpec];
                            int newOff = oldToNewSpec[oldSpec];
                            int nextInGroup = (i < specList.Count - 1) ? oldToNewSpec[specList[i + 1]] : -1;

                            newSpecFs.WriteByte(0); // canBeDel
                            WriteInt(newSpecFs, oldToNewComp[data.prodPtr]);
                            WriteUshort(newSpecFs, data.mentions);
                            WriteInt(newSpecFs, nextInGroup);
                        }
                    }
                }
            }

            // 9. Записываем новый файл компонентов
            string tempProdPath = _prodFs.Name + ".tmp";
            using (var newProdFs = new FileStream(tempProdPath, FileMode.Create))
            {
                // Заголовок
                newProdFs.Write(Encoding.ASCII.GetBytes("PS"), 0, 2);
                WriteUshort(newProdFs, _nameSize);
                int firstProd = activeComps.Count > 0 ? oldToNewComp[activeComps[0].oldOffset] : -1;
                WriteInt(newProdFs, firstProd);
                // freeSpace
                WriteInt(newProdFs, 28 + activeComps.Count * compSize); 

                // Имя файла спецификации в заголовке (ASCII)
                byte[] nameBuf = new byte[16];
                string specFileName = Path.GetFileName(_specFs.Name);
                Encoding.ASCII.GetBytes(specFileName.PadRight(16)).CopyTo(nameBuf, 0);
                newProdFs.Write(nameBuf, 0, 16);

                // Кодировка для имён компонентов (windows-1251)
                Encoding nameEncoding = Encoding.GetEncoding(1251);

                // Записи компонентов
                for (int i = 0; i < activeComps.Count; i++)
                {
                    var comp = activeComps[i];
                    int newOff = oldToNewComp[comp.oldOffset];
                    int newNext = (i < activeComps.Count - 1) ? oldToNewComp[activeComps[i + 1].oldOffset] : -1;
                    int newSpecPtr = -1;
                    if (compToSpecs.TryGetValue(comp.oldOffset, out var specList) && specList.Count > 0)
                        newSpecPtr = oldToNewSpec[specList[0]];

                    newProdFs.Seek(newOff, SeekOrigin.Begin);
                    // canBeDel
                    newProdFs.WriteByte(0); 
                    newProdFs.WriteByte(comp.type);
                    WriteInt(newProdFs, newSpecPtr);
                    WriteInt(newProdFs, newNext);

                    // Запись имени компонента
                    byte[] nameBytes = new byte[_nameSize];
                    byte[] src = nameEncoding.GetBytes(comp.name);

                    if (src.Length > _nameSize)
                        throw new Exception($"Имя компонента '{comp.name}' слишком длинное (макс. {_nameSize} байт)");

                    Array.Copy(src, nameBytes, src.Length);
                    for (int j = src.Length; j < _nameSize; j++)
                        nameBytes[j] = 32; // пробел

                    newProdFs.Write(nameBytes, 0, _nameSize);
                }
            }

            // 10. Замена старых файлов новыми
            _prodFs.Close();
            _specFs.Close();
            File.Delete(_prodFs.Name);
            File.Move(tempProdPath, _prodFs.Name);
            File.Delete(_specFs.Name);
            File.Move(tempSpecPath, _specFs.Name);
        }

        private void WriteInt(FileStream fs, int value)
        {
            fs.Write(BitConverter.GetBytes(value), 0, 4);
        }

        private void WriteUshort(FileStream fs, ushort value)
        {
            fs.Write(BitConverter.GetBytes(value), 0, 2);
        }
    }
}
