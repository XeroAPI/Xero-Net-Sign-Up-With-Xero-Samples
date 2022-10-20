using Microsoft.EntityFrameworkCore;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using XeroNetSSUOpenIdPluginApp.Models;

namespace XeroNetSSUOpenIdPluginApp.Utilities
{
  public class DbUtilities
  {

    // Creates a user in the local database
    public static void RegisterUserToDb(User user, UserContext _context)
    {
      _context.Database.EnsureCreated();

      if (_context.User.Find(user.XeroUserId) != null)
      {
        var existingUser = _context.Find<User>(user.XeroUserId);
        _context.Entry(existingUser).CurrentValues.SetValues(user);
        _context.Entry(existingUser).State = EntityState.Modified;
      }
      else
      {
        _context.Add<User>(user);
      }
      _context.SaveChanges();
    }

    // Updates a user in the local database
    public static void UpdateUser(User user, UserContext _context)
    {
      _context.Database.EnsureCreated();

      if (_context.User.Find(user.XeroUserId) != null)
      {
        var existingUser = _context.Find<User>(user.XeroUserId);
        _context.Entry(existingUser).CurrentValues.SetValues(user);
        _context.Entry(existingUser).State = EntityState.Modified;
      }
      _context.SaveChanges();
    }

    // Checks if user exists in the local database
    public static bool UserExists(User user, UserContext _context)
    {
      _context.Database.EnsureCreated();

      if (_context.User.Find(user.XeroUserId) != null)
      {
        return true;
      }
      else
      {
        return false;
      }
    }

    // Disconnect user's account from Xero 
    public static void DisconnectAccountFromXero(User user, UserContext _context)
    {
      _context.Database.EnsureCreated();

      if (_context.User.Find(user.XeroUserId) != null)
      {
        var existingUser = _context.Find<User>(user.XeroUserId);
        user.Status = User.StatusEnum.NotLinkedToXero;
        _context.Entry(existingUser).CurrentValues.SetValues(user);
        _context.Entry(existingUser).State = EntityState.Modified;
      }
      _context.SaveChanges();
    }

    // Delete account for user in the local database
    public static void DeleteAccount(User user, UserContext _context)
    {
      _context.Database.EnsureCreated();

      if (_context.User.Find(user.XeroUserId) != null)
      {
        var existingUser = _context.Find<User>(user.XeroUserId);
        _context.Entry(existingUser).State = EntityState.Deleted;
      }
      _context.SaveChanges();
    }

    public static User GetUserFromIdToken(String IdToken)
    {
      var handler = new JwtSecurityTokenHandler();
      var token = handler.ReadJwtToken(IdToken);

      // Extract the information from token
      return new User
      {
        Email = token.Claims.First(claim => claim.Type == "email").Value,
        XeroUserId = token.Claims.First(claim => claim.Type == "xero_userid").Value,
        SessionId = token.Claims.First(claim => claim.Type == "global_session_id").Value,
        Name = token.Claims.First(claim => claim.Type == "name").Value,
        FirstName = token.Claims.First(claim => claim.Type == "given_name").Value,
        LastName = token.Claims.First(claim => claim.Type == "family_name").Value,
        Status = User.StatusEnum.LinkedToXero
      };
    }
  }
}
