using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Syadeu.Database
{
    public interface IValidation
    {
        /// <summary>
        /// 이 객체(혹은 구조체)가 유효한지 반환합니다.
        /// </summary>
        /// <returns></returns>
        bool IsValid();
    }
}
