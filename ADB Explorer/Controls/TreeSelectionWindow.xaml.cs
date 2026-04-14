using ADB_Explorer.Helpers;
using ADB_Explorer.Models;
using ADB_Explorer.ViewModels;
using static ADB_Explorer.Models.AbstractFile;

namespace ADB_Explorer.Controls;

public partial class TreeSelectionWindow : Window
{
    private IReadOnlyList<FileClass> TopLevelFiles { get; }
    public IReadOnlyList<FileClass> SelectedFiles { get; private set; } = [];

    public TreeSelectionWindow(IReadOnlyList<FileClass> topLevelFiles)
    {
        InitializeComponent();
        TopLevelFiles = topLevelFiles;
        BuildTree();
    }

    private void BuildTree()
    {
        var roots = new List<TreeSelectionNode>();

        foreach (var file in TopLevelFiles.OrderBy(f => !f.IsDirectory).ThenBy(f => f.FullName, StringComparer.CurrentCultureIgnoreCase))
        {
            if (!file.IsDirectory)
            {
                roots.Add(new TreeSelectionNode(file));
                continue;
            }

            var root = new TreeSelectionNode(file);
            var subtree = BuildDirectorySubtree(file, root);
            root.SetChildren(subtree);
            root.IsLoaded = true;
            roots.Add(root);
        }

        SelectionTree.ItemsSource = roots;
    }

    private static IEnumerable<TreeSelectionNode> BuildDirectorySubtree(FileClass root, TreeSelectionNode rootNode)
    {
        (string, long?, double?)[] tree;
        try
        {
            tree = FileHelper.GetFolderTree([root.FullPath]);
        }
        catch (Exception)
        {
            return [];
        }

        if (tree.Length == 0)
            return [];

        var allFiles = tree
            .Select(entry => new FileClass(
                FileHelper.GetFullName(entry.Item1),
                entry.Item1,
                entry.Item2 is null ? FileType.Folder : FileType.File,
                size: entry.Item2))
            .ToList();

        List<TreeSelectionNode> BuildChildren(string parentPath, TreeSelectionNode parentNode)
        {
            var directChildren = allFiles
                .Where(f => string.Equals(FileHelper.GetParentPath(f.FullPath), parentPath, StringComparison.Ordinal))
                .OrderBy(f => !f.IsDirectory)
                .ThenBy(f => f.FullName, StringComparer.CurrentCultureIgnoreCase)
                .ToList();

            var nodes = new List<TreeSelectionNode>(directChildren.Count);
            foreach (var file in directChildren)
            {
                var node = new TreeSelectionNode(file, parentNode);
                if (file.IsDirectory)
                {
                    node.SetChildren(BuildChildren(file.FullPath, node));
                    node.IsLoaded = true;
                }

                nodes.Add(node);
            }

            return nodes;
        }

        return BuildChildren(root.FullPath, rootNode);
    }

    private void Apply_Click(object sender, RoutedEventArgs e)
    {
        var roots = SelectionTree.ItemsSource as IEnumerable<TreeSelectionNode>;
        if (roots is null)
        {
            DialogResult = false;
            Close();
            return;
        }

        var selected = roots
            .SelectMany(r => r.EnumerateCheckedLeavesAndFolders())
            .Select(node => new FileClass(
                FileHelper.GetFullName(node.FullPath),
                node.FullPath,
                node.IsDirectory ? FileType.Folder : FileType.File))
            .ToList();

        SelectedFiles = selected;
        DialogResult = true;
        Close();
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void SelectionTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
    {
    }
}
