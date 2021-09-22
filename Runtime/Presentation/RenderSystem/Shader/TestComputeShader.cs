using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TestComputeShader : MonoBehaviour
{
    //public int sphereAmount = 17;
    //public ComputeShader ComputeShader;

    //ComputeBuffer resultBuffer;
    //int kernel;
    //uint threadGroupSize;
    //Vector3[] output;
    //Transform[] transforms;

    //private void Start()
    //{
    //    kernel = ComputeShader.FindKernel("CSMain");
    //    ComputeShader.GetKernelThreadGroupSizes(kernel, out threadGroupSize, out _, out _);

    //    resultBuffer = new ComputeBuffer(sphereAmount, sizeof(float) * 3);
    //    output = new Vector3[sphereAmount];
    //    transforms = new Transform[sphereAmount];

    //    for (int i = 0; i < sphereAmount; i++)
    //    {
    //        transforms[i] = GameObject.CreatePrimitive(PrimitiveType.Sphere).transform;
    //    }
    //}
    //private void OnDestroy()
    //{
    //    resultBuffer.Dispose();
    //}

    //private void Update()
    //{
    //    ComputeShader.SetBuffer(kernel, "Result", resultBuffer);
    //    int threadGroups = (int)((sphereAmount + (threadGroupSize - 1)) / threadGroupSize);
    //    ComputeShader.Dispatch(kernel, threadGroups, 1, 1);
    //    resultBuffer.GetData(output);

    //    for (int i = 0; i < sphereAmount; i++)
    //    {
    //        transforms[i].position = output[i];
    //    }
    //}
}
