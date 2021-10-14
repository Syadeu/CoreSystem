namespace Syadeu.Collections
{
    [SQLiteTable]
    public struct SQLiteVersionInfoTableData
    {
        [SQLiteDatabase(IsPrimaryKey = true)] public string Version { get; set; }
    }
}