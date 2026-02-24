using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace laba1.Helpers
{
    public class TreePrinter
    {
        private readonly FileStream _prodFs;
        private readonly FileStream _specFs;
        private readonly ushort _nameSize;

        public TreePrinter(FileStream prodFs, FileStream specFs, ushort nameSize)
        {
            _prodFs = prodFs;
            _specFs = specFs;
            _nameSize = nameSize;
        }

        public void Print(ProdNodeHelper rootNode)
        {
            Console.WriteLine($"\n{rootNode.Name}");
            PrintRecursive(rootNode.SpecNodePtr, 1);
        }

        private void PrintRecursive(int specOffset, int level)
        {
            int currentEntry = specOffset;
            while (currentEntry != -1)
            {
                var spec = new SpecNodeHelper(_specFs, currentEntry);
                if (spec.CanBeDel == 0)
                {
                    var component = new ProdNodeHelper(_prodFs, spec.ProdNodePtr, _nameSize);
                    if (component.CanBeDel != 0) continue;

                    string indent = new string('|', level).Replace("|", "|   ");
                    Console.WriteLine($"{indent}|");
                    Console.WriteLine($"{indent} {component.Name} (x{spec.Mentions})");

                    if (component.SpecNodePtr != -1)
                        PrintRecursive(component.SpecNodePtr, level + 1);
                }
                currentEntry = spec.NextNodePtr;
            }
        }
    }
}
