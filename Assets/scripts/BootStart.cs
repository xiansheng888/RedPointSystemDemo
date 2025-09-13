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
        if (GUILayout.Button("�����ʼ�����"))
        {
            MailModel.Instance.AddMail(MailModel.Instance.CreateMailData("ϵͳ�ʼ�", "��ӭʹ�ñ��ʼ�ϵͳ", (MailType)Random.Range(0, 3)));
        }
    }
}
