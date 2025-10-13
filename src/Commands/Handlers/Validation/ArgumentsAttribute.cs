namespace codecrafters_redis.Commands.Handlers.Validation;

public class ArgumentsAttribute : Attribute
{
    public int Min { get; set; } = 0;
    public int Max { get; set; } = int.MaxValue;
}