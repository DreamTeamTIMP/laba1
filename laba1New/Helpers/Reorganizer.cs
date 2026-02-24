using System;
using System.Collections.Generic;
using System.Linq;
using laba1.Helpers;
using System.Text;
using System.Threading.Tasks;

namespace laba1.Helpers
{
    public class Reorganizer
    {
        private readonly FileStream _prodFs;
        private readonly FileStream _specFs;
        private readonly ushort _nameSize;
        private readonly FileHeaderHelper _headerHelper;

        public Reorganizer(FileStream prodFs, FileStream specFs, ushort nameSize, FileHeaderHelper headerHelper)
        {
            _prodFs = prodFs;
            _specFs = specFs;
            _nameSize = nameSize;
            _headerHelper = headerHelper;
        }

        /// <summary>
        /// Перестроение алфавитного порядка всех активных записей .prd
        /// </summary>
        public void ReorderAll()
        {
            // Собираем все активные компоненты (CanBeDel == 0)
            var activeNodes = new List<(int offset, string name)>();
            int curr = 28;
            int freeSpace = _headerHelper.GetFreeProd();
            while (curr < freeSpace)
            {
                var node = new ProdNodeHelper(_prodFs, curr, _nameSize);
                if (node.CanBeDel == 0)
                    activeNodes.Add((curr, node.Name));
                curr += node.TotalSize;
            }

            // Сортируем по имени (без учёта регистра)
            activeNodes.Sort((a, b) => string.Compare(a.name, b.name, StringComparison.OrdinalIgnoreCase));

            // Обновляем голову списка
            if (activeNodes.Count > 0)
            {
                _headerHelper.SetFirstProd(activeNodes[0].offset);
            }
            else
            {
                _headerHelper.SetFirstProd(-1);
                return;
            }

            // Проставляем указатели на следующий узел
            for (int i = 0; i < activeNodes.Count - 1; i++)
            {
                var node = new ProdNodeHelper(_prodFs, activeNodes[i].offset, _nameSize);
                node.NextNodePtr = activeNodes[i + 1].offset;
            }

            // Последний узел указывает на -1
            var lastNode = new ProdNodeHelper(_prodFs, activeNodes[^1].offset, _nameSize);
            lastNode.NextNodePtr = -1;
        }

        /// <summary>
        /// Восстановление всех логически удалённых записей (сбрасывание флага CanBeDel)
        /// </summary>
        public void RestoreAll()
        {
            int curr = 28;
            int freeProd = _headerHelper.GetFreeProd();
            while (curr < freeProd)
            {
                var node = new ProdNodeHelper(_prodFs, curr, _nameSize);
                node.CanBeDel = 0;
                curr += node.TotalSize;
            }

            int currSpec = 8;
            int freeSpec = _headerHelper.GetFreeSpec();
            while (currSpec < freeSpec)
            {
                var spec = new SpecNodeHelper(_specFs, currSpec);
                spec.CanBeDel = 0;
                currSpec += 11; // фиксированный размер записи спецификации
            }

            ReorderAll();
        }
    }
}
