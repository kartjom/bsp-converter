namespace PrimitiveTypeExtensions
{
    public static class CharArrayExtension
    {
        public static string String(this char[] value) => new string(value).Trim('\0');
        public static string String(this byte[] value) => System.Text.Encoding.UTF8.GetString(value).Trim('\0');
    }
}
