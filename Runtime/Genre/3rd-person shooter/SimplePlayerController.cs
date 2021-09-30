using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;

public partial class SimplePlayerController : MonoBehaviour
{
    [SerializeField] private Animator m_Animator;
    [SerializeField] private Transform m_Camera;

    [SerializeField] private string m_HorizontalKeyString = "Horizontal";
    [SerializeField] private string m_VerticalKeyString = "Vertical";
    [SerializeField] private string m_SpeedKeyString = "Speed";

    [SerializeField] private float m_Speed = 4;

    private int m_HorizontalKey;
    private int m_VerticalKey;
    private int m_SpeedKey;

    private void Awake()
    {
        if (m_Animator == null)
        {
            m_Animator = GetComponentInChildren<Animator>();
        }

        m_HorizontalKey = Animator.StringToHash(m_HorizontalKeyString);
        m_VerticalKey = Animator.StringToHash(m_VerticalKeyString);
        m_SpeedKey = Animator.StringToHash(m_SpeedKeyString);
    }

    private void Update()
    {
        if (m_Camera == null) return;

        Vector3 camForward = Vector3.ProjectOnPlane(m_Camera.transform.forward, Vector3.up);

        float
            currentSpeed = m_Animator.GetFloat(m_SpeedKey),
            currentX = m_Animator.GetFloat(m_HorizontalKey),
            currentZ = m_Animator.GetFloat(m_VerticalKey);

        float
            inputX = Input.GetAxis("Horizontal"),
            inputY = Input.GetAxis("Vertical");

        Vector3 
            movement = new Vector3(inputX, 0, inputY),
            norm = movement.normalized;

        quaternion rot = quaternion.LookRotation(camForward, new float3(0, 1, 0));
        float4x4 vp = new float4x4(new float3x3(rot), float3.zero);
        float3 point = math.mul(vp, new float4(norm, 1)).xyz;

        float
            horizontal = Vector3.Dot(point, transform.right),
            vertical = Vector3.Dot(point, transform.forward);

        float
            speed = Mathf.Lerp(currentSpeed, vertical < 0 ? -norm.magnitude : norm.magnitude, m_Speed * Time.deltaTime),

            x = Mathf.Lerp(currentX, horizontal, m_Speed * Time.deltaTime),
            z = Mathf.Lerp(currentZ, vertical, m_Speed * Time.deltaTime);

        m_Animator.SetFloat(m_SpeedKey, speed);
        m_Animator.SetFloat(m_HorizontalKey, x);
        m_Animator.SetFloat(m_VerticalKey, z);
    }
}
