using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 红点触发任务类
/// </summary>
public class RedPointUpdateTask
{
    public string redPointName;
    public RedPointNode node;
    public int uddateNum;

}
public class RedPointSystem
{
    public delegate void OnPointNumChange(RedPointNode node);//红点变化通知
    RedPointNode mRootNode;//红点树Root节点

    private Queue<RedPointUpdateTask> mUpdateQueue = new Queue<RedPointUpdateTask>();//更新队列
    private Dictionary<string, RedPointUpdateTask> updateMap = new Dictionary<string, RedPointUpdateTask>();


    static List<string> lstRedPointTreeList = new List<string>//初始化红点树
    {
        RedPointConst.main,
        RedPointConst.mail,
        RedPointConst.mailSystem,
        RedPointConst.mailTeam,
        RedPointConst.mailAlliance,
        RedPointConst.task,
        RedPointConst.alliance,

    };

    #region 单例模式
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
                Debug.Log("红点树根节点错误:" + treeNodeAy[0]);
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
        startCheckUpdate().Coroutine();
    }

    private async Task startCheckUpdate()
    {
        while (true)
        {
            await new WaitForSeconds(0.1f);
            if (mUpdateQueue.Count > 0)
            {
                RedPointUpdateTask tmp = mUpdateQueue.Dequeue();
                updateMap.Remove(tmp.redPointName);
                tmp.node.SetRedPointNum(tmp.uddateNum);
            }

        }

    }

    //给这棵树绑定一个事件回调
    public void SetRedPointNodeCallBack(string strNode, RedPointSystem.OnPointNumChange callBack)
    {
        var nodeList = strNode.Split('.');//分析树节点
        if (nodeList.Length == 1)
        {
            if (nodeList[0] != RedPointConst.main)
            {
                Debug.Log("获取错误的根节点！当前是" + nodeList[0]);
                return;
            }
        }

        var node = mRootNode;
        for (int i = 1; i < nodeList.Length; i++)
        {
            if (!node.dicChilds.ContainsKey(nodeList[i]))
            {
                Debug.Log("不包含子节点：" + nodeList[i]);
                return;
            }
            node = node.dicChilds[nodeList[i]];

            if (i == nodeList.Length - 1)//最后一个节点了
            {
                node.numChangeFunc += callBack;
                return;
            }
        }

    }

    public void SetInvoke(string strNode, int rpNum)
    {
        ///1.把节点找到，和节点名和数量封装成一个task，放入队列，待更新。
        if (updateMap.TryGetValue(strNode, out RedPointUpdateTask task))
        {
            task.uddateNum += rpNum;
        }
        else
        {
            RedPointNode tmpNode = this.GetNodeByName(strNode);
            var tmpTask = new RedPointUpdateTask() { redPointName = strNode, node = tmpNode, uddateNum = rpNum };
            updateMap.Add(strNode, tmpTask);
            mUpdateQueue.Enqueue(tmpTask);
        }

        //var nodeList = strNode.Split('.');//分析树节点
        //if (nodeList.Length == 1)
        //{
        //    if (nodeList[0] != RedPointConst.main)
        //    {
        //        Debug.Log("获取错误的根节点！当前是" + nodeList[0]);
        //        return;
        //    }
        //}

        //var node = mRootNode;
        //for (int i = 1; i < nodeList.Length; i++)
        //{
        //    if (!node.dicChilds.ContainsKey(nodeList[i]))
        //    {
        //        Debug.Log("不包含子节点：" + nodeList[i]);
        //        return;
        //    }
        //    node = node.dicChilds[nodeList[i]];

        //    if (i == nodeList.Length - 1)//最后一个节点了
        //    {
        //        node.SetRedPointNum(rpNum);//设置节点的红点数量
        //    }
        //}
    }


    private RedPointNode GetNodeByName(string strNode)
    {
        var nodeList = strNode.Split('.');//分析树节点
        if (nodeList.Length == 1)
        {
            if (nodeList[0] != RedPointConst.main)
            {
                Debug.Log("获取错误的根节点！当前是" + nodeList[0]);
                return null;
            }
        }
        var node = mRootNode;
        for (int i = 1; i < nodeList.Length; i++)
        {
            if (!node.dicChilds.ContainsKey(nodeList[i]))
            {
                Debug.Log("不包含子节点：" + nodeList[i]);
                return null;
            }
            node = node.dicChilds[nodeList[i]];
            if (i == nodeList.Length - 1)//最后一个节点了
            {
                return node;
            }
        }
        return null;
    }
    public bool GetNodeRedPoint(string strNode, out RedPointNode redPointNode)
    {
        var nodeList = strNode.Split('.');//分析树节点
        if (nodeList.Length == 1)
        {
            if (nodeList[0] != RedPointConst.main)
            {
                Debug.Log("获取错误的根节点！当前是" + nodeList[0]);
                redPointNode = null;
                return false;
            }
        }
        var node = mRootNode;
        for (int i = 1; i < nodeList.Length; i++)
        {
            if (!node.dicChilds.ContainsKey(nodeList[i]))
            {
                Debug.Log("不包含子节点：" + nodeList[i]);
                redPointNode = null;
                return false;
            }
            node = node.dicChilds[nodeList[i]];
            if (i == nodeList.Length - 1)//最后一个节点了
            {
                redPointNode = node;
                return node.pointNum > 0;//设置节点的红点数量
            }
        }
        redPointNode = null;
        return false;
    }
}
