namespace AITeam.Hub.Services;

/// <summary>R-015 (Claude): các trang đã bỏ chuyển (302) về trang thay thế, để dấu trang cũ không ra 404.
/// Chỉ GET, đúng đường dẫn (không phân biệt hoa thường), bỏ query; đường dẫn khác chuyển next nguyên vẹn.</summary>
public sealed class LegacyPageRedirectMiddleware(RequestDelegate next)
{
    public static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        ["/Overview"] = "/",
        ["/Work"] = "/",
        ["/Performance"] = "/Reports",
        ["/Team"] = "/Reports?tab=people",
        ["/AI"] = "/AiServices",
        ["/Accounts"] = "/AiServices",
        ["/Providers"] = "/AiServices",
        ["/Process"] = "/Reports/Lessons",
        ["/Map"] = "/Workflow",
    };

    public Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;
        if (HttpMethods.IsGet(context.Request.Method) && path != null && Map.TryGetValue(path, out var target))
        {
            context.Response.StatusCode = StatusCodes.Status302Found;
            context.Response.Headers.Location = target;
            return Task.CompletedTask;
        }

        return next(context);
    }
}
