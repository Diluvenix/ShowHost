namespace Network
{
    public readonly record struct Result(
        bool Success,
        Exception? Error)
    {
        public static Result Ok()
            => new(true, null);

        public static Result Fail(Exception error)
            => new(false, error);
    }

    public readonly record struct Result<T>(
        bool Success,
        T? Value,
        Exception? Error)
    {
        public static Result<T> Ok(T value)
            => new(true, value, null);

        public static Result<T> Fail(Exception error)
            => new(false, default, error);
    }
}
