namespace SideQuest.Services
{
    public enum ServiceResultStatus
    {
        Success,
        Created,
        NotFound,
        Forbidden,
        Conflict,
        Validation,
        Unauthorized
    }

    public sealed class ServiceResult<T>
    {
        private ServiceResult(ServiceResultStatus status, T? value, string? message, IReadOnlyDictionary<string, string[]>? errors)
        {
            Status = status;
            Value = value;
            Message = message;
            Errors = errors ?? new Dictionary<string, string[]>();
        }

        public ServiceResultStatus Status { get; }

        public T? Value { get; }

        public string? Message { get; }

        public IReadOnlyDictionary<string, string[]> Errors { get; }

        public bool Succeeded => Status is ServiceResultStatus.Success or ServiceResultStatus.Created;

        public static ServiceResult<T> Success(T value) => new(ServiceResultStatus.Success, value, null, null);

        public static ServiceResult<T> Created(T value) => new(ServiceResultStatus.Created, value, null, null);

        public static ServiceResult<T> NotFound(string message) => new(ServiceResultStatus.NotFound, default, message, null);

        public static ServiceResult<T> Forbidden(string message) => new(ServiceResultStatus.Forbidden, default, message, null);

        public static ServiceResult<T> Conflict(string message) => new(ServiceResultStatus.Conflict, default, message, null);

        public static ServiceResult<T> Unauthorized(string message) => new(ServiceResultStatus.Unauthorized, default, message, null);

        public static ServiceResult<T> Validation(string field, string message)
            => new(ServiceResultStatus.Validation, default, message, new Dictionary<string, string[]>
            {
                [field] = [message]
            });

        public static ServiceResult<T> Validation(IReadOnlyDictionary<string, string[]> errors, string? message = null)
            => new(ServiceResultStatus.Validation, default, message, errors);
    }
}
