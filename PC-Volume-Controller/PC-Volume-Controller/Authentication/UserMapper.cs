using System.Configuration;
using Nancy.Authentication.Basic;
using Nancy.Security;

namespace PC_Volume_Controller.Authentication
{
  /// <summary>
  /// Class used for very basic user authentication. Only one user exists for this service and its credentials
  /// should be stored in the app.config file as Username and Password.
  /// </summary>
  /// <seealso cref="Nancy.Authentication.Basic.IUserValidator" />
  public class UserMapper : IUserValidator
  {
    public IUserIdentity Validate(string username, string password)
    {
      if (string.IsNullOrWhiteSpace(username) ||
          string.IsNullOrWhiteSpace(password))
      {
        return null;
      }

      string expectedUsername = ConfigurationManager.AppSettings["Username"];
      string expectedPassword = ConfigurationManager.AppSettings["Password"];

      //if this is true it means the app is not configured properly
      //if AuthenticateUser is true in the app.config file Username and Password must also be set.
      if (string.IsNullOrWhiteSpace(expectedUsername) ||
          string.IsNullOrWhiteSpace(expectedPassword))
      {
        return null;
      }

      if (username != expectedUsername ||
          password != expectedPassword)
      {
        return null;
      }

      return new User
      {
        UserName = username
      };
    }
  }
}