using QFramework;
using UnityEngine;
using UnityEngine.UI;

public class MainView : MonoBehaviour
{
    public GameObject mailBtn;
    public GameObject bagBtn;
    public GameObject mailViewGo;

    private void Awake()
    {
        mailViewGo.SetActive(false);
        ///ui��ʱ����ʼ��ѯ���״̬
        if (RedPointSystem.Instance.GetNodeRedPoint(RedPointConst.mail, out RedPointNode node))
        {
            mailBtn.transform.Find("redPoint").gameObject.SetActive(true);
            mailBtn.transform.Find("redPoint").GetComponentInChildren<Text>().text = node.pointNum.ToString();
        }
    }

    private void OnEnable()
    {
        Image img = mailBtn.transform.Find("Image").GetComponent<Image>();
        UIEventListener.Get(img.gameObject).onClick += MainView_onClick;

        Debug.Log("onMainViewEnable");
        RedPointSystem.Instance.SetRedPointNodeCallBack(RedPointConst.mail, (node) =>
        {
            Debug.Log("�������ʼ����仯:" + node.pointNum);
            if (node.pointNum <= 0)
            {
                mailBtn.transform.Find("redPoint").gameObject.SetActive(false);
                return;
            }
            mailBtn.transform.Find("redPoint").gameObject.SetActive(true);
            mailBtn.transform.Find("redPoint").GetComponentInChildren<Text>().text = node.pointNum.ToString();
        });
    }

    private void MainView_onClick()
    {
        mailViewGo.SetActive(true);
    }

}
