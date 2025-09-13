using System.Collections.Generic;

public enum MailType : byte
{
    System,
    Team,
    Alliance
}

public class MailData
{
    public int id;
    public MailType mailType;
    public string sender;
    public string subject;
    public string body;
    public bool isRead;
    public MailData(int id, MailType type, string sender, string subject, string body)
    {
        this.id = id;
        this.mailType = type;
        this.sender = sender;
        this.subject = subject;
        this.body = body;
        this.isRead = false;
    }
}
public class MailModel
{
    #region µ¥ÀýÄ£Ê½
    private static MailModel _instance;
    public static MailModel Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = new MailModel();
            }
            return _instance;
        }
    }
    private MailModel() { }

    #endregion
    private List<MailData> mails = new List<MailData>();
    public List<MailData> Mails { get { return mails; } }
    public void init()
    {
        this.AddMail(new MailData(1, MailType.System, "System", "Welcome!", "Welcome to the game!"));
        this.AddMail(new MailData(2, MailType.Team, "TeamLeader", "Team Meeting", "Don't forget our team meeting at 5 PM."));
        this.AddMail(new MailData(3, MailType.Alliance, "AllianceMaster", "Alliance War", "Prepare for the upcoming alliance war!"));
    }
    public void AddMail(MailData mail)
    {
        mails.Add(mail);
        switch (mail.mailType)
        {
            case MailType.System:
                RedPointSystem.Instance.SetInvoke(RedPointConst.mailSystem, 1);
                break;
            case MailType.Team:
                RedPointSystem.Instance.SetInvoke(RedPointConst.mailTeam, 1);
                break;
            case MailType.Alliance:
                RedPointSystem.Instance.SetInvoke(RedPointConst.mailAlliance, 1);
                break;
            default:
                break;
        }
    }

    public void RemoveMail(MailData mail)
    {
        mails.Remove(mail);
    }

    public void SetMailRead(MailData mail, bool isRead)
    {
        mail.isRead = isRead;
        switch (mail.mailType)
        {
            case MailType.System:
                RedPointSystem.Instance.SetInvoke(RedPointConst.mailSystem, 0);
                break;
            case MailType.Team:
                RedPointSystem.Instance.SetInvoke(RedPointConst.mailTeam, 0);
                break;
            case MailType.Alliance:
                RedPointSystem.Instance.SetInvoke(RedPointConst.mailAlliance, 0);
                break;
            default:
                break;
        }
    }

    public MailData CreateMailData(string v1, string v2, MailType type)
    {
        return new MailData(UnityEngine.Random.Range(1000, 9999), type, "System", v1, v2);
    }
}
