namespace Attractions.DTOs;

public class ServiceResult
{
    public bool IsSuccess { get; }
    public string Message { get; }

    protected ServiceResult(bool isSuccess, string message)
    {
        IsSuccess = isSuccess;
        Message = message;
    }

    public static ServiceResult Ok(string message) => new(true, message);
    public static ServiceResult Fail(string message) => new(false, message);
}

public class ServiceResult<T> : ServiceResult
{
    public T? Data { get; }

    private ServiceResult(bool isSuccess, string message, T? data)
        : base(isSuccess, message)
    {
        Data = data;
    }

    public static ServiceResult<T> Ok(T data, string message) => new(true, message, data);
    public new static ServiceResult<T> Fail(string message) => new(false, message, default);
}
