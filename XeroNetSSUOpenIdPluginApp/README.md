# Xero NetStandard Sign Up With Xero App (and Sign In)
This is a sample app that has been built with the Xero NetStandard SDK to show how app partners build the Sign Up With Xero Recommended flow. This particular app shows how a developer can build an application with .Net to support both *Sign Up with Xero and Sign In with Xero* capabilities. The sample app uses the Microsoft OpenId plugin to register separate authentication schemes for both sign in and sign up.

In order to run this application, users must have the following:
- A Xero app created with the credentials ready to use. See [Create a Xero app](#create-a-xero-app)
- A local instance of a MySQL Database running. See [Create a local database](#create-a-local-database)

## What is Sign Up With Xero Recommended Flow?
Xero can be used as an identity provider. This is where a user can use their Xero account to sign up to another service. 
App partners are required to implement the sign up with Xero as part of their certification, and this demo shows a sample of how an app
may use Xero to create an account. 

For more information about this process, refer to the [Sign Up with Xero blog post](https://developer.xero.com/documentation/xero-app-store/app-partner-guides/sign-up/) or the awesome [Sign Up with Xero explainer](https://www.google.com/url?q=https://www.youtube.com/watch?v%3DpFGHti5Y17Q%26t%3D2s%26ab_channel%3DXeroDeveloper&sa=D&source=docs&ust=1664933858033865&usg=AOvVaw3V1VrIUyyRpdPc8LgtN4xH) video Lee has created.


### Create a Xero app
You will need your Xero app credentials created to run this demo app.

To obtain your API keys, follow these steps:

* Create a [free Xero user account](https://www.xero.com/us/signup/api/) (if you don't have one)
* Login to [Xero developer center](https://developer.xero.com/myapps)
* Click "New App" link
* Enter your App name, company url, privacy policy url.
* Enter the redirect URI (your callback url - i.e. `https://localhost:5001/signup-oidc`)
* Agree to terms and condition and click "Create App".
* Navigate to the configuration page and create another redirect URI (your callback url - i.e. `https://localhost:5001/signin-oidc`)
* Click "Generate a secret" button.
* Copy your client id and client secret and save for use later.
* Click the "Save" button. You secret is now hidden.

Note that we create two separate redirect URI's to represent the sign up and sign in actions respectively.

### Download the code
Clone this repo to your local drive or open with GitHub desktop client.

### Configure your API Keys
In /XeroNetSSUOpenIdPluginApp/appsettings.json, you should populate your XeroConfiguration as such: 

```json
  "XeroConfiguration": {
    "ClientId": "YOUR_XERO_APP_CLIENT_ID",
    "ClientSecret": "YOUR_XERO_APP_CLIENT_SECRET",
    "CallbackUri": "https://localhost:5001/Authorization/Callback",
    "Scope": "openid offline_access profile ... ",
    "State": "YOUR_STATE"
  }
```

Note that you will have to have a state. The CallbackUri has to be exactly the same as redirect URI you put in Xero developer portal letter by letter. 

### Create a local database

For the purpose of mocking an authentication system, the app spins up a local database called Test in which a database table called "User" is created. This table will contain all the users that will be registered using Xero as an identity provider.

In order to create the database, we recommend using Microsoft Visual Studio.
1. Load the sample application in Microsoft Visual Studio
2. Open SQL Server Object Explorer. This should open in a left pane.
3. If you have an existing SQL Server available, select the expand button on the server to reveal a Databases folder.
4. Right click on the Databases folder. Click 'Add new Database'
5. Add a name and click Ok to create the database.
6. Once the database is created, right click on your newly created databases name and select properties.
7. The properties should display in the bottom left pane. Scroll and find the connection string. If only a few properties display, right click again on the database name and click refresh and then click properties again until the connection string appears.
8. Copy the connection string and replace in the appsettings.json as shown below:
```json
"ConnectionStrings": {
        "Database": "Data Source=(localdb)\\ProjectsV13..."
    }
```

## Getting started with _dotnet_  & command line 
You can run this application with [dotnet SDK](https://code.visualstudio.com/download) from command line. 
### Install dotnet SDK
[Download](https://code.visualstudio.com/download) and install dotnet SDK on your machine. 

Verify in command line by:
```
$ dotnet --version
3.1.102
```
### Build the project
Change directory to XeroNetSSUOpenIdPluginApp directory where you can see XeroNetSSUOpenIdPluginApp.csproj, build the project by: 

```
$ dotnet build
Microsoft (R) Build Engine version 16.4.0+e901037fe for .NET Core
Copyright (C) Microsoft Corporation. All rights reserved.
.
.
.

Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:02.22
```
### Run the project 
In /XeroNetSSUOpenIdPluginApp, run the project by:

```
$ dotnet run
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: https://localhost:5001
info: Microsoft.Hosting.Lifetime[0]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to shut down.
info: Microsoft.Hosting.Lifetime[0]
      Hosting environment: Development
info: Microsoft.Hosting.Lifetime[0]
      Content root path: /Users/.../Xero-NetStandard-App/XeroNetSSUOpenIdPluginApp
```
### Test the project
Open your browser, type in https://localhost:5001


## Some explanation of the code

### Startup.cs

```c#
services.AddDbContext<UserContext>(options => options.UseSqlServer(Configuration.GetConnectionString("Database")));
```
Within our `ConfigureServices()`, we add a database context. If you recall from earlier, you had to configure a local database and add the database connection string to the appsettings.json file. This database context allows our application to query from the database. We specify the UserContext as the type which is configured to create a table using our [User model](#user-model).

```c#
services.AddSingleton<StateContainer>();
```

As this is a simplified sample app, we have used a state container to encapsulate retaining the state information relating to:
- current Xero token (which is used to access the Xero API and obtain information about the user)
- current tenant (the organisation's information which is being displayed)

```c#
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
```
The code above shows how an open id authentication scheme is registered. The authority is set to point to the Xero identity server which will prompt users to log in to their Xero accounts when trying to sign in via the sample app. Setting save tokens to true ensures that the tokens are persisted and can be accessed within the application later.

The client ID and secret correspond to your applications ID and secret.

The response type specifies that the client would like authorization code returned to them upon authenticating.

The scopes control what the user can access once they have authenticated. For example, adding the open id scope ensures that you can access profile information about the user, which is necessary for the app to create and store information about the user.

The callback path is specified to point to the redirect URI that was supplied when configuring the application. This means that once the user hits the callback route, in this case `/signin-oidc`, the application should initiate the OAuth 2.0 code flow.

```c#
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
        };
		
		...
        await context.HttpContext.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(claimsIdentity), new AuthenticationProperties()
            {
              ExpiresUtc = accessToken.ValidTo,
            });
        return;
      };
    }

```

The on token validated function is called and in the sample app, we implement basic cookie authentication. Here, the app extracts information about the user from the tokens returned from Xero, and this is then embedded into a new claim which is used to sign the user in using the cookie authentication scheme. 

```c#
app.UseCookiePolicy(new CookiePolicyOptions()
{
  Secure = CookieSecurePolicy.Always
});
app.UseAuthentication();
app.UseAuthorization();
```

Finally, within `Configure()`, we need to specify that we want to use Authentication and Authorization as defined in `ConfigureServices()`. We have also added a specification which enforces the cookie policy to always be secure. This is necessary to override a Chrome error that may occur as by default, Chrome blocks insecure cookies.

### User Model
The user model represents key information that is stored in the local database. Attributes relating to the user's personal information, such as name, email, Xero user id etc, are populated from the id_token.  There is an additional property called state which is used to represent whether a user's account is linked to Xero or not. Whilst this application only allows users to register via Xero, a real application may provide an alternate registration method that does not use Xero as an identity provider. 


### Home Controller
The controller which handles the data fetching using the provided access token, and render information about the user (organisations, contacts and accounts).

#### Sign Up
When a user clicks the sign up button, they invoke the following

```c#
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
```

The Authorize attribute enforces that the user must be authenticated with the specified XeroSignUp scheme. As we've configured this using the OpenID plugin in our startup, the unauthenticated user will be redirected to the Xero identity server and must log in. Once successful, the `SignUpAsync` function will execute. It first uses the access token returned from the identity server to construct a XeroOAuth2Token. 

```c#
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

  xeroToken.Tenants = await client.GetConnectionsAsync(xeroToken);

  _stateContainer.XeroToken = xeroToken;
}
```

We do this so that we can also save the tenants that are currently connected without repeatedly calling `GetConnectionsAsync()`. Once we retrieve the tenants and update the XeroToken, we save this to our stateContainer so we can use this value for subsequent calls to the SDK. 

Once `setTokenAsync()` has finished executing, we can then create a user and add it to the local database if the user does not exist already. If they do, we update the database in case the details of the user changed. 

Finally, we update the state container to hold the current tenant's id as the application fetches information based on the selected tenant. When completed, the controller redirects to the `Index()` action which fetches details about the tenant's accounts and contacts and renders on the Home page.

#### Sign In
The process for sign in the same as sign up except for the user clicks the sign in button and the `signInAsync()` function is invoked.

### Revoking
Revoking access refers to when the user removes their connections to the application. If the user had 3 organisations connected and then clicked revoke, all 3 organisations will no longer be connected. In the application, revoking also results in the user's account being unlinked from 
Xero
### Disconnect/Deleting connections
Deleting a connection refers to when a user removes a single connection. If a user had 3 organisations connected and clicked delete connections, they would still have 2 connections remaining.

## License

This software is published under the [MIT License](http://en.wikipedia.org/wiki/MIT_License).

	Copyright (c) 2020 Xero Limited

	Permission is hereby granted, free of charge, to any person
	obtaining a copy of this software and associated documentation
	files (the "Software"), to deal in the Software without
	restriction, including without limitation the rights to use,
	copy, modify, merge, publish, distribute, sublicense, and/or sell
	copies of the Software, and to permit persons to whom the
	Software is furnished to do so, subject to the following
	conditions:

	The above copyright notice and this permission notice shall be
	included in all copies or substantial portions of the Software.

	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
	EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
	OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
	NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
	HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
	WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
	FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
	OTHER DEALINGS IN THE SOFTWARE.
