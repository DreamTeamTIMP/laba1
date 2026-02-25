using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace laba1.Helpers
{
    // Класс-помощник печати дерева
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
        public SpecTreeNode BuildSpecTree(int prodOffset)
        {
            var prodNode = new ProdNodeHelper(_prodFs!, prodOffset, _nameSize);
            string typeStr = prodNode.Type switch
            {
                ComponentTypes.Product => "Изделие",
                ComponentTypes.Node => "Узел",
                ComponentTypes.Detail => "Деталь",
                _ => "Неизвестно"
            };
            var treeNode = new SpecTreeNode
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
                    var spec = new SpecNodeHelper(_specFs!, currSpec);
                    if (spec.CanBeDel == 0)
                    {
                        int childOffset = spec.ProdNodePtr;
                        var childProd = new ProdNodeHelper(_prodFs!, childOffset, _nameSize);
                        if (childProd.CanBeDel == 0)
                        {
                            var childNode = BuildSpecTree(childOffset);
                            if (childNode != null)
                            {
                                childNode.Mentions = spec.Mentions;
                                childNode.SpecOffset = currSpec;
                                childNode.Text = $"{childProd.Name} (x{spec.Mentions}) {childProd.Type switch
                                {
                                    ComponentTypes.Product => "Изделие",
                                    ComponentTypes.Node => "Узел",
                                    ComponentTypes.Detail => "Деталь",
                                    _ => "Неизвестно"
                                }}";
                                treeNode.Children.Add(childNode);
                            }
                        }
                    }
                    currSpec = spec.NextNodePtr;
                }
            }
            return treeNode;
        }

    }
}
