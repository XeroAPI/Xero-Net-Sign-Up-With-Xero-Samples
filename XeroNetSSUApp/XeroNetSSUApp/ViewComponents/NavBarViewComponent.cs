using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace XeroNetSSUApp.ViewComponents{
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
        var xeroToken = _stateContainer.Token;

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

      return View(_stateContainer.TokenExists());
    }
#pragma warning restore CS1998 // This async method lacks 'await' operators
  }

}

