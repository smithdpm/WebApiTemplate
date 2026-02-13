using Ardalis.Result;

namespace Cqrs.Extensions;

public static class IResultExtensnions
{
    public static bool IsSuccess(this IResult result) => result.Status is ResultStatus.Ok or ResultStatus.NoContent or ResultStatus.Created;

}