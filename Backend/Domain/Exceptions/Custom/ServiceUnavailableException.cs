using System.Net;

namespace Domain.Exceptions.Custom;

public class ServiceUnavailableException(string message) : CustomException(message, HttpStatusCode.ServiceUnavailable);
