using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Syadeu;
using Syadeu.Database;
using System.IO;
using System.Linq;

public class SQLiteDatabaseTests
{
    [SQLiteTable]
    public struct SQliteTestStruct : System.IEquatable<SQliteTestStruct>
    {
        [SQLiteDatabase(IsPrimaryKey = true)] public int Idx { get; set; }

        // Convertibles
        [SQLiteDatabase] public string TestString { get; set; }
        [SQLiteDatabase] public int TestInt32 { get; set; }
        [SQLiteDatabase] public long TestInt64 { get; set; }
        [SQLiteDatabase] public double TestDouble { get; set; }
        [SQLiteDatabase] public float TestSingle { get; set; }

        [SQLiteDatabase] public byte[] TestBytes { get; set; }

        // Collections
        [SQLiteDatabase] public List<int> TestInt32List { get; set; }
        [SQLiteDatabase] public int[] TestInt32Array { get; set; }

        public static SQliteTestStruct GetRandom(int idx = 0)
        {
            if (idx <= 0) idx = GetRandomInt();

            byte[] testBytes = new SQliteTestStruct().ToBytes();

            return new SQliteTestStruct
            {
                Idx = idx,

                TestString = $"TestString{Random.value}{Random.value}{Random.value}{Random.value}",
                TestInt32 = GetRandomInt(),
                TestInt64 = GetRandomInt(),
                TestDouble = GetRandomSingle(),
                TestSingle = GetRandomSingle(),

                TestBytes = testBytes,

                TestInt32List = new List<int> { GetRandomInt(), GetRandomInt(), 2, 3, 4, 5, 6, 7, 8, 9, 0 },
                TestInt32Array = new int[] { GetRandomInt(), GetRandomInt(), 8, 7, 6, 5, 4, 3, 2, 1 }
            };
        }
        private static int GetRandomInt() => Random.Range(0, 9999);
        private static float GetRandomSingle() => Random.Range(0, 9999);

        public bool Equals(SQliteTestStruct other)
        {
            if (Idx != other.Idx || !TestString.Equals(other.TestString) ||

                TestInt32 != other.TestInt32 || TestInt64 != other.TestInt64 ||
                TestDouble != other.TestDouble || TestSingle != other.TestSingle ||

                TestBytes.Length != other.TestBytes.Length ||
                !TestBytes.SequenceEqual(other.TestBytes) ||

                TestInt32List.Count != other.TestInt32List.Count ||
                !TestInt32List.SequenceEqual(other.TestInt32List) ||
                TestInt32Array.Length != other.TestInt32Array.Length ||
                !TestInt32Array.SequenceEqual(other.TestInt32Array)) return false;

            return true;
        }
    }
    private SQLiteDatabase Init()
        => SQLiteDatabase.Initialize(
            Path.Combine(Application.dataPath, "testDatabase"), 
            "testDB", true);

    [Test]
    public void A1_InitializeDatabase()
    {
        string path = Path.Combine(Application.dataPath, "testDatabase");

        if (File.Exists(Path.Combine(path, "testDB") + ".db"))
        {
            File.Delete(Path.Combine(path, "testDB") + ".db");
            Debug.Log("deleted old testDB");
        }

        SQLiteDatabase.Initialize(path, "testDB", true);
    }
    [UnityTest]
    public IEnumerator B1_CreateTableWithStruct()
    {
        SQLiteDatabase db = Init();

        var job = db.CreateTable<SQliteTestStruct>("TestTable");

        yield return new WaitForBackgroundJob(job);

        Assert.IsTrue(db.HasTable("TestTable"), "TestTable Create Failed");
    }
    [UnityTest]
    public IEnumerator B2_CreateTableWithKeyValuePair()
    {
        SQLiteDatabase db = Init();

        List<KeyValuePair<System.Type, string>> columns = new List<KeyValuePair<System.Type, string>>
        {
            new KeyValuePair<System.Type, string>(typeof(int), "Idx"),
            new KeyValuePair<System.Type, string>(typeof(string), "TestString")
        };

        var job = db.CreateTable("TestTable2", columns);
        yield return new WaitForBackgroundJob(job);

        Assert.IsTrue(db.HasTable("TestTable2"), "TestTable Create Failed");
    }
    [UnityTest]
    public IEnumerator B3_CreateTableWithTable()
    {
        SQLiteDatabase db = Init();

        List<SQliteTestStruct> rows = new List<SQliteTestStruct>();
        for (int i = 1; i < 4; i++)
        {
            rows.Add(SQliteTestStruct.GetRandom(i));
        }

        var job = db.CreateTable(rows.ToSQLiteTable("TestTable3"));
        yield return new WaitForBackgroundJob(job);

        Assert.IsTrue(db.HasTable("TestTable3"), "TestTable Create Failed");
        SQLiteTable table = db.GetTable("TestTable3");
        Assert.IsTrue(table.Count == rows.Count, 
            $"TestTable Data Count is not equal {table.Count} : {rows.Count}");

        for (int i = 0; i < 3; i++)
        {
            Assert.IsTrue(table.ReadLine<SQliteTestStruct>(in i).Equals(rows[i]),
                "TestTable Data is not equal");
        }
    }
    [UnityTest]
    public IEnumerator B4_UpdateTableDataWithStruct()
    {
        SQLiteDatabase db = Init();

        int rndCount = 5;
        List<SQliteTestStruct> testList = new List<SQliteTestStruct>();
        for (int i = 1; i < rndCount + 1; i++)
        {
            testList.Add(SQliteTestStruct.GetRandom(i));
        }

        var job = db.UpdateTable(testList.ToSQLiteTable("TestTable"));
        yield return new WaitForBackgroundJob(job);

        SQLiteTable table = db.GetTable("TestTable");
        Assert.IsTrue(testList.Count == table.Count, $"Saved Data count is not equal {testList.Count} : {table.Count}");

        for (int i = 0; i < rndCount; i++)
        {
            Assert.IsTrue(testList[i].Equals(table.ReadLine<SQliteTestStruct>(i)),
                "Saved Data is not equal");
        }
    }
    [UnityTest]
    public IEnumerator B5_ReplaceTableDataWithStruct()
    {
        SQLiteDatabase db = Init();

        int rndCount = 5;
        List<SQliteTestStruct> testList = new List<SQliteTestStruct>();
        for (int i = 1; i < rndCount + 1; i++)
        {
            testList.Add(SQliteTestStruct.GetRandom(i));
        }

        var job = db.ReplaceTable(testList.ToSQLiteTable("TestTable"), "TestTable");
        yield return new WaitForBackgroundJob(job);

        SQLiteTable table = db.GetTable("TestTable");
        Assert.IsTrue(testList.Count == table.Count, $"Saved Data count is not equal {testList.Count} : {table.Count}");

        for (int i = 0; i < rndCount; i++)
        {
            Assert.IsTrue(testList[i].Equals(table.ReadLine<SQliteTestStruct>(i)),
                "Saved Data is not equal");
        }
    }
    [UnityTest]
    public IEnumerator C1_AddColumn()
    {
        SQLiteDatabase db = Init();

        var job = db.AddColumn("TestTable", typeof(string), "NewColumnString");
        yield return new WaitForBackgroundJob(job);

        SQLiteTable table = db.GetTable("TestTable");
        Assert.IsTrue(table.HasColumn("NewColumnString"), "Add Column Failed");
    }
    [UnityTest]
    public IEnumerator C2_DeleteColumn()
    {
        SQLiteDatabase db = Init();

        var job = db.DeleteColumn("TestTable", "NewColumnString");
        yield return new WaitForBackgroundJob(job);

        SQLiteTable table = db.GetTable("TestTable");
        Assert.IsFalse(table.HasColumn("NewColumnString"), "Delete Column Failed");
    }

    [UnityTest]
    public IEnumerator Z1_RenameTable()
    {
        SQLiteDatabase db = Init();

        var job = db.RenameTable("TestTable", "NewTestTable");
        yield return new WaitForBackgroundJob(job);

        Assert.IsFalse(db.HasTable("TestTable"), "Rename Table Failed 1");
        Assert.IsTrue(db.HasTable("NewTestTable"), "Rename Table Failed 2");
    }
    [UnityTest]
    public IEnumerator Z2_DropTable()
    {
        SQLiteDatabase db = Init();

        var job = db.DropTable("NewTestTable");
        yield return new WaitForBackgroundJob(job);

        Assert.IsFalse(db.HasTable("NewTestTable"), "Drop Table Failed");
    }
}
