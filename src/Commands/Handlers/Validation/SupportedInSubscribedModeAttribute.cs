namespace codecrafters_redis.Commands.Handlers.Validation;

internal class SupportedInSubscribedModeAttribute : Attribute
{
    public bool IsSupported { get; set; }
}