using System.Net;

namespace Domain.Exceptions.Custom;

public class ConflictException(string message)
    : CustomException(message, HttpStatusCode.Conflict);
