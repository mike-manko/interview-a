namespace SupportTriage.Domain.SharedKernel;

// Values equal the HTTP status the API filter answers with (api.md "Problem details").
public enum ErrorType
{
    Validation = 400,
    Unauthorized = 401,
    Forbidden = 403,
    NotFound = 404,
    Conflict = 409,
    PreconditionFailed = 412,
    Unexpected = 500,
}
