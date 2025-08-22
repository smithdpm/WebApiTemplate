namespace SharedKernel;

public sealed record ValidationError: Error
{
    public Error[] Errors { get; init; }
    public ValidationError(Error[] errors) 
        : base("ValidationError", "One or more validation errors has occurred", ErrorType.Validation)
    {
        Errors = errors ?? throw new ArgumentNullException(nameof(errors), "Errors cannot be null");
    }

}
