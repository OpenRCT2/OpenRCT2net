using System;
using System.Text;
using OpenRCT2.Core;

namespace OpenRCT2
{
    public class OpenRCT2String : IOpenRCT2String
    {
        public string Raw { get; }

        public OpenRCT2String(string s)
        {
            Raw = s ?? String.Empty;
        }

        public override int GetHashCode()
        {
            return Raw.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            var other = obj as OpenRCT2String;
            if (other != null)
            {
                return Raw == other.Raw;
            }
            return false;
        }

        public override string ToString()
        {
            return StripFormatCodes();
        }

        private string StripFormatCodes()
        {
            var sb = new StringBuilder();
            foreach (char c in Raw)
            {
                if (!IsFormatCode(c))
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        public static bool IsFormatCode(char c)
        {
            if (c == 11) return true;
            if (IsColourCode(c)) return true;
            return false;
        }

        public static bool IsColourCode(char c)
        {
            return c >= FormatCodes.Black && c <= FormatCodes.PaleSilver;
        }
    }
}
