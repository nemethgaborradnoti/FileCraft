namespace FileCraft.Shared.Helpers
{
    public static class IgnoreCommentsHelper
    {
        public static int FindActualCommentIndex(string line)
        {
            bool inDoubleQuotes = false;
            bool inSingleQuotes = false;

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"' && (i == 0 || line[i - 1] != '\\'))
                {
                    inDoubleQuotes = !inDoubleQuotes;
                }
                else if (c == '\'' && (i == 0 || line[i - 1] != '\\'))
                {
                    inSingleQuotes = !inSingleQuotes;
                }

                if (!inDoubleQuotes && !inSingleQuotes)
                {
                    if (i + 1 < line.Length && line[i] == '/' && line[i + 1] == '/')
                    {
                        if (i > 0 && line[i - 1] == ':')
                        {
                            continue;
                        }
                        return i;
                    }
                }
            }
            return -1;
        }

        public static (bool IsXmlComment, int CommentLength) CalculateXmlCommentStats(string line)
        {
            int commentIndex = FindActualCommentIndex(line);

            if (commentIndex != -1)
            {
                if (commentIndex + 2 < line.Length && line[commentIndex + 2] == '/')
                {
                    return (true, line.Length - commentIndex);
                }
            }

            return (false, 0);
        }
    }
}