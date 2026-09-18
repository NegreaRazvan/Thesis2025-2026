using System.Net;

namespace Domain.Exceptions.Custom;

public class UnprocessableContentException(string message)
    : CustomException(message, HttpStatusCode.UnprocessableContent);
