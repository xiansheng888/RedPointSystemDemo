using UnityEditor;
using UnityEngine;

public class BootStart : MonoBehaviour
{
    public void Awake()
    {
        RedPointSystem.Instance.InitRedPointTreeNode();
        MailModel.Instance.init();

    }


    private void OnGUI()
    {
        if (GUILayout.Button("插入邮件数据"))
        {
            MailModel.Instance.AddMail(MailModel.Instance.CreateMailData("系统邮件", "欢迎使用本邮件系统", (MailType)Random.Range(0, 3)));
        }
    }
}
