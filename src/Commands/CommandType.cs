namespace codecrafters_redis.Commands;

public enum CommandType
{
    Unknown,
    Config,
    Echo,
    Get,
    Info,
    Keys,
    LLen,
    LPop,
    LPush,
    LRange,
    Ping,
    ReplConf,
    RPush,
    PSync,
    Set,
    Wait,
}