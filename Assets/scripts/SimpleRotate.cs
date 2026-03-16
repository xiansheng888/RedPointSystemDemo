using UnityEngine;

/// <summary>
/// 简单的旋转脚本
/// </summary>
public class SimpleRotate : MonoBehaviour
{
    /// <summary>
    /// 旋转速度
    /// </summary>
    public float speed = 50f;
    
    /// <summary>
    /// 旋转轴
    /// </summary>
    public Vector3 axis = Vector3.up;
    
    void Update()
    {
        // 每帧旋转
        transform.Rotate(axis, speed * Time.deltaTime);
    }
}