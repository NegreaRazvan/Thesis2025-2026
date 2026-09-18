using System.Net;

namespace Domain.Exceptions;

public record ExceptionResponse(HttpStatusCode StatusCode, string Message);
