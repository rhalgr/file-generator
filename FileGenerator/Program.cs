using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace FileGenerator
{
    public static class Program
    {
        private static readonly Regex fileExtensionRegex = new Regex(@"^\.(txt|TXT|jpg|JPG|gif|GIF|doc|DOC|pdf|PDF)$");

        public static void Main(string[] args)
        {
            var minFileSizeInMegabytes = 0;
            var maxFileSizeInMegabytes = 0;
            
            Console.WriteLine("Enter destination directory path:");
            var destinationDirectory = Console.ReadLine();

            Console.WriteLine("Enter file extension to use .(txt|TXT|jpg|JPG|gif|GIF|doc|DOC|pdf|PDF):");
            var fileExtension = Console.ReadLine();

            if (!fileExtension.StartsWith("."))
                fileExtension = $".{fileExtension}";

            if (!fileExtensionRegex.IsMatch(fileExtension))
            {
                Console.WriteLine("File extension is invalid. Files will be generated as .txt files");
                fileExtension = ".txt";
            }

            Console.WriteLine("Number of files to generate:");
            Int32.TryParse(Console.ReadLine(), out var numberOfFiles);

            if (numberOfFiles <= 0)
            {
                Console.WriteLine("Entered 0 or an invalid number of files, exiting.");
                return;
            }

            if (numberOfFiles == 1)
            {
                Console.WriteLine("Enter file size in MB:");
                Int32.TryParse(Console.ReadLine(), out maxFileSizeInMegabytes);
                minFileSizeInMegabytes = maxFileSizeInMegabytes;
            }
            else
            {
                Console.WriteLine("Enter min file size in MB:");
                Int32.TryParse(Console.ReadLine(), out minFileSizeInMegabytes);

                Console.WriteLine("Enter max file size in MB:");
                Int32.TryParse(Console.ReadLine(), out maxFileSizeInMegabytes);

                if (minFileSizeInMegabytes < 1)
                {
                    Console.WriteLine("Min file size less than 1. Setting at 1.");
                    minFileSizeInMegabytes = 1;
                }

                if (maxFileSizeInMegabytes < 1)
                {
                    Console.WriteLine("Max file size less than 1. Setting at 1.");
                    maxFileSizeInMegabytes = 1;
                }

                if (maxFileSizeInMegabytes < minFileSizeInMegabytes)
                {
                    Console.WriteLine("Max file size less than min. Inverting values.");
                    var swapFileSize = minFileSizeInMegabytes;
                    minFileSizeInMegabytes = maxFileSizeInMegabytes;
                    maxFileSizeInMegabytes = swapFileSize;
                }
            }

            try
            {
                Directory.CreateDirectory(destinationDirectory);
            }
            catch
            {
                Console.WriteLine("Unable to create directory specified. Creating at default location: \"C:\\_generated files\"");
                destinationDirectory = "C:\\_generated files";
                Directory.CreateDirectory(destinationDirectory);
            }

            Console.WriteLine();

            GenerateFiles(destinationDirectory, minFileSizeInMegabytes, maxFileSizeInMegabytes, numberOfFiles, fileExtension);
        }

        private static void GenerateFiles(string path, int minSize, int maxSize, int numberOfFiles, string fileExtension)
        {
            Console.WriteLine($"Generating {numberOfFiles} {fileExtension} file{(numberOfFiles > 1 ? "s" : "")} {(minSize != maxSize ? $"varying from {minSize} MB to {maxSize} MB" : $"of size {minSize}")} in the following directory: {path}");
            
            var maxParallelOps = 20;
            var errorMessages = new ConcurrentBag<string>();

            Task.Run(async () => 
            {
                using (var slim = new SemaphoreSlim(maxParallelOps))
                {
                    var generationTasks = new List<Task>();

                    for (var i = 0; i < numberOfFiles; i++)
                    {
                        await slim.WaitAsync();
                        generationTasks.Add(Task.Run(async () =>
                        {
                            await Task.Yield();
                            var fileName = Path.Combine(path, $"{DateTime.UtcNow:yyyy-MM-dd HH-mm-ss-fff}{fileExtension}");

                            try
                            {
                                var sleepRng = new Random();
                                // Sleep to give some variation to the file size since this is time-based
                                System.Threading.Thread.Sleep(sleepRng.Next(500, 1000));

                                var rng = new Random();
                                var sizeInMb = rng.Next(minSize, maxSize);

                                // Borrowed from https://stackoverflow.com/questions/4432178/creating-a-random-file-in-c-sharp
                                // Note: block size must be a factor of 1MB to avoid rounding errors :)
                                const int blockSize = 1024 * 8;
                                const int blocksPerMb = (1024 * 1024) / blockSize;
                                var data = new byte[blockSize];
                                using (var stream = File.Create(fileName, 4096, FileOptions.Asynchronous))
                                {
                                    // There 
                                    for (var k = 0; k < sizeInMb * blocksPerMb; k++)
                                    {
                                        rng.NextBytes(data);
                                        await stream.WriteAsync(data, 0, data.Length);
                                    }
                                }
                            }
                            catch
                            {
                                // We don't particularly care why generation failed. This can be added to if there is a desire to track exceptions
                                errorMessages.Add($"Failed to generate: {fileName}");
                            }
                            finally
                            {
                                slim?.Release();
                            }
                        }));
                    }

                    await Task.WhenAll(generationTasks);
                }
            }).GetAwaiter().GetResult();

            Console.WriteLine("File generation complete");

            if (errorMessages.Count > 0)
            {
                Console.WriteLine($"Errors were encountered during file generation. Failed to generate {errorMessages.Count} file{(errorMessages.Count > 1 ? "s" : "" )}");
            }

            Console.WriteLine("Press any key to continue...");
            Console.ReadKey();
        }
    }
}
