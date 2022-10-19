# Xero NetStandard Sign Up With Xero App
This is a sample app that has been built with the Xero NetStandard SDK to show how app partners build the Sign Up With Xero Recommended flow. This particular app shows how a developer building an application with .Net who only wants to support *Sign Up with Xero* capabilities may do so. The sample example shows how you would embed a standalone sign up with Xero button and use the SDK to use Xero as an identity provider.

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
* Enter the redirect URI (your callback url - i.e. `https://localhost:5001/Authorization/Callback`)
* Agree to terms and condition and click "Create App".
* Click "Generate a secret" button.
* Copy your client id and client secret and save for use later.
* Click the "Save" button. You secret is now hidden.

### Download the code
Clone this repo to your local drive or open with GitHub desktop client.

### Configure your API Keys
In /XeroNetSSUApp/appsettings.json, you should populate your XeroConfiguration as such: 

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
Change directory to XeroNetSSUApp directory where you can see XeroNetSSUApp.csproj, build the project by: 

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
In /XeroNetSSUApp, run the project by:

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
      Content root path: /Users/.../Xero-NetStandard-App/XeroNetSSUApp
```
### Test the project
Open your browser, type in https://localhost:5001


## Explanation of the code

### User Model
The user model represents key information that is stored in the local database. Attributes relating to the user's personal information, such as name, email, Xero user id etc, are populated from the id_token.  There is an additional property called state which is used to represent whether a user's account is linked to Xero or not. Whilst this application only allows users to register via Xero, a real application may provide an alternate registration method that does not use Xero as an identity provider. 

### State container
As this is a simplified sample app, we have used a state container to encapsulate retaining the state information relating to:
- current Xero token (which is used to access the Xero API and obtain information about the user)
- current tenant (the organisation's information which is being displayed)
- state (the state to detect any CSRF attacks)

### Authorization Controller
#### Signing up
The Authorization Controller handles the logic of using Xero as an identity provider. Once the user selects the connect to Xero option, they are redirected to the callback URI. This initiates a connection to Xero via the standard OAuth 2.0 flow. Once the user authorizes the connection, the sample app receives tokens that can be used to access the Xero API and identity details. As we want to use Xero to 
create an account, we use decode and parse the ID token returned to extract out user information (name, email, Xero user ID) which is then fed directly into the sample app's registration process (creating a user in the local mySQL database).


### Revoking
Revoking access refers to when the user removes their connections to the application. If the user had 3 organisations connected and then clicked revoke, all 3 organisations will no longer be connected. In the application, revoking also results in the user's account being unlinked from 
Xero
### Disconnect/Deleting connections
Deleting a connection refers to when a user removes a single connection. If a user had 3 organisations connected and clicked delete connections, they would still have 2 connections remaining. For the application, when a user selects delete connection when there is one organisation, the behaviour is the same as [revoking](#revoking).
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
