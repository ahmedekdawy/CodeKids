namespace CodeKids.Application.Common;

public sealed class ApiException : Exception
{
    public string Code { get; }
    public IReadOnlyDictionary<string, string> Args { get; }

    public ApiException(string code, string? message = null, IReadOnlyDictionary<string, string>? args = null)
        : base(message ?? code)
    {
        Code = code;
        Args = args ?? new Dictionary<string, string>();
    }

    public static ApiException Create(string code, string? message = null, params (string Key, string Value)[] args)
    {
        var map = args.Length == 0
            ? new Dictionary<string, string>()
            : args.ToDictionary(x => x.Key, x => x.Value, StringComparer.Ordinal);
        return new ApiException(code, message, map);
    }
}
