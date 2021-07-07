using Syadeu.Database;
using Syadeu.Internal;
using Syadeu.Presentation.Entities;
using System;
using System.Collections.Generic;
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
        private readonly Dictionary<Type, RuntimeType> m_BakedTypes = new Dictionary<Type, RuntimeType>();

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
            GetOrGenericTypeObject<float>();
            GetOrGenericTypeObject<double>();
            GetOrGenericTypeObject<string>();
            GetOrGenericTypeObject<string>();
            GetOrGenericTypeObject<Component>();
            GetOrGenericTypeObject<MonoBehaviour>();
            GetOrGenericTypeObject<ItemDataList>();
            "in".ToLog();
            return base.OnStartPresentation();
        }

        /// <summary>
        /// 사용한 오브젝트를 재사용할 수 있도록 해당 오브젝트 풀로 반환합니다.<br/>
        /// 해당 타입이 <seealso cref="ITerminate"/>를 참조하면 <seealso cref="ITerminate.Terminate"/>를 호출합니다.
        /// </summary>
        public void ReturnGenericTypeObject<T>(MonoBehaviour<T> obj)
        {
            if (!m_BakedTypes.TryGetValue(TypeHelper.TypeOf<T>.Type, out RuntimeType runtimeType))
            {
                runtimeType = new RuntimeType
                {
                    TargetType = TypeHelper.TypeOf<T>.Type,
                    BakedType = obj.GetType()
                };
                m_BakedTypes.Add(TypeHelper.TypeOf<T>.Type, runtimeType);
            }
            if (obj is ITerminate terminate) terminate.Terminate();
            runtimeType.ObjectPool.Enqueue(obj);
        }
        public MonoBehaviour<T> GetOrGenericTypeObject<T>()
        {
            if (!m_BakedTypes.TryGetValue(TypeHelper.TypeOf<T>.Type, out RuntimeType runtimeType) ||
                runtimeType.ObjectPool.Count == 0)
            {
                return CreateGenericTypeObject<T>();
            }
            return runtimeType.ObjectPool.Dequeue() as MonoBehaviour<T>;
        }

        #region Runtime Generic MonoBehaviour Maker
        private class RuntimeType
        {
            public Type TargetType;
            public Type BakedType;

            public int CreatedCount = 0;
            public Queue<MonoBehaviour> ObjectPool = new Queue<MonoBehaviour>();
        }
        /// <summary>
        /// 런타임에서 요구하는 <typeparamref name="T"/>의 값의 <see cref="MonoBehaviour"/> 타입을 만들어 반환합니다.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private Type MakeGenericTypeMonobehaviour<T>()
        {
            const string newName = "{0}Proxy";

            Type testMono = typeof(MonoBehaviour<>).MakeGenericType(TypeHelper.TypeOf<T>.Type);
            TypeBuilder tb = m_ModuleBuilder.DefineType(
                string.Format(newName, TypeHelper.TypeOf<T>.Name), TypeAttributes.Public, testMono);

            return tb.CreateType();
        }
        private MonoBehaviour<T> CreateGenericTypeObject<T>()
        {
            GameObject obj = new GameObject(TypeHelper.TypeOf<T>.Name);
            if (!m_BakedTypes.TryGetValue(TypeHelper.TypeOf<T>.Type, out RuntimeType runtimeType))
            {
                Type baked = MakeGenericTypeMonobehaviour<T>();
                runtimeType = new RuntimeType
                {
                    TargetType = TypeHelper.TypeOf<T>.Type,
                    BakedType = baked
                };
                m_BakedTypes.Add(TypeHelper.TypeOf<T>.Type, runtimeType);
            }
            obj.name += runtimeType.CreatedCount;
            runtimeType.CreatedCount++;
            return obj.AddComponent(runtimeType.BakedType) as MonoBehaviour<T>;
        }
        #endregion
    }

    public abstract class MonoBehaviour<T> : MonoBehaviour
    {
        [SerializeReference] public T m_Value;

        private void Start()
        {
            "TestMono DONE".ToLog();
        }
    }
}
