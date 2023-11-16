using System.Text;

namespace Sisus.Debugging.Extensions
{
    internal static class StringBuilderExtensions
    {
        public static int IndexOf(this StringBuilder sb, char value)
        {
            for(int i = sb.Length - 1; i >= 0; i--)
            {
                if(sb[i] == value)
                {
                    return i;
                }
            }
            return -1;
        }

        public static bool Contains(this StringBuilder sb, char value)
        {
            for(int i = sb.Length - 1; i >= 0; i--)
            {
                if(sb[i] == value)
                {
                    return true;
                }
            }
            return false;
        }
    }
}