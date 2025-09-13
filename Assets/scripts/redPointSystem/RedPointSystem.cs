using System.Collections.Generic;
using UnityEngine;

public class RedPointSystem
{
    public delegate void OnPointNumChange(RedPointNode node);//���仯֪ͨ
    RedPointNode mRootNode;//�����Root�ڵ�

    static List<string> lstRedPointTreeList = new List<string>//��ʼ�������
    {
        RedPointConst.main,
        RedPointConst.mail,
        RedPointConst.mailSystem,
        RedPointConst.mailTeam,
        RedPointConst.mailAlliance,
        RedPointConst.task,
        RedPointConst.alliance,

    };

    #region ����ģʽ
    private static RedPointSystem _instance;
    public static RedPointSystem Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new RedPointSystem();
            }
            return _instance;
        }
    }
    private RedPointSystem() { }
    #endregion

    public void InitRedPointTreeNode()
    {
        mRootNode = new RedPointNode();
        mRootNode.nodeName = RedPointConst.main;

        foreach (var s in lstRedPointTreeList)
        {
            var node = mRootNode;
            var treeNodeAy = s.Split('.');
            if (treeNodeAy[0] != mRootNode.nodeName)
            {
                Debug.Log("��������ڵ����:" + treeNodeAy[0]);
                continue;
            }
            if (treeNodeAy.Length > 1)
            {
                for (int i = 1; i < treeNodeAy.Length; i++)
                {
                    if (!node.dicChilds.ContainsKey(treeNodeAy[i]))
                    {
                        node.dicChilds.Add(treeNodeAy[i], new RedPointNode());
                    }
                    node.dicChilds[treeNodeAy[i]].nodeName = treeNodeAy[i];
                    node.dicChilds[treeNodeAy[i]].parent = node;

                    node = node.dicChilds[treeNodeAy[i]];
                }
            }
        }


    }
    //���������һ���¼��ص�
    public void SetRedPointNodeCallBack(string strNode, RedPointSystem.OnPointNumChange callBack)
    {
        var nodeList = strNode.Split('.');//�������ڵ�
        if (nodeList.Length == 1)
        {
            if (nodeList[0] != RedPointConst.main)
            {
                Debug.Log("��ȡ����ĸ��ڵ㣡��ǰ��" + nodeList[0]);
                return;
            }
        }

        var node = mRootNode;
        for (int i = 1; i < nodeList.Length; i++)
        {
            if (!node.dicChilds.ContainsKey(nodeList[i]))
            {
                Debug.Log("�������ӽڵ㣺" + nodeList[i]);
                return;
            }
            node = node.dicChilds[nodeList[i]];

            if (i == nodeList.Length - 1)//���һ���ڵ���
            {
                node.numChangeFunc = callBack;
                return;
            }
        }

    }

    public void SetInvoke(string strNode, int rpNum)
    {
        var nodeList = strNode.Split('.');//�������ڵ�
        if (nodeList.Length == 1)
        {
            if (nodeList[0] != RedPointConst.main)
            {
                Debug.Log("��ȡ����ĸ��ڵ㣡��ǰ��" + nodeList[0]);
                return;
            }
        }

        var node = mRootNode;
        for (int i = 1; i < nodeList.Length; i++)
        {
            if (!node.dicChilds.ContainsKey(nodeList[i]))
            {
                Debug.Log("�������ӽڵ㣺" + nodeList[i]);
                return;
            }
            node = node.dicChilds[nodeList[i]];

            if (i == nodeList.Length - 1)//���һ���ڵ���
            {
                node.SetRedPointNum(rpNum);//���ýڵ�ĺ������
            }
        }
    }

    public bool GetNodeRedPoint(string strNode, out RedPointNode redPointNode)
    {
        var nodeList = strNode.Split('.');//�������ڵ�
        if (nodeList.Length == 1)
        {
            if (nodeList[0] != RedPointConst.main)
            {
                Debug.Log("��ȡ����ĸ��ڵ㣡��ǰ��" + nodeList[0]);
                redPointNode = null;
                return false;
            }
        }
        var node = mRootNode;
        for (int i = 1; i < nodeList.Length; i++)
        {
            if (!node.dicChilds.ContainsKey(nodeList[i]))
            {
                Debug.Log("�������ӽڵ㣺" + nodeList[i]);
                redPointNode = null;
                return false;
            }
            node = node.dicChilds[nodeList[i]];
            if (i == nodeList.Length - 1)//���һ���ڵ���
            {
                redPointNode = node;
                return node.pointNum > 0;//���ýڵ�ĺ������
            }
        }
        redPointNode = null;
        return false;
    }
}
