using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

using Syadeu;
using Syadeu.Mono;

public class PrefabManagerTests
{
    [Test]
    public void AddNewPrefabTest()
    {
        GameObject obj = new GameObject("test");
        int idx = PrefabManager.AddRecycleObject(new PrefabList.ObjectSetting()
        {
            m_Name = "test",
            Prefab = obj
        });

        RecycleableMonobehaviour testObj = PrefabManager.GetRecycleObject(idx);
        Debug.Log($"{testObj.name} has created and returned");
    }
    [Test]
    public void GetRecycleObjectTest()
    {
        List<RecycleableMonobehaviour> tempList = new List<RecycleableMonobehaviour>();
        for (int i = 0; i < 100; i++)
        {
            tempList.Add(PrefabManager.GetRecycleObject(0));
        }

        Object.Destroy(PrefabManager.Instance.gameObject);
    }
    [UnityTest]
    public IEnumerator DestoryBackgroundRecycleObjectTest()
    {
        List<RecycleableMonobehaviour> tempList = new List<RecycleableMonobehaviour>();
        var tempJob = CoreSystem.AddBackgroundJob(() =>
        {
            for (int i = 0; i < 100; i++)
            {
                var obj = PrefabManager.GetRecycleObject(0);
                Assert.IsTrue(obj != null);
                tempList.Add(obj);
            }
        });
        yield return new WaitForBackgroundJob(tempJob);

        Debug.Log($"0. init check {PrefabManager.Initialized}");
        Assert.IsTrue(PrefabManager.Initialized);

        yield return null;

        Object.Destroy(PrefabManager.Instance.gameObject);

        yield return null;

        Debug.Log($"1. init check {PrefabManager.Initialized}");
        Assert.IsFalse(PrefabManager.Initialized);

        yield return null;

        Debug.Log($"2. {PrefabManager.Instance.GetInstanceCount(0)}개");
        Assert.IsTrue(PrefabManager.Instance.GetInstanceCount(0) == 0);

        yield return null;
        Object.Destroy(PrefabManager.Instance.gameObject);
    }
    [UnityTest]
    public IEnumerator DestoryForegroundRecycleObjectTest()
    {
        List<RecycleableMonobehaviour> tempList = new List<RecycleableMonobehaviour>();
        for (int i = 0; i < 100; i++)
        {
            var obj = PrefabManager.GetRecycleObject(0);
            Assert.IsTrue(obj != null);
            tempList.Add(obj);
        }

        Debug.Log($"0. init check{PrefabManager.Initialized}");
        Assert.IsTrue(PrefabManager.Initialized);

        yield return null;

        Object.Destroy(PrefabManager.Instance.gameObject);

        yield return null;

        Debug.Log($"1. init check {PrefabManager.Initialized}");
        Assert.IsFalse(PrefabManager.Initialized);

        yield return null;

        Debug.Log($"2. {PrefabManager.Instance.GetInstanceCount(0)}개");
        Assert.IsTrue(PrefabManager.Instance.GetInstanceCount(0) == 0);

        yield return null;
        Object.Destroy(PrefabManager.Instance.gameObject);
    }
    [UnityTest]
    public IEnumerator NullRecycleObjectTest()
    {
        List<RecycleableMonobehaviour> tempList = new List<RecycleableMonobehaviour>();
        for (int i = 0; i < 100; i++)
        {
            var obj = PrefabManager.GetRecycleObject(0);
            Assert.IsTrue(obj != null);
            tempList.Add(obj);
        }

        yield return null;

        var temp = PrefabManager.GetRecycleObject(0);

        Assert.IsTrue(temp == null, $"null test failed: {temp}");

        Object.Destroy(PrefabManager.Instance.gameObject);
    }
}
