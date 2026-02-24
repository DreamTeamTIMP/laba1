namespace laba1.Helpers
{
    public class SpecTreeNode
    {
        public int ProdOffset { get; set; }
        public int SpecOffset { get; set; }
        public string Name { get; set; }
        public byte Type { get; set; }
        public ushort Mentions { get; set; }
        public string Text { get; set; }
        public List<SpecTreeNode> Children { get; set; } = new List<SpecTreeNode>();
    }
}