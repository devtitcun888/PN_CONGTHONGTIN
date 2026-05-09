using PN_HDSWeb_Library;

namespace PN_HDSWeb_Admin.Services.Auth;

public sealed record LoginResult(bool IsSuccess, string Message, UserSession? Session)
{
    public static LoginResult Ok(UserSession session) => new(true, string.Empty, session);
    public static LoginResult Fail(string message) => new(false, message, null);
}
