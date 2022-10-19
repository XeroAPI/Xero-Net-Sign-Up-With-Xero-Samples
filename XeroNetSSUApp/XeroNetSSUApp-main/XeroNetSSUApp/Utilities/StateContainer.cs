using System;
using System.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Net.Http;
using Xero.NetStandard.OAuth2.Token;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Xero.NetStandard.OAuth2.Client;
using Microsoft.Extensions.DependencyInjection;
using Xero.NetStandard.OAuth2.Models;

public class StateContainer
{
  [Serializable]
  public struct State
  {  
    public string state {get; set;}
    public State(string state){
      this.state = state;
    }
  }

  public State CurrentState { get; set;  }

  public XeroOAuth2Token Token { get; set; }

  public Tenant CurrentTenant { get; set; }

  public XeroOAuth2Token GetStoredToken()
  {
    if (Token != null)
    {
      return Token;
    }
    
    return new XeroOAuth2Token();
  }

  public bool TokenExists()
  {
    return Token != null;
  }

  public void DestroyToken()
  {
    Token = null;
  }
}
