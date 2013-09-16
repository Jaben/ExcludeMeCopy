/* The MIT License (MIT)
 * 
 * Copyright (c) 2013 Jaben Cargman
 * 
 * Permission is hereby granted, free of charge, to any person obtaining a copy of
 * this software and associated documentation files (the "Software"), to deal in
 * the Software without restriction, including without limitation the rights to
 * use, copy, modify, merge, publish, distribute, sublicense, and/or sell copies of
 * the Software, and to permit persons to whom the Software is furnished to do so,
 * subject to the following conditions:
 * 
 * The above copyright notice and this permission notice shall be included in all
 * copies or substantial portions of the Software.
 * 
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS
 * FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR
 * COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER
 * IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN
 * CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
 */
namespace ExcludeMeCopy
{
    #region Using

    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;

    using Args;
    using Args.Help;
    using Args.Help.Formatters;

    #endregion

    /// <summary>
    /// The program.
    /// </summary>
    internal class Program
    {
        #region Methods

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Main(string[] args)
        {
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(new string('*', 60));
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.WriteLine("Exclude Me! Copy - Copyright 2013 Jaben Cargman");
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(new string('*', 60));
            Console.ResetColor();

            try
            {
                var definition = Configuration.Configure<CopyArguments>();
                CopyArguments copy = null;

                try
                {
                    copy = definition.CreateAndBind(args);
                }
                catch
                {

                    var help = new HelpProvider().GenerateModelHelp(definition);
                    var f = new ConsoleHelpFormatter(80, 1, 5);
                    f.WriteHelp(help, Console.Error);

                    return;
                }

                var excludeLike = new HashSet<string>();

                if (copy.IgnoreFile.IsSet())
                {
                    Console.WriteLine(@" - Loading Ignore File ""{0}""...", copy.IgnoreFile);
                    foreach (var line in File.ReadAllLines(copy.IgnoreFile).Where(x => x.IsSet()))
                    {
                        excludeLike.Add(line);
                    }
                }

                if (copy.Exclusions != null)
                {
                    foreach (var line in copy.Exclusions.Where(x => x.IsSet()))
                    {
                        excludeLike.Add(line);
                    }
                }

                if (!Path.IsPathRooted(copy.SourceDirectory))
                {
                    copy.SourceDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, copy.SourceDirectory));
                }

                if (!Path.IsPathRooted(copy.DestinationDirectory))
                {
                    copy.DestinationDirectory = Path.GetFullPath(Path.Combine(Environment.CurrentDirectory, copy.DestinationDirectory));
                }

                Console.WriteLine(@" - Source Directory is ""{0}""", copy.SourceDirectory);
                Console.WriteLine(@" - Destination Directory is ""{0}""", copy.DestinationDirectory);
                Console.WriteLine(@" - Directory Recursion is {0}", copy.Recurse ? "ON" : "OFF");

                if (!Directory.Exists(copy.SourceDirectory))
                {
                    Console.Beep();
                    Console.Error.WriteLine(@" ! Failure: Source directory ""{0}"" does not exist!", copy.SourceDirectory);
                    Console.WriteLine();
                    return;
                }

                Console.WriteLine();

                bool isCancelled = false;

                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    isCancelled = true;
                };

                var cancelSource = new CancellationTokenSource();
                var cancellationToken = cancelSource.Token;

                Task<int> copyTask = Task.Factory.StartNew(() => CopyStructure(copy, excludeLike.ToList(), token: cancellationToken), cancellationToken);

                while (!(copyTask.IsCompleted || copyTask.IsCanceled || copyTask.IsFaulted))
                {
                    if (isCancelled)
                    {
                        Console.WriteLine(" * Control-C Pressed. Cancelling!");
                        cancelSource.Cancel();
                    }

                    Thread.Sleep(50);
                }

                if (copyTask.Result > 0)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.WriteLine();
                    Console.WriteLine(@" * {0} File(s) Copied!", copyTask.Result);
                }
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!! Exception: " + ex);
            }
            finally
            {
                Console.ResetColor();
            }

            Console.WriteLine();
            Console.ForegroundColor = ConsoleColor.DarkCyan;
            Console.WriteLine(new string('*', 60));
            Console.ResetColor();
        }

        private static string GetDisplayPath(string path)
        {
            return path.Replace(Environment.CurrentDirectory, @".");
        }

        private static int CopyStructure(CopyArguments copy, IList<string> excludeLike, string currentDirectory = null, CancellationToken token = default(CancellationToken))
        {
            if (token.IsCancellationRequested)
            {
                return 0;
            }

            currentDirectory = currentDirectory ?? copy.SourceDirectory;

            var relativeDirectory = currentDirectory.Replace(copy.SourceDirectory, string.Empty);

            if (relativeDirectory.StartsWith(new string(Path.DirectorySeparatorChar, 1)))
            {
                relativeDirectory = relativeDirectory.Substring(1);
            }

            var destinationDirectory = Path.Combine(copy.DestinationDirectory, relativeDirectory);

            if (!Directory.Exists(destinationDirectory))
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(@" * Creating destination directory ""{0}"" as it does not exist...", GetDisplayPath(destinationDirectory));
                Directory.CreateDirectory(destinationDirectory);
            }

            int copyCount = 0;

            if (copy.Recurse)
            {
                foreach (var directory in Directory.GetDirectories(currentDirectory, "*.*", SearchOption.TopDirectoryOnly))
                {
                    if (!directory.Matches(excludeLike))
                    {
                        copyCount += CopyStructure(copy, excludeLike, directory, token);
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.DarkGray;
                        Console.WriteLine(@" * Directory ""{0}"" Ignored", GetDisplayPath(directory));
                    }
                }
            }

            if (token.IsCancellationRequested)
            {
                return 0;
            }

            // get all the files we want and loop through them))
            foreach (var file in Directory.GetFiles(currentDirectory, "*.*", SearchOption.TopDirectoryOnly))
            {
                if (!file.Matches(excludeLike))
                {
                    Copy(file, destinationDirectory);
                    copyCount++;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.DarkGray;
                    Console.WriteLine(@" * File ""{0}"" Ignored", GetDisplayPath(file));
                }
            }

            return copyCount;
        }

        private static void Copy(string file, string destinationDirectory)
        {
            var fileName = Path.GetFileName(file);
            var destFileName = Path.Combine(destinationDirectory, fileName);

            Console.ForegroundColor = ConsoleColor.White;
            Console.WriteLine(@" * Copying ""{0}"" to ""{1}""...", GetDisplayPath(file), GetDisplayPath(destFileName));

            File.Copy(file, destFileName, true);
        }

        #endregion
    }
}
