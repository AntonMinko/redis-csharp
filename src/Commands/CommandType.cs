// ReSharper disable InconsistentNaming
namespace codecrafters_redis.Commands;

public enum CommandType
{
    Unknown,
    
    Get,
    Set,
    Keys,

    Ping,
    Echo,
    
    Config,
    Info,
    
    ReplConf,
    PSync,
    Wait,
    
    LLen,
    LPush,
    RPush,
    LRange,
    LPop,
    BLPop,
    
    Subscribe,
    Publish,
    Unsubscribe,
}