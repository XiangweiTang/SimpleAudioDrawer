using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace SimpleAudioDrawer
{
    /// <summary>
    /// The class for sanity check.
    /// </summary>
    static class Sanity
    {
        /// <summary>
        /// If the condition is not matched, throw an MtInfrastructureException.
        /// </summary>
        /// <param name="valid">The testify condition</param>
        /// <param name="message">The error message</param>
        public static void Requires(bool valid, string message)
        {
            if (!valid)
                throw new MtInfrastructureException(message);
        }

        /// <summary>
        /// If the condition is not matched, throw an MtInfrastructureException.
        /// </summary>
        /// <param name="valid">The testify condition</param>
        public static void Requires(bool valid)
        {
            Requires(valid, "MtInfrastructureException.");
        }

        /// <summary>
        /// If the condition for Wave is not matched, throw an MtInfrastructureException.
        /// </summary>
        /// <param name="valid">The testify condition</param>
        /// <param name="message">The error message body.</param>
        public static void RequiresWave(bool valid, string message)
        {
            if (!valid)
                throw new MtInfrastructureException("Wave format error:\t" + message);
        }

        /// <summary>
        /// If the condition for Wave is not matched, throw an MtInfrastructureException.
        /// </summary>
        /// <param name="valid">The testify condition</param>
        public static void RequiresWave(bool valid)
        {
            RequiresWave(valid, "Wave error.");
        }

        /// <summary>
        /// If the file path does not exist, throw an MtInfrastructureException.
        /// </summary>
        /// <param name="filePath">The file path needs to be checked.</param>
        /// <param name="message">The error message.</param>
        public static void RequiresFileExists(string filePath, string message)
        {
            Requires(File.Exists(filePath), message);
        }

        /// <summary>
        /// If the file path does not exist, throw an MtInfrastructureException.
        /// </summary>
        /// <param name="filePath">The file path needs to be checked.</param>
        public static void RequiresFileExists(string filePath)
        {
            RequiresFileExists(filePath, $"File path does not exist:\t" + filePath);
        }

        /// <summary>
        /// If the folder path does not exist, throw an MtInfrastructureException.
        /// </summary>
        /// <param name="folderPath">The folder path needs to be checked.</param>
        /// <param name="message">The error message.</param>
        public static void RequiresFolderExists(string folderPath, string message)
        {
            Requires(Directory.Exists(folderPath), message);
        }

        /// <summary>
        /// If the folder path does not exist, throw an MtInfrastructureException.
        /// </summary>
        /// <param name="folderPath">The folder path needs to be checked.</param>
        public static void RequiresFolderExists(string folderPath)
        {
            RequiresFolderExists(folderPath, $"Folder path does not exist:\t" + folderPath);
        }

        /// <summary>
        /// If the line format is invalid, throw an MtInfrastructureException.
        /// </summary>
        /// <param name="valid">If the line format is valid.</param>
        /// <param name="lineType">The name of the line type.</param>
        public static void RequiresLineFormat(bool valid, string lineType)
        {
            Requires(valid, $"Invalid line format: {lineType}.");
        }

        /// <summary>
        /// If the condition is not matched, give a warning
        /// </summary>
        /// <param name="valid">The testify condition</param>
        /// <param name="message">The error message</param>
        public static void BeBetter(bool valid, string message)
        {
            if (!valid)
            {
                string fullMessage = "Warning: " + message;
                Logger.WriteLineWithLock(fullMessage);
            }
        }

        /// <summary>
        /// If the condition is not matched, give a warning.
        /// </summary>
        /// <param name="valid">The testify condition</param>
        public static void BeBetter(bool valid)
        {
            BeBetter(valid, "Something is wrong here, may not affect the task.");
        }
    }

    /// <summary>
    /// The MtInfrastructureException, inherit the Exception class.
    /// </summary>
    class MtInfrastructureException : Exception
    {
        public MtInfrastructureException() : base() { }
        public MtInfrastructureException(string message) : base(message) { }
    }
}
