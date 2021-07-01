using NUnit.Framework;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using UnityEngine;

public class SketchbookTests
{
    public class SomeAttribute : Attribute
    {
        public SomeAttribute(string value)
        {
            this.Value = value;
        }

        public string Value { get; set; }
    }

    public class SomeClass
    {
        public string Value = "Test";
    }

    [Test]
    public void CanAddAttribute()
    {
        Type type = typeof(SomeClass);

        AssemblyName aName = new AssemblyName("SomeNamespace");
        AssemblyBuilder ab = AppDomain.CurrentDomain.DefineDynamicAssembly(aName, AssemblyBuilderAccess.Run);
        ModuleBuilder mb = ab.DefineDynamicModule(aName.Name);
        TypeBuilder tb = mb.DefineType(type.Name + "Proxy", TypeAttributes.Public, type);

        Type[] attrCtorParams = new Type[] { typeof(string) };
        ConstructorInfo attrCtorInfo = typeof(SomeAttribute).GetConstructor(attrCtorParams);
        CustomAttributeBuilder attrBuilder = new CustomAttributeBuilder(attrCtorInfo, new object[] { "Some Value" });
        tb.SetCustomAttribute(attrBuilder);

        Type newType = tb.CreateType();
        SomeClass instance = (SomeClass)Activator.CreateInstance(newType);

        Assert.AreEqual("Test", instance.Value);
        SomeAttribute attr = (SomeAttribute)instance.GetType()
            .GetCustomAttributes(typeof(SomeAttribute), false)
            .SingleOrDefault();
        Assert.IsNotNull(attr);
        Assert.AreEqual(attr.Value, "Some Value");

        Debug.Log($"{newType.Name} == {type.Name} = {newType.Equals(type)}");
    }
}
