using System.Collections.Generic;
using UnityEngine;

public class RedPointNode
{
    public string nodeName;//节点名称
    public int pointNum = 0;//红点数量
    public RedPointNode parent = null;//父节点
    public event RedPointSystem.OnPointNumChange numChangeFunc;//发生变化的回调

    //子节点
    public Dictionary<string, RedPointNode> dicChilds = new Dictionary<string, RedPointNode>();

    public void SetRedPointNum(int rpNum)
    {
        if (dicChilds.Count > 0)//红点树数量只能设置叶子节点
        {
            Debug.LogError("只能设置叶子节点");
            return;
        }
        pointNum += rpNum;

        NotifyPointNumChange();
        if (parent != null)
        {
            parent.ChangePredPointNum();
        }

    }

    //计算当前红点数量
    public void ChangePredPointNum()
    {
        int num = 0;
        foreach (var node in dicChilds.Values)
        {
            num += node.pointNum;
        }
        if (num != pointNum)//红点有变化
        {
            pointNum = num;
            NotifyPointNumChange();
        }
    }

    //通知红点数量变化
    public void NotifyPointNumChange()
    {
        numChangeFunc?.Invoke(this);
    }
}
