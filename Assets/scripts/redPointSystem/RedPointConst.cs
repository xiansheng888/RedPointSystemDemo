using System.Collections;
using System.Collections.Generic;
using UnityEngine;
/// <summary>
/// 定一个const类，按照系统来划分树的各个节点，
/// 比如：上面展示的红点树结构，代码可以使用string来完成。
/// 这个定义不仅仅是用来规划结构，也会为后面事件驱动的时候提供key。
/// </summary>
public class RedPointConst
{
    public const string main = "Main";//主界面
    public const string mail = "Main.Mail";//主界面邮件按钮
    public const string mailSystem = "Main.Mail.System";//邮件系统界面
    public const string mailTeam = "Main.Mail.Team";//邮件队伍界面
    public const string mailAlliance = "Main.Mail.Alliance";//邮件工会界面

    public const string task = "Main.Task";//主界面任务按钮

    public const string alliance = "Main.Alliacne";//主界面工会按钮
}
