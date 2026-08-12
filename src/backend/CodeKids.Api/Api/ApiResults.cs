using CodeKids.Application.Common;

namespace CodeKids.Api;

public static class ApiResults

{

    public static IResult ProblemFromException(Exception ex)

    {

        if (ex is ApiException api)

        {

            return Results.BadRequest(new { code = api.Code, message = api.Message, args = api.Args });

        }

        var resolved = ApiErrorCatalog.TryResolve(ex.Message);

        if (resolved is not null)

        {

            return Results.BadRequest(new { code = resolved.Value.Code, message = ex.Message, args = resolved.Value.Args });

        }

        return Results.BadRequest(new { code = "api.errors.unknown", message = ex.Message });

    }

}
