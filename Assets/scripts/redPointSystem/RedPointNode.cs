using System.Collections.Generic;
using UnityEngine;

public class RedPointNode
{
    public string nodeName;//�ڵ�����
    public int pointNum = 0;//�������
    public RedPointNode parent = null;//���ڵ�
    public event RedPointSystem.OnPointNumChange numChangeFunc;//�����仯�Ļص�

    //�ӽڵ�
    public Dictionary<string, RedPointNode> dicChilds = new Dictionary<string, RedPointNode>();

    public void SetRedPointNum(int rpNum)
    {
        if (dicChilds.Count > 0)//���������ֻ������Ҷ�ӽڵ�
        {
            Debug.LogError("ֻ������Ҷ�ӽڵ�");
            return;
        }
        pointNum += rpNum;

        NotifyPointNumChange();
        if (parent != null)
        {
            parent.ChangePredPointNum();
        }

    }

    //���㵱ǰ�������
    public void ChangePredPointNum()
    {
        int num = 0;
        foreach (var node in dicChilds.Values)
        {
            num += node.pointNum;
        }
        if (num != pointNum)//����б仯
        {
            pointNum = num;
            NotifyPointNumChange();
        }
    }

    //֪ͨ��������仯
    public void NotifyPointNumChange()
    {
        numChangeFunc?.Invoke(this);
    }
}
