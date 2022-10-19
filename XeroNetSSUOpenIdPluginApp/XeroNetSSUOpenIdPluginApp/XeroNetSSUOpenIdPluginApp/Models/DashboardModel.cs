using Xero.NetStandard.OAuth2.Model.Accounting;

namespace XeroNetSSUOpenIdPluginApp.Models
{
  public class DashboardModel
  {
    public Organisations organisation { get; set; }

    public Contacts contacts { get; set; }

    public Accounts accounts { get; set; }
  }
}
