using QFramework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MailItem : MonoBehaviour
{
    public Text subjectTxt;
    public GameObject redPoint;
    private MailData mailData;
    public void SetMailData(MailData mail)
    {
        subjectTxt.text = mail.id + "--" + mail.subject;
        redPoint.SetActive(!mail.isRead);
        this.mailData = mail;
    }

    private void OnEnable()
    {
        UIEventListener.Get(this.gameObject).onClick += () =>
        {
          ///  MailDetailView.Instance.ShowMailDetail(this);
            //标记为已读

            if (mailData != null && !mailData.isRead)
            {
                mailData.isRead = true;
                redPoint.SetActive(false);
                //更新红点
                switch (mailData.mailType)
                {
                    case MailType.System:
                        RedPointSystem.Instance.SetInvoke(RedPointConst.mailSystem, -1);
                        break;
                    case MailType.Team:
                        RedPointSystem.Instance.SetInvoke(RedPointConst.mailTeam, -1);
                        break;
                    case MailType.Alliance:
                        RedPointSystem.Instance.SetInvoke(RedPointConst.mailAlliance, -1);
                        break;
                }
            }
        };
    }
    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
