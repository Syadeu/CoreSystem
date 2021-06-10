namespace SyadeuEditor.Tree
{
    public sealed class TreeFolderElement : TreeElementEntity
    {
        public TreeFolderElement(string name, int depth, int id) : base(name, depth, id)
        {
        }
        public TreeFolderElement(string name, int depth, System.Func<int> id) : base(name, depth, id.Invoke())
        {

        }
    }
}