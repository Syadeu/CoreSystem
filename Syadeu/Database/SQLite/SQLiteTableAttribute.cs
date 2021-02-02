using System;

namespace Syadeu.Database
{
    /// <summary>
    /// <para>이 구조체를 <see cref="SQLiteDatabase"/> 용 데이터 테이블로 선언합니다.</para>
    /// <see cref="SQLiteTable"/>의 열 정보를 이 구조체로 받아오려면,
    /// 동일한 이름을 가진 필드 혹은 프로퍼티 맴버가 존재해야되며,<br/>
    /// 그 맴버는 반드시 <see cref="SQLiteDatabaseAttribute"/>를 상속받아야됩니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Struct)]
    public sealed class SQLiteTableAttribute : Attribute
    {
        public bool IsDefault = false;
        public SQLiteTableAttribute(bool isDefault = false)
        {
            IsDefault = isDefault;
        }
    }
}