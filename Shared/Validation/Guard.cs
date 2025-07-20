using System;
using System.IO;

namespace FileCraft.Shared.Validation
{
    public static class Guard
    {
        public static void AgainstNull(object value, string paramName, string message = "Value cannot be null.")
        {
            if (value == null)
            {
                throw new ArgumentNullException(paramName, message);
            }
        }

        public static void AgainstNullOrWhiteSpace(string value, string paramName, string message = "String cannot be null or whitespace.")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                throw new ArgumentException(message, paramName);
            }
        }

        public static void AgainstNonExistentDirectory(string path, string message = "The specified directory does not exist.")
        {
            if (!Directory.Exists(path))
            {
                throw new DirectoryNotFoundException($"{message}\nPath: '{path}'");
            }
        }
    }
}
