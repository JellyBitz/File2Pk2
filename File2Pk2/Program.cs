using SRO.PK2;

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace File2Pk2
{
    internal class Program
    {
        #region App Setup
        private static string mClientPath = string.Empty;
        private static string mPk2Key = "169841";
        private static List<string> mFiles = new List<string>();
        private static bool mOverrideOnly = false;
        #endregion
        /// <summary>
        /// Application entry point.
        /// </summary>
        private static void Main(string[] args)
        {
            Console.Title = $"File2Pk2 v" + FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location).FileVersion + " - https://github.com/JellyBitz/File2Pk2";
            Console.WriteLine(Console.Title + Environment.NewLine);

            LoadCommandLine(args);
            // Check if the minimum has been set up
            if (mFiles.Count == 0)
            {
                DisplayUsage();
                return;
            }

            // Set current path as client folder
            if (mClientPath == string.Empty)
            {
                Console.WriteLine(" (!) Client path hasn't been specified, using the current path as client." + Environment.NewLine);
                mClientPath = Path.GetDirectoryName(Process.GetCurrentProcess().MainModule.FileName);
            }

            // Check all .pk2 files that will be available to import
            foreach (var pk2Path in Directory.GetFiles(mClientPath, "*.pk2"))
            {
                var pk2Name = Path.GetFileNameWithoutExtension(pk2Path).ToLowerInvariant();

                Pk2Stream pk2 = null;
                try
                {
                    // Check for files that belongs to this pk2
                    // Order doesn't matter, just to remove them easier after every import
                    for (int i = mFiles.Count - 1; i >= 0; i--)
                    {
                        var file = mFiles[i];

                        // Check folder hierarchy from file to find the right path if possible
                        string[] pathSegments = Path.GetDirectoryName(file).Split(new[] { Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar }, StringSplitOptions.RemoveEmptyEntries);
                        string filePk2Path = null;

                        // Order does matter, last segment (folder) has priority
                        for (int j = pathSegments.Length - 1; j >= 0; j--)
                        {
                            if (pathSegments[j].Equals(pk2Name, StringComparison.OrdinalIgnoreCase))
                            {
                                filePk2Path = string.Join(Path.DirectorySeparatorChar.ToString(), pathSegments, j + 1, pathSegments.Length - j - 1);
                                // Add filename
                                filePk2Path = Path.Combine(filePk2Path, Path.GetFileName(file));
                                break;
                            }
                        }

                        // This file belons to this .pk2
                        if (filePk2Path != null)
                        {
                            // Open pk2 when it's required
                            if (pk2 == null)
                            {
                                pk2 = new Pk2Stream(pk2Path, mPk2Key);
                                Console.WriteLine($"Importing files into {Path.GetFileName(pk2Path)}..." + Environment.NewLine);
                            }

                            // Check if the file exists as requirement to be override
                            if (mOverrideOnly)
                            {
                                if(pk2.GetFile(filePk2Path) == null)
                                    continue;
                            }

                            // Load file and add it
                            pk2.AddFile(filePk2Path, File.ReadAllBytes(file));
                            Console.WriteLine(" Imported: " + filePk2Path);

                            // Remove file from further checks
                            mFiles.RemoveAt(i);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                    DisplayPause();
                }
                finally
                {
                    pk2?.Dispose();
                }
            }
        }
        /// <summary>
        /// Read and loads data from the command line arguments.
        /// </summary>
        private static void LoadCommandLine(string[] args)
        {
            foreach (var arg in args)
            {
                var cmd = arg.ToLowerInvariant();
                // Check commands
                if (cmd.StartsWith("-client="))
                {
                    var path = arg.Substring("-client=".Length);
                    // Make sure it exists
                    if (Directory.Exists(path))
                        mClientPath = path;
                }
                else if (cmd.StartsWith("-key="))
                {
                    mPk2Key = arg.Substring("-key=".Length);
                }
                else if (cmd.StartsWith("--override-only"))
                {
                    mOverrideOnly = true;
                }
                else
                {
                    // Check if argument is path to a file
                    if (File.Exists(arg))
                        mFiles.Add(arg);
                }
            }
        }
        /// <summary>
        /// Shows a quick info about command line usage.
        /// </summary>
        private static void DisplayUsage()
        {
            // Short description
            Console.WriteLine("Import file(s) right into the .pk2 file by autoselecting their respective folder.");
            Console.WriteLine("All this with just drag and drop the file into the application.");
            Console.WriteLine();
            Console.WriteLine("File2Pk2 \"-client=C:\\Games\\Silkroad\" \"-key=169841\"");
            Console.WriteLine();
            Console.WriteLine(" -client= : Path to the client to import the folder");
            Console.WriteLine(" -key= : Encryption key used by the .pk2 file");
            Console.WriteLine(" --override-only : Confirm the file needs to exists before importing");
            Console.WriteLine();
            DisplayPause();
        }
        /// <summary>
        /// Mimic classic System("pause") from C++.
        /// </summary>
        private static void DisplayPause()
        {
            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
