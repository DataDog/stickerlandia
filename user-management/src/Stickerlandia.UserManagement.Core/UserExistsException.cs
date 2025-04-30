namespace Stickerlandia.UserManagement.Core;

public class UserExistsException : Exception
{
    public UserExistsException() : base()
    {
    }

    public UserExistsException(string message) : base(message)
    {
    }

    public UserExistsException(string message, Exception innerException) : base(message, innerException)
    {
    }
}