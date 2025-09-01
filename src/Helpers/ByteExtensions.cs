namespace codecrafters_redis.Helpers;

public static class ByteExtensions
{
    public static byte[] Concat(this byte[] left, byte[] right)
    {
        var result = new byte[left.Length + right.Length];
        Buffer.BlockCopy(left, 0, result, 0, left.Length);
        Buffer.BlockCopy(right, 0, result, left.Length, right.Length);
        return result;
    }
}