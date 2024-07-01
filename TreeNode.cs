namespace CodeAggregatorGtk
{
    public class TreeNode
    {
        public string Path { get; set; }
        public bool IsSelected { get; set; }
        public List<TreeNode> Children { get; set; }

        public TreeNode(string path)
        {
            Path = path;
            IsSelected = false;
            Children = new List<TreeNode>();
        }

        public void AddChild(TreeNode child)
        {
            Children.Add(child);
        }
    }
}
