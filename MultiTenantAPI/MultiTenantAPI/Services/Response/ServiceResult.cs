using Microsoft.AspNetCore.Identity;

namespace MultiTenantAPI.Services.Response
{
    public class ServiceResult<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? ErrorMessage { get; set; }
        public object? Errors { get; set; }

        public static ServiceResult<T> Ok(T data) => new() { Success = true, Data = data };
        public static ServiceResult<T> Fail(string message) => new()
        {
            Success = false,
            ErrorMessage = message,
            Errors = new[] { message }
        };

        public static ServiceResult<T> Fail(object errors) => new()
        {
            Success = false,
            Errors = errors
        };

        public static ServiceResult<T> Fail(IEnumerable<IdentityError> identityErrors) => new()
        {
            Success = false,
            Errors = identityErrors.Select(e => new { e.Code, e.Description })
        };
    }
}
