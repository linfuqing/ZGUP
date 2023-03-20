using System.Text.RegularExpressions;

namespace ZG
{
    public struct JsonObject
    {
        public string value;

        public bool isEmpty
        {
            get
            {
                return Regex.IsMatch(value, "\\{\\s*\\}");
            }
        }

        public JsonObject this[string x]
        {
            get
            {
                return new JsonObject(Get(value, x));
            }
        }

        public JsonObject(string value)
        {
            this.value = value;
        }

        public static string Get(string text, string key)
        {
            var match = Regex.Match(text, $"\\{{[\\s\\S]*\"{key}\"\\s*:\\s*\\{{");
            if (!match.Success)
                return null;

            if (!IndexOf(text, '{', '}', 0, match.Index + match.Length - 1, out int indexX, out int indexY))
                return null;

            return text.Substring(indexX, indexY - indexX + 1);
        }

        public static bool IndexOf(string text, char x, char y, int count, int index, out int indexX, out int indexY)
        {
            indexY = count > 0 ? text.IndexOf(y, index, count) : text.IndexOf(y, index);
            if(indexY == -1)
            {
                indexX = -1;

                return false;
            }

            indexX = text.IndexOf(x, index, indexY - index);
            if (indexX == -1)
                return false;

            int newIndex = indexX + 1;
            if (newIndex < indexY)
            {
                newIndex = text.IndexOf(x, newIndex, indexY - newIndex);
                if (newIndex != -1 && IndexOf(text, x, y, count > 0 ? count + index - newIndex : 0, newIndex, out _, out int newIndexY))
                    indexY = text.IndexOf(y, newIndexY + 1);
            }

            return indexY != -1;
        }
    }
}