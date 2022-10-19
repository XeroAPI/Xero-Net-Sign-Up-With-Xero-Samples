using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xero.NetStandard.OAuth2.Token;
using XeroNetSSUOpenIdPluginApp.Utilities;

namespace XeroNetSSUOpenIdPluginApp.ViewComponents{
  public class NavBarViewComponent : ViewComponent
  {
    private readonly StateContainer _stateContainer;

    public NavBarViewComponent(StateContainer stateContainer)
    {
      _stateContainer = stateContainer;
    }
    public class TenantDetails
    {
      public string TenantName { get; set; }
      public Guid TenantId { get; set; }
    }
#pragma warning disable CS1998 // This async method lacks 'await' operators
    public async Task<IViewComponentResult> InvokeAsync(int maxPriority, bool isDone)
    {
      if (User.Identity.IsAuthenticated)
      {
        XeroOAuth2Token xeroToken = _stateContainer.XeroToken;

        var tenantId = _stateContainer.CurrentTenant.TenantId;
        try
        {
          ViewBag.OrgPickerCurrentTenantId = tenantId;
          ViewBag.OrgPickerTenantList = xeroToken.Tenants.Select(
            (t) => new TenantDetails { TenantName = t.TenantName, TenantId = t.TenantId }
          ).ToList();
        }
        catch (Exception)
        {
          // handle exception
        }
      }

      return View(User.Identity.IsAuthenticated);
    }
#pragma warning restore CS1998 // This async method lacks 'await' operators
  }

}

