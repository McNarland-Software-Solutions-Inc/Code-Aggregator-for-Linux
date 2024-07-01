using System.Collections.Generic;
using Gtk;

namespace CodeAggregatorGtk
{
    public class TreeViewHandler
    {
        private readonly TreeView folderTreeView;
        private readonly TreeNode rootNode;

        public TreeViewHandler(TreeView treeView, TreeNode root)
        {
            folderTreeView = treeView;
            rootNode = root;
        }

        public void PopulateTreeView(List<string> selectedNodes)
        {
            var store = new TreeStore(typeof(bool), typeof(string), typeof(string));
            folderTreeView.Model = store;

            if (folderTreeView.Columns.Length == 0)
            {
                var toggleRenderer = new CellRendererToggle();
                toggleRenderer.Toggled += OnToggled;

                folderTreeView.AppendColumn("Include", toggleRenderer, "active", 0);
                folderTreeView.AppendColumn("Name", new CellRendererText(), "text", 1);
            }

            PopulateTreeView(store, rootNode, null, selectedNodes);
        }

        private void PopulateTreeView(TreeStore store, TreeNode node, TreeIter? parent, List<string> selectedNodes)
        {
            var iter = parent.HasValue
                ? store.AppendValues(parent.Value, selectedNodes.Contains(node.Path), System.IO.Path.GetFileName(node.Path), node.Path)
                : store.AppendValues(selectedNodes.Contains(node.Path), System.IO.Path.GetFileName(node.Path), node.Path);

            foreach (var child in node.Children)
            {
                PopulateTreeView(store, child, iter, selectedNodes);
            }
        }

        private void OnToggled(object sender, ToggledArgs args)
        {
            if (folderTreeView.Model is TreeStore store)
            {
                if (store.GetIterFromString(out TreeIter iter, args.Path))
                {
                    bool active = (bool)store.GetValue(iter, 0);
                    store.SetValue(iter, 0, !active);
                }
            }
        }

        public List<string> GetSelectedNodes()
        {
            var selectedNodes = new List<string>();
            if (folderTreeView.Model is TreeStore store)
            {
                if (store.GetIterFirst(out TreeIter iter))
                {
                    CollectSelectedNodes(store, iter, selectedNodes);
                }
            }
            return selectedNodes;
        }

        private void CollectSelectedNodes(TreeStore store, TreeIter iter, List<string> selectedNodes)
        {
            do
            {
                bool isSelected = (bool)store.GetValue(iter, 0);
                string path = (string)store.GetValue(iter, 2);

                if (isSelected)
                {
                    selectedNodes.Add(path);
                }

                if (store.IterChildren(out TreeIter childIter, iter))
                {
                    CollectSelectedNodes(store, childIter, selectedNodes);
                }
            } while (store.IterNext(ref iter));
        }

        public bool AnyFileSelected()
        {
            if (folderTreeView.Model is TreeStore store)
            {
                if (store.GetIterFirst(out TreeIter iter))
                {
                    return CheckAnyFileSelected(store, iter);
                }
            }
            return false;
        }

        private bool CheckAnyFileSelected(TreeStore store, TreeIter iter)
        {
            do
            {
                bool isSelected = (bool)store.GetValue(iter, 0);
                if (isSelected)
                {
                    return true;
                }

                if (store.IterChildren(out TreeIter childIter, iter))
                {
                    if (CheckAnyFileSelected(store, childIter))
                    {
                        return true;
                    }
                }
            } while (store.IterNext(ref iter));

            return false;
        }
    }
}
