using System.Net;

namespace Domain.Exceptions.Custom;

public class EntityValidationException(string message)
    : CustomException(message, HttpStatusCode.UnprocessableContent);
