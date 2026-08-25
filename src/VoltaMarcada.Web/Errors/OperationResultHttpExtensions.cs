using Microsoft.AspNetCore.Mvc;
using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Web.Errors;

public static class OperationResultHttpExtensions
{
    public static IResult ToHttpResult(
        this OperationResult result,
        Func<IResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess();
        }

        return ToProblem(result.Errors);
    }

    public static IResult ToHttpResult<TValue>(
        this OperationResult<TValue> result,
        Func<TValue, IResult> onSuccess)
    {
        if (result.IsSuccess)
        {
            return onSuccess(result.Value!);
        }

        return ToProblem(result.Errors);
    }

    public static IResult ValidationProblem(string code, string message)
    {
        var problem = new ProblemDetails
        {
            Status = StatusCodes.Status400BadRequest,
            Title = message
        };
        problem.Extensions["code"] = code;
        return Results.Problem(problem);
    }

    private static IResult ToProblem(IReadOnlyList<OperationError> errors)
    {
        if (errors.Count == 0)
        {
            var unexpectedProblem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = OperationErrorMessages.GetMessage(null)
            };
            unexpectedProblem.Extensions["code"] = "General.Unexpected";
            return Results.Problem(unexpectedProblem);
        }

        var error = errors[0];
        var problem = new ProblemDetails
        {
            Status = GetStatusCode(error.Type),
            Title = OperationErrorMessages.GetMessage(error.Code)
        };
        problem.Extensions["code"] = error.Code;

        var fieldErrors = errors
            .Where(item => !string.IsNullOrWhiteSpace(item.Field))
            .GroupBy(item => item.Field!, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(item => OperationErrorMessages.GetMessage(item.Code)).ToArray(),
                StringComparer.Ordinal);

        if (fieldErrors.Count > 0)
        {
            problem.Extensions["errors"] = fieldErrors;
        }

        return Results.Problem(problem);
    }

    private static int GetStatusCode(OperationErrorType type) => type switch
    {
        OperationErrorType.Validation => StatusCodes.Status400BadRequest,
        OperationErrorType.NotFound => StatusCodes.Status404NotFound,
        OperationErrorType.Forbidden => StatusCodes.Status403Forbidden,
        OperationErrorType.Conflict => StatusCodes.Status409Conflict,
        _ => StatusCodes.Status500InternalServerError
    };
}
