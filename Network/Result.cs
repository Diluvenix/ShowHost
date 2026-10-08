namespace Network
{
    public readonly record struct Result(
        bool Success,
        Exception? Error)
    {
        public static Result Ok()
            => new(true, default);

        public static Result Fail(Exception error)
            => new(false, error);
    }

    public readonly record struct Result<T>(
        bool Success,
        T? Value,
        Exception? Error)
    {
        public static Result<T> Ok(T value)
            => new(true, value, default);

        public static Result<T> Fail(Exception error)
            => new(false, default, error);

        public static implicit operator Result(Result<T> result)
            => new(result.Success, result.Error);

        public static implicit operator Result<object>(Result<T> result)
            => new(result.Success, result.Value, result.Error);
    }
}
