

namespace SharedKernel.Extensions;
public static class EnumerableExtensions
{
    /// <summary>
    /// Projects each function in a sequence, evaluates it with the given input, 
    /// and returns the result, filtering out any null results.
    /// This overload is for reference types.
    /// </summary>
    /// <typeparam name="TIn">The type of the input parameter.</typeparam>
    /// <typeparam name="TOut">The type of the output elements (must be a class).</typeparam>
    /// <param name="funcs">A sequence of functions to evaluate.</param>
    /// <param name="input">The input object to pass to each function.</param>
    /// <returns>A sequence of non-null results of type TOut.</returns>
    public static IEnumerable<TOut> SelectNotNull<TIn, TOut>(this IEnumerable<Func<TIn, TOut?>> funcs, TIn input)
    where TOut : class
    {
        return funcs.Select(func => func(input))
            .Where(x => x != null)!;
    }

    /// <summary>
    /// Projects each function in a sequence, evaluates it with the given input, 
    /// and returns the result, filtering out any null results.
    /// This overload is for nullable value types (structs).
    /// </summary>
    /// <typeparam name="TIn">The type of the input parameter.</typeparam>
    /// <typeparam name="TOut">The type of the output elements (must be a struct).</typeparam>
    /// <param name="funcs">A sequence of functions to evaluate.</param>
    /// <param name="input">The input object to pass to each function.</param>
    /// <returns>A sequence of non-null results of type TOut.</returns>
    public static IEnumerable<TOut> SelectNotNull<TIn, TOut>(this IEnumerable<Func<TIn, TOut?>> funcs, TIn input)
        where TOut : struct
    {
        return funcs.Select(func => func(input))
            .Where(result => result.HasValue)
            .Select(result => result!.Value);
    }
}
