using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MailListView : MonoBehaviour
{
    public void ShowMailList(MailType type)
    {
        foreach (Transform item in this.transform)
        {
            GameObject.Destroy(item.gameObject);
        }
        ///先过滤数据，然后展示
        var list = MailModel.Instance.Mails.FindAll((mail) =>
            {
                return mail.mailType == type;
            });

        foreach (var mail in list)
        {
            var go = GameObject.Instantiate(Resources.Load<GameObject>("UI/Mail/MailItem"), this.transform);
            go.GetComponent<MailItem>().SetMailData(mail);
        }
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
