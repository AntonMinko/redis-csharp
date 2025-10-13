namespace codecrafters_redis.Commands;

public enum CommandType
{
    Unknown,
    Ping,
    Echo,
    Get,
    Set,
    Config,
    Keys,
    Info,
    RPush,
    LRange,
    ReplConf,
    PSync,
    Wait,
}