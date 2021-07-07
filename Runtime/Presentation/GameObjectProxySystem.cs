using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

namespace Syadeu.Presentation
{
    public sealed class GameObjectProxySystem : PresentationSystemEntity<GameObjectProxySystem>
    {
        public override bool EnableBeforePresentation => true;
        public override bool EnableOnPresentation => true;
        public override bool EnableAfterPresentation => true;

        private ModuleBuilder m_ModuleBuilder;

        public override PresentationResult OnInitialize()
        {
            return base.OnInitialize();
        }
        public override PresentationResult OnInitializeAsync()
        {
            AssemblyName aName = new AssemblyName("CoreSystem_Runtime");
            AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
            m_ModuleBuilder = ab.DefineDynamicModule(aName.Name);

            return base.OnInitializeAsync();
        }

        public override PresentationResult OnStartPresentation()
        {
            GameObject obj = new GameObject("TestTESTMono");
            obj.AddComponent(MakeGenericTypeMonobehaviour<float>());
            "in".ToLog();
            return base.OnStartPresentation();
        }

        /// <summary>
        /// 런타임에서 요구하는 <typeparamref name="T"/>의 값의 <see cref="MonoBehaviour"/> 타입을 만들어 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private Type MakeGenericTypeMonobehaviour<T>()
        {
            Type testMono = typeof(Monobehaviour<>).MakeGenericType(TypeHelper.TypeOf<T>.Type);
            TypeBuilder tb = m_ModuleBuilder.DefineType(TypeHelper.TypeOf<T>.Name + "Proxy", TypeAttributes.Public, testMono);

            return tb.CreateType();
        }
    }

    public abstract class Monobehaviour<T> : MonoBehaviour
    {
        public T m_Value;

        private void Awake()
        {
            "TestMono DONE".ToLog();
        }
    }
}
