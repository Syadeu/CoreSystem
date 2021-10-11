namespace Syadeu.Collections
{
    internal struct SQLiteByteTable
    {
        [SQLiteDatabase(IsPrimaryKey = true)] public int Idx { get; set; }
        [SQLiteDatabase] public string Name { get; set; }
        [SQLiteDatabase] public byte[] Bytes { get; set; }
    }
}