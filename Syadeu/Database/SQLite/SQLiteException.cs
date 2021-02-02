using System;
using System.Reflection;

namespace Syadeu.Database
{
    public class SQLiteException : Exception
    {
        const string Msg = "SQLite Database Error: ";
        public SQLiteException(string msg) : base($"{Msg}{msg}")
        {
        }
    }
    /// <summary>
    /// 디버깅용 Exception 입니다
    /// </summary>
    public sealed class SQLiteAssertException : Exception
    {
        const string Msg = "SQLite Assert: ";
        public SQLiteAssertException(string msg) : base($"{Msg}{msg}")
        {
            //GameConsole.Log($"{Msg}{msg}", ConsoleFlag.Warnning);
        }
        public SQLiteAssertException(MethodInfo method, bool target, string msg = null)
            : base($"{Msg}" +
                  $"From {method.ReflectedType}.{method.Name}() != {!target}" +
                  $"{(string.IsNullOrEmpty(msg) ? "" : $"\n{msg}")}")
        {
            //GameConsole.Log($"{Msg}" +
            //      $"From {method.ReflectedType}.{method.Name}() != {!target}" +
            //      $"{(string.IsNullOrEmpty(msg) ? "" : $"\n{msg}")}", ConsoleFlag.Warnning);
        }
    }
    /// <summary>
    /// 데이터 테이블을 읽을 수 없을때 발생하는 에러입니다.
    /// </summary>
    /// <remarks>
    /// 연관 메소드<br/>
    /// <see cref="SQLiteDatabase.ReloadTable(string)"/><br/>
    /// <see cref="SQLiteDatabase.ReloadAllTables"/><br/>
    /// </remarks>
    public sealed class SQLiteUnreadableException : SQLiteException
    {
        public string TableName;
        public bool IsMasterTable;

        const string UnreadMsg = "The table({name}) cannot readable.";
        public SQLiteUnreadableException(string tableName, bool isMasterTable = false) :
            base(UnreadMsg.Replace("{name}", tableName))
        {
            TableName = tableName;
            IsMasterTable = isMasterTable;
        }
    }
    /// <summary>
    /// 데이터 파일을 쓰거나 재가공이 불가능하면 발생하는 에러입니다.
    /// </summary>
    /// <remarks>
    /// 연관 메소드<br/>
    /// <see cref="SQLiteDatabase.Excute"/>: 거의 대부분의 경우 이 메소드를 통해 호출됩니다.
    /// </remarks>
    /// 
    public sealed class SQLiteExcuteExcpetion : SQLiteException
    {
        const string ExcuteMsg = "An error occurred while excuting query.\n";
        public SQLiteExcuteExcpetion(string query, Exception ex) :
            base($"{ExcuteMsg}{ex.Message}:{ex.StackTrace}\nQuery: {query}")
        {
        }
    }
}