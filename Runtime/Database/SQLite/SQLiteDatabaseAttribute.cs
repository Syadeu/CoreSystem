using System;

namespace Syadeu.Collections
{
    /// <summary>
    /// <para>이 맴버를 <see cref="SQLiteDatabase"/> 용 변수로 선언합니다.</para>
    /// 이 변수의 부모 구조체는 반드시 <see cref="SQLiteTableAttribute"/>를 상속받아야되며,<br/>
    /// 무조건 단 하나의 <c>IsPrimaryKey = true</c>인 맴버가 존재해야됩니다.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class SQLiteDatabaseAttribute : Attribute
    {
        /// <summary>
        /// 데이터의 메인 키값임을 결정합니다
        /// </summary>
        public bool IsPrimaryKey = false;
        /// <summary>
        /// 데이터 컬럼에서 순서를 결정합니다
        /// </summary>
        public int Order = -1;
        /// <summary>
        /// 데이터 생성시 기본값을 설정합니다. null이면 기본값 지정안함<br/>
        /// 만약 리스트나 어레이 형식일때 <see cref="int"/>로 값을 지정하면 해당 값은 배열의 기본 길이 값이 됩니다
        /// </summary>
        public object DefaultValue = null;

        public SQLiteDatabaseAttribute(bool isPrimaryKey = false)
        {
            IsPrimaryKey = isPrimaryKey;
        }
    }
}