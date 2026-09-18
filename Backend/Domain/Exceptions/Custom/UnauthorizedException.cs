using System.Net;

namespace Domain.Exceptions.Custom;

public class UnauthorizedException(string message)
    : CustomException(message, HttpStatusCode.Unauthorized);
