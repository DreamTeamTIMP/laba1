using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace laba1.Helpers
{
    public class NodeFinder
    {
        private readonly FileStream _prodFs;
        private readonly FileStream _specFs;
        private readonly ushort _nameSize;

        public NodeFinder(FileStream prodFs, FileStream specFs, ushort nameSize)
        {
            _prodFs = prodFs;
            _specFs = specFs;
            _nameSize = nameSize;
        }

        public ProdNodeHelper? FindNode(string name, int startFrom, bool includeDeleted = false)
        {
            int curr = startFrom;
            while (curr != -1)
            {
                var n = new ProdNodeHelper(_prodFs, curr, _nameSize);
                if (n.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (includeDeleted || n.CanBeDel == 0) return n;
                }
                curr = n.NextNodePtr;
            }
            return null;
        }

        public bool HasReferences(int prodOffset, int firstProd)
        {
            int currComp = firstProd;
            while (currComp != -1)
            {
                var comp = new ProdNodeHelper(_prodFs, currComp, _nameSize);
                if (comp.CanBeDel == 0)
                {
                    int currSpec = comp.SpecNodePtr;
                    while (currSpec != -1)
                    {
                        var spec = new SpecNodeHelper(_specFs, currSpec);
                        if (spec.CanBeDel == 0 && spec.ProdNodePtr == prodOffset)
                            return true;
                        currSpec = spec.NextNodePtr;
                    }
                }
                currComp = comp.NextNodePtr;
            }
            return false;
        }

        public bool IsAncestor(int potentialAncestorOffset, int nodeOffset)
        {
            var node = new ProdNodeHelper(_prodFs, nodeOffset, _nameSize);
            if (node.SpecNodePtr == -1) return false;
            int currSpec = node.SpecNodePtr;
            while (currSpec != -1)
            {
                var spec = new SpecNodeHelper(_specFs, currSpec);
                if (spec.CanBeDel == 0)
                {
                    int childOffset = spec.ProdNodePtr;
                    if (childOffset == potentialAncestorOffset)
                        return true;
                    if (IsAncestor(potentialAncestorOffset, childOffset))
                        return true;
                }
                currSpec = spec.NextNodePtr;
            }
            return false;
        }
    }
}
