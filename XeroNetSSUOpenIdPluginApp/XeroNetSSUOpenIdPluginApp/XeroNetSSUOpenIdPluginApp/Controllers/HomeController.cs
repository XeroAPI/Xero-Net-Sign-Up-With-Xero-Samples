using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Diagnostics;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Threading.Tasks;
using Xero.NetStandard.OAuth2.Api;
using Xero.NetStandard.OAuth2.Client;
using Xero.NetStandard.OAuth2.Config;
using Xero.NetStandard.OAuth2.Models;
using Xero.NetStandard.OAuth2.Token;
using XeroNetSSUOpenIdPluginApp.Models;
using XeroNetSSUOpenIdPluginApp.Utilities;

namespace XeroNetSSUOpenIdPluginApp.Controllers
{
  public class HomeController : Controller
  {
    private readonly ILogger<HomeController> _logger;
    private readonly IOptions<XeroConfiguration> XeroConfig;
    private readonly UserContext _context;
    private readonly StateContainer _stateContainer;


    public HomeController(IOptions<XeroConfiguration> XeroConfig, ILogger<HomeController> logger, UserContext context, StateContainer stateContainer)
    {
      _logger = logger;
      this.XeroConfig = XeroConfig;
      _context = context;
      _stateContainer = stateContainer;
    } 
    public async Task<IActionResult> IndexAsync([FromQuery] Guid? tenantId)
    {
      if (User.Identity.IsAuthenticated)
      {
        

        await RefreshToken();
        XeroOAuth2Token xeroToken = _stateContainer.XeroToken;
        var accessToken = xeroToken.AccessToken;
        
        if (tenantId is Guid tenantIdValue)
        {
          if (xeroToken.Tenants.Any((t) => t.TenantId == tenantIdValue))
          {
            _stateContainer.CurrentTenant = xeroToken.Tenants.First((t) => t.TenantId == tenantIdValue); ;
          }
        } if (_stateContainer.CurrentTenant == null)
        {
          _stateContainer.CurrentTenant = xeroToken.Tenants.First();
        }
        Tenant xeroTenant = _stateContainer.CurrentTenant;


        // Make calls to Xero requesting organisation info, accounts and contacts and feed into dashboard
        var AccountingApi = new AccountingApi();
        try
        {
          var organisation_info = await AccountingApi.GetOrganisationsAsync(accessToken, xeroTenant.TenantId.ToString());

          var contacts = await AccountingApi.GetContactsAsync(accessToken, xeroTenant.TenantId.ToString());

          var accounts = await AccountingApi.GetAccountsAsync(accessToken, xeroTenant.TenantId.ToString());
                    
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

    [HttpGet]
    public async Task<ActionResult> Disconnect()
    {
      var client = new XeroClient(XeroConfig.Value);
      await RefreshToken();

      XeroOAuth2Token xeroToken = _stateContainer.XeroToken;

      Tenant xeroTenant = _stateContainer.CurrentTenant;

      await client.DeleteConnectionAsync(xeroToken, xeroTenant);

      // Update the xero token to exclude removed tenant
      xeroToken.Tenants.Remove(xeroTenant);

      // If other tenants exist, set the next tenant as current tenant and update xero token to exclude deleted token. Otherwise destroy token
      if (xeroToken.Tenants.Count > 0)
      {
        _stateContainer.XeroToken = xeroToken;
        _stateContainer.CurrentTenant = xeroToken.Tenants[0];
      }
      else
      {
        return RedirectToAction("SignOut", "Home");
      }

      return RedirectToAction("Index", "Home");
    }

    [HttpGet]
    public async Task<ActionResult> Revoke()
    {
      var client = new XeroClient(XeroConfig.Value);

      await RefreshToken();
      XeroOAuth2Token xeroToken = _stateContainer.XeroToken;

      await client.RevokeAccessTokenAsync(xeroToken);

      // Disconnects xero connection from  the users account
      User user = DbUtilities.GetUserFromIdToken(xeroToken.IdToken);
      DbUtilities.DisconnectAccountFromXero(user, _context);

      return RedirectToAction("SignOut", "Home");
    }

    [HttpGet]
    public async Task<ActionResult> Reconnect()
    {
      await HttpContext.SignOutAsync("Cookies");
      await HttpContext.SignOutAsync("XeroSignIn");
      await HttpContext.SignOutAsync("XeroSignUp");
      return Redirect("/home/signin");
    }


    [HttpGet]
    [Authorize]
    public IActionResult NoTenants()
    {
      return View();
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "XeroSignUp")]
    public async Task<IActionResult> SignUpAsync()
    {
      await setTokenAsync();
      User user = DbUtilities.GetUserFromIdToken(await HttpContext.GetTokenAsync("id_token"));
      // Can customise login behaviour to indicate account created when user signed in for the first time
      if (!DbUtilities.UserExists(user, _context))
      {
        DbUtilities.RegisterUserToDb(user, _context);
      }
      else
      {
        DbUtilities.UpdateUser(user, _context);
      }
      XeroOAuth2Token xeroToken = _stateContainer.XeroToken;
      _stateContainer.CurrentTenant = xeroToken.Tenants.First();
      return RedirectToAction("Index");
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = "XeroSignIn")]
    public async Task<IActionResult> SignInAsync()
    {
      await setTokenAsync();
      User user = DbUtilities.GetUserFromIdToken(await HttpContext.GetTokenAsync("id_token"));
      // Can customise login behaviour to indicate account created when user signed in for the first time
      if (!DbUtilities.UserExists(user, _context))
      {
        DbUtilities.RegisterUserToDb(user, _context);
      }
      else
      {
        DbUtilities.UpdateUser(user, _context);
      }
      XeroOAuth2Token xeroToken = _stateContainer.XeroToken;
      _stateContainer.CurrentTenant = xeroToken.Tenants.First();
      return RedirectToAction("Index");
    }

    private async Task setTokenAsync()
    {
      var client = new XeroClient(XeroConfig.Value);

      var handler = new JwtSecurityTokenHandler();
      var accessToken = await HttpContext.GetTokenAsync("access_token");
      var accessTokenParsed = handler.ReadJwtToken(accessToken);

      var xeroToken = new XeroOAuth2Token()
      {
        AccessToken = accessToken,
        IdToken = await HttpContext.GetTokenAsync("id_token"),
        ExpiresAtUtc = accessTokenParsed.ValidTo,
        RefreshToken = await HttpContext.GetTokenAsync("refresh_token"),
      };

      if (DateTime.UtcNow > xeroToken.ExpiresAtUtc)
      {
        var newToken = await client.RefreshAccessTokenAsync(xeroToken);
        _stateContainer.XeroToken = (XeroOAuth2Token)newToken;
      }

      xeroToken.Tenants = await client.GetConnectionsAsync(xeroToken);

      _stateContainer.XeroToken = xeroToken;
    }

    private async Task RefreshToken()
    {
      var client = new XeroClient(XeroConfig.Value);
      XeroOAuth2Token xeroToken = _stateContainer.XeroToken;
      var utcTimeNow = DateTime.UtcNow;

      if (xeroToken == null)
      {
        await setTokenAsync();
        xeroToken = _stateContainer.XeroToken;
      }

      if (utcTimeNow > xeroToken.ExpiresAtUtc)
      {
        var newToken = await client.RefreshAccessTokenAsync(xeroToken);
        _stateContainer.XeroToken = (XeroOAuth2Token)newToken;
      }
    }

    [HttpGet]
    public async Task<IActionResult> SignOut()
    {
      await HttpContext.SignOutAsync("Cookies");
      await HttpContext.SignOutAsync("XeroSignIn");
      await HttpContext.SignOutAsync("XeroSignUp");
      return RedirectToAction("Index");
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


