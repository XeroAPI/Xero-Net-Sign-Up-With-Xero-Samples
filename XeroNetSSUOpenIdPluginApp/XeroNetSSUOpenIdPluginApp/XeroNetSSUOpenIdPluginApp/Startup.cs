using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text.Json;
using System.Threading.Tasks;
using Xero.NetStandard.OAuth2.Client;
using Xero.NetStandard.OAuth2.Config;
using Xero.NetStandard.OAuth2.Token;
using XeroNetSSUOpenIdPluginApp.Utilities;

namespace XeroNetSSUOpenIdPluginApp
{
  public class Startup
  {
    public Startup(IConfiguration configuration)
    {
      Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    public void ConfigureServices(IServiceCollection services)
    {
      services.AddHttpClient();
      services.AddControllersWithViews();
      services.AddDbContext<UserContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Database")));
      services.AddSingleton<StateContainer>();
      services.Configure<XeroConfiguration>(Configuration.GetSection("XeroConfiguration"));
      services.AddAuthentication(options =>
      {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = "XeroSignIn";
      })
            .AddCookie()
            .AddOpenIdConnect("XeroSignIn", options =>
            {
              options.Authority = "https://identity.xero.com";
              options.SaveTokens = true;

              options.ClientId = Configuration["XeroConfiguration:ClientId"];
              options.ClientSecret = Configuration["XeroConfiguration:ClientSecret"];

              options.ResponseType = "code";

              options.Scope.Clear();
              foreach (var scope in Configuration["XeroConfiguration:Scope"].Split(" "))
              {
                options.Scope.Add(scope);
              }

              options.CallbackPath = "/signin-oidc";

              options.Events = new OpenIdConnectEvents
              {
                OnTokenValidated = OnTokenValidated()
              };
            })
            .AddOpenIdConnect("XeroSignUp", options =>
            {
              options.Authority = "https://identity.xero.com";
              options.SaveTokens = true;

              options.ClientId = Configuration["XeroConfiguration:ClientId"];
              options.ClientSecret = Configuration["XeroConfiguration:ClientSecret"];

              options.ResponseType = "code";

              options.Scope.Clear();
              foreach (var scope in Configuration["XeroConfiguration:Scope"].Split(" "))
              {
                options.Scope.Add(scope);
              }

              options.CallbackPath = "/signup-oidc";

              options.Events = new OpenIdConnectEvents
              {
                OnTokenValidated = OnTokenValidated(),
              };
            });
      
      services.AddDistributedMemoryCache();
      services.AddSession();
      services.AddMvc(options => options.EnableEndpointRouting = false);
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
    {
      if (env.IsDevelopment())
      {
        app.UseDeveloperExceptionPage();
      }
      else
      {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
      }
      app.UseStaticFiles();

      app.UseRouting();
      app.UseCookiePolicy(new CookiePolicyOptions()
      {
        Secure = CookieSecurePolicy.Always
      });
      app.UseAuthentication();
      app.UseAuthorization();
      
      app.UseHttpsRedirection();

      app.UseSession();

      app.UseEndpoints(endpoints =>
      {
        endpoints.MapControllerRoute(
                  name: "default",
                  pattern: "{controller=Home}/{action=Index}/{id?}");
      });

      app.UseStaticFiles();



      app.UseMvc();
    }

    private static Func<TokenValidatedContext, Task> OnTokenValidated()
    {
      return async context =>
      {

        var handler = new JwtSecurityTokenHandler();
        var accessToken = handler.ReadJwtToken(context.TokenEndpointResponse.AccessToken);
        var idToken = handler.ReadJwtToken(context.TokenEndpointResponse.IdToken);
        
        // Custom cookie authentication
        var claims = new List<Claim>()
        {
          new Claim("XeroUserId", accessToken.Claims.First(Claim => Claim.Type == "xero_userid").Value),
          new Claim("SessionId", accessToken.Claims.First(claim => claim.Type == "global_session_id").Value),
          new Claim("Name", idToken.Claims.First(claim => claim.Type == "name").Value),
          new Claim("FirstName", idToken.Claims.First(claim => claim.Type == "given_name").Value),
          new Claim("LastName", idToken.Claims.First(claim => claim.Type == "family_name").Value),
          new Claim ("Email", idToken.Claims.First(claim => claim.Type == "email").Value),
        };

        var claimsIdentity = new ClaimsIdentity(
            claims, CookieAuthenticationDefaults.AuthenticationScheme);

        context.Principal.AddIdentity(claimsIdentity);

        await context.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties()
            {
              ExpiresUtc = accessToken.ValidTo,
            });
        return;
      };
    }
  }
}
