using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Xero.NetStandard.OAuth2.Api;
using Xero.NetStandard.OAuth2.Client;
using Xero.NetStandard.OAuth2.Config;
using Xero.NetStandard.OAuth2.Token;
using XeroNetSSUApp.Models;

namespace XeroNetSSUApp.Controllers
{
  public class HomeController : Controller
  {
    private readonly ILogger<HomeController> _logger;
    private readonly IOptions<XeroConfiguration> XeroConfig;
    private readonly StateContainer _stateContainer;

    public HomeController(IOptions<XeroConfiguration> XeroConfig, ILogger<HomeController> logger, StateContainer stateContainer)
    {
      _logger = logger;
      this.XeroConfig = XeroConfig;
      _stateContainer = stateContainer;
    } 
    public async Task<IActionResult> IndexAsync([FromQuery] Guid? tenantId)
    {
      if (User.Identity.IsAuthenticated)
      {

        // Get token and refresh if expired
        var xeroToken = _stateContainer.GetStoredToken();
        var utcTimeNow = DateTime.UtcNow;

        if (utcTimeNow > xeroToken.ExpiresAtUtc)
        {
          xeroToken = await updateToken(xeroToken);
        }

        // Set tenantId to a valid tenantId that has been parsed in the URL
        // or set as first tenant in the list of connections
        string accessToken = xeroToken.AccessToken;
        if (!(tenantId is Guid tenantIdValue))
        {
          if (_stateContainer.CurrentTenant != null)
          {
            tenantIdValue = _stateContainer.CurrentTenant.TenantId;
          } else
          {
            tenantIdValue = new Guid();
          }
        }
        if (xeroToken.Tenants.Any((t) => t.TenantId == tenantIdValue))
        {
          _stateContainer.CurrentTenant = xeroToken.Tenants.First((t) => t.TenantId == tenantIdValue);
        }
        else
        {
          _stateContainer.CurrentTenant = xeroToken.Tenants.First();
        }

        // Make calls to Xero requesting organisation info, accounts and contacts and feed into dashboard
        var AccountingApi = new AccountingApi();
        try
        {
          var organisation_info = await AccountingApi.GetOrganisationsAsync(accessToken, _stateContainer.CurrentTenant.TenantId.ToString());

          var accounts = await AccountingApi.GetAccountsAsync(accessToken, _stateContainer.CurrentTenant.TenantId.ToString());

          var contacts = await AccountingApi.GetContactsAsync(accessToken, _stateContainer.CurrentTenant.TenantId.ToString());
          
          var response = new DashboardModel { accounts = accounts, contacts = contacts, organisation = organisation_info };

          return View(response);
        } catch (ApiException e)
        {
          // If the current tenant is disconnected from the app, redirect to re-authorize
          if (e.ErrorCode == 403)
          {
            return RedirectToAction("Index", "Authorization");
          }
        }
          
      }

      return View();
    }

    // Refreshes token and updates local token to contain updated version
    private async Task<XeroOAuth2Token> updateToken(XeroOAuth2Token xeroToken) 
    {
      var client = new XeroClient(XeroConfig.Value);
      xeroToken = (XeroOAuth2Token)await client.RefreshAccessTokenAsync(xeroToken);
      _stateContainer.Token = xeroToken;
      return xeroToken;
    }

    public IActionResult Privacy()
    {
      return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
      return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
  }
}


