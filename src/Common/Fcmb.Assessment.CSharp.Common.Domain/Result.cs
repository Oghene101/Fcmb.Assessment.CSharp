namespace Fcmb.Assessment.CSharp.Common.Domain;

public class Result
{
    protected Result(bool isSuccess, Error? error)
    {
        switch (isSuccess)
        {
            case true when error is not null:
                throw new InvalidOperationException("cannot be successful with error");
            case false when error is null:
                throw new InvalidOperationException("cannot be unsuccessful without error");
        }

        IsSuccess = isSuccess;
        Error = error;
    }

    public readonly bool IsSuccess;
    public readonly Error? Error;

    public static Result Success() => new(true, Error.None);
    public static Result<TValue> Success<TValue>(TValue value) => new(value, true, Error.None);

    public static Result Failure(Error error) => new(false, error);

    public static Result<TValue> Failure<TValue>(Error error) => new(default!, false, error);

    public static implicit operator Result(Error error) => Failure(error);

    public void Deconstruct(out bool isSuccess, out Error? error)
    {
        isSuccess = IsSuccess;
        error = Error;
    }
}

public sealed class Result<TValue>(TValue value, bool isSuccess, Error? error)
    : Result(isSuccess, error)

{
    private readonly TValue _value = isSuccess switch
    {
        true when value is null => throw new InvalidOperationException("Successful result must have a value"),
        false when value is not null => throw new InvalidOperationException("Failed result must not have a value"),
        _ => value
    };

    public TValue Value =>
        IsSuccess ? _value : throw new InvalidOperationException("Cannot access value of a failed result");

    public static implicit operator Result<TValue>(TValue value) => Success(value);

    public static implicit operator TValue(Result<TValue> result)
    {
        if (result.IsSuccess) { return result.Value; }

        return default;
    }

    public static implicit operator Result<TValue>(Error error) => Failure<TValue>(error);

    public void Deconstruct(out bool isSuccess, out TValue value, out Error? error)
    {
        isSuccess = IsSuccess;
        value = _value;
        error = Error;
    }
}
