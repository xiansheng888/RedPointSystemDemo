using UnityEngine;
using UnityEngine.UI;

public class MailView : MonoBehaviour
{
    public GameObject systemMailBtn;
    public GameObject allianceMailBtn;
    public GameObject teamMailBtn;
    public GameObject mailList;

    private void Awake()
    {
        Debug.Log("MailView aWAKE");
        initShow();
    }

    private void initShow()
    {
        checkRedPoint();
        systemMailBtn.GetComponentInChildren<Button>().onClick.AddListener(() =>
        {
            mailList.GetComponent<MailListView>().ShowMailList(MailType.System);
        });

        allianceMailBtn.GetComponentInChildren<Button>().onClick.AddListener(() =>
        {
            mailList.GetComponent<MailListView>().ShowMailList(MailType.Alliance);
        });

        teamMailBtn.GetComponentInChildren<Button>().onClick.AddListener(() =>
        {
            mailList.GetComponent<MailListView>().ShowMailList(MailType.Team);
        });


    }

    private void checkRedPoint()
    {
        RedPointNode node;
        if (RedPointSystem.Instance.GetNodeRedPoint(RedPointConst.mailSystem, out node))
        {
            if (node.pointNum > 0)
            {
                systemMailBtn.transform.Find("redPoint").gameObject.SetActive(true);
                systemMailBtn.transform.Find("redPoint").GetComponentInChildren<Text>().text = node.pointNum.ToString();
            }
            else
            {
                systemMailBtn.transform.Find("redPoint").gameObject.SetActive(false);
            }
        }

        if (RedPointSystem.Instance.GetNodeRedPoint(RedPointConst.mailTeam, out node))
        {
            if (node.pointNum > 0)
            {
                teamMailBtn.transform.Find("redPoint").gameObject.SetActive(true);
                teamMailBtn.transform.Find("redPoint").GetComponentInChildren<Text>().text = node.pointNum.ToString();
            }
            else
            {
                teamMailBtn.transform.Find("redPoint").gameObject.SetActive(false);
            }
        }

        if (RedPointSystem.Instance.GetNodeRedPoint(RedPointConst.mailAlliance, out node))
        {
            if (node.pointNum > 0)
            {
                allianceMailBtn.transform.Find("redPoint").gameObject.SetActive(true);
                allianceMailBtn.transform.Find("redPoint").GetComponentInChildren<Text>().text = node.pointNum.ToString();
            }
            else
            {
                allianceMailBtn.transform.Find("redPoint").gameObject.SetActive(false);
            }
        }
    }

    private void OnEnable()
    {

        Debug.Log("onMailViewEnable");
        RedPointSystem.Instance.SetRedPointNodeCallBack(RedPointConst.mailSystem, (node) =>
        {
            Debug.Log("系统邮件:" + node.pointNum);
            if (node.pointNum <= 0)
            {
                systemMailBtn.transform.Find("redPoint").gameObject.SetActive(false);
                return;
            }
            systemMailBtn.transform.Find("redPoint").gameObject.SetActive(true);
            systemMailBtn.transform.Find("redPoint").GetComponentInChildren<Text>().text = node.pointNum.ToString();
        });
        RedPointSystem.Instance.SetRedPointNodeCallBack(RedPointConst.mailAlliance, (node) =>
        {
            Debug.Log("工会邮件:" + node.pointNum);
            if (node.pointNum <= 0)
            {
                allianceMailBtn.transform.Find("redPoint").gameObject.SetActive(false);
                return;
            }
            allianceMailBtn.transform.Find("redPoint").gameObject.SetActive(true);
            allianceMailBtn.transform.Find("redPoint").GetComponentInChildren<Text>().text = node.pointNum.ToString();
        });
        RedPointSystem.Instance.SetRedPointNodeCallBack(RedPointConst.mailTeam, (node) =>
        {
            Debug.Log("组队:" + node.pointNum);
            if (node.pointNum <= 0)
            {
                teamMailBtn.transform.Find("redPoint").gameObject.SetActive(false);
                return;
            }
            teamMailBtn.transform.Find("redPoint").gameObject.SetActive(true);
            teamMailBtn.transform.Find("redPoint").GetComponentInChildren<Text>().text = node.pointNum.ToString();
        });
    }

    private void Start()
    {
        Debug.Log("MailView Start");
    }
}
