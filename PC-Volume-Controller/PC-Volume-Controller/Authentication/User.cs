using System.Collections.Generic;
using Nancy.Security;

namespace PC_Volume_Controller.Authentication
{
  public class User : IUserIdentity
  {
    #region Fields

    private string _UserName;
    private IEnumerable<string> _Claims;

    #endregion

    #region Properties

    public string UserName
    {
      get { return _UserName; }
      set { _UserName = value; }
    }

    public IEnumerable<string> Claims
    {
      get { return _Claims; }
      set { _Claims = value; }
    }

    #endregion
  }
}