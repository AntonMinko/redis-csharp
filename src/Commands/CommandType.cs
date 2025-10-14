namespace codecrafters_redis.Commands;

public enum CommandType
{
    Unknown,
    Config,
    Echo,
    Get,
    Info,
    Keys,
    LPush,
    LRange,
    Ping,
    ReplConf,
    RPush,
    PSync,
    Set,
    Wait,
}