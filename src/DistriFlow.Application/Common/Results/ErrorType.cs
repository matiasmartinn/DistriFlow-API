namespace DistriFlow.Application.Common.Results;

public enum ErrorType
{
    None,
    Validation,
    Unauthorized,
    Forbidden,
    NotFound,
    Conflict,
    Domain,
    Unexpected
}
