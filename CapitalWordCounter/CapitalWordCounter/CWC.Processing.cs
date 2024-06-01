using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace CapitalWordCounter
{
    partial class CWC_Server
    {  
    //====================================================================================================================================
    // *** CWC SERVER PROCESSING FUNCTIONS *** //
    //====================================================================================================================================
    //
    //
        //=============================================================================================================
        /// <summary>
        /// Processes incoming HTTP GET requests, generating appropriate responses based on the request URL path.
        /// </summary>
        /// <param name="state">The HTTP context representing the incoming request.</param>
        //=============================================================================================================
        private async Task ProcessRequestAsync(HttpListenerContext context)
        {
            HttpListenerRequest request = context.Request;
            HttpListenerResponse response = context.Response;

            if (request.HttpMethod != "GET")
            {
                response.StatusCode = (int)HttpStatusCode.MethodNotAllowed;
                response.Close();
                Console.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] : Received non-GET request: {request.HttpMethod} {request.Url}");
                return;
            }

            if (request.Url.AbsolutePath == "/favicon.ico")
            {// to get rid of the annoying favicon request...
                response.StatusCode = (int)HttpStatusCode.Forbidden;
                response.Close();
                Console.WriteLine("  # An request was invalid: GET /favicon.ico - 403 Forbidden");
                return;
            }

            Console.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] : New request received: GET {request.Url}");

            if (request.Url.AbsolutePath == "/")
            {// to show "homepage"
                byte[] buffer = Encoding.UTF8.GetBytes(_homepageFrontend);
                response.ContentType = "text/html";
                response.ContentLength64 = buffer.Length;
                await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);
                Console.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] : Response sent successfully for request: GET {request.Url}");
            }
            else
            {// to attempt to process the request received in the right format
                string urlQuery = request.Url.AbsolutePath;

                if (IsSinglePath(urlQuery) && Path.GetExtension(urlQuery.TrimStart('/')).Equals(".txt", StringComparison.OrdinalIgnoreCase))
                {// only requests that have a file name with .txt extension in their query will be actually processed

                    string fileName = urlQuery.TrimStart('/');

                    try
                    {
                        string wordCount = await _cache.GetOrCreateAsync(fileName, RootFolder, CountCapitalWordsAsync);

                        response.ContentType = "text/html";
                        response.StatusCode = 200;

                        string responseBody = GenerateResponse(fileName, wordCount);

                        byte[] buffer = Encoding.UTF8.GetBytes(responseBody);
                        response.ContentLength64 = buffer.Length;
                        await response.OutputStream.WriteAsync(buffer, 0, buffer.Length);

                        Console.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] : Response sent successfully for request: GET {request.Url}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"  # Error processing request: {ex.Message}");
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                    }
                }
                else
                {
                    response.StatusCode = (int)HttpStatusCode.BadRequest;
                    Console.WriteLine($"  # Bad request received: GET {request.Url} - 400 Bad Request");
                }
            }
            response.Close();
        }


        //=======================================================================================
        /// <summary>
        /// Determines whether the URL path consists of a single segment.
        /// </summary>
        /// <param name="url">The URL path to evaluate.</param>
        /// <returns>True if the URL path contains only one segment; otherwise, false.</returns>
        //=======================================================================================
        private bool IsSinglePath(string url)
        {
            int slashCount = 0;

            foreach (char c in url)
            {
                if (c == '/')
                {
                    slashCount++;

                    if (slashCount > 1)
                    {
                        return false;
                    }
                }
            }

            return slashCount == 1;
        }


        //====================================================================================================
        /// <summary>
        /// Searches for a specified file within the given root directory and its subdirectories.
        /// </summary>
        /// <param name="root">The root directory to start the search from.</param>
        /// <param name="fileName">The name of the file to search for.</param>
        /// <returns>The full path of the first occurrence of the file found, or null if not found.</returns>
        //====================================================================================================
        public static async Task<string> SearchFileAsync(string root, string fileName)
        {
            Stopwatch swSearchFile = new Stopwatch();
            swSearchFile.Start();

            string filePath = await Task.Run(() => SearchFile(root, fileName));

            swSearchFile.Stop();
            Console.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] : File [{fileName}] found in {swSearchFile.ElapsedMilliseconds}ms");

            return filePath;
        }


        private static string SearchFile(string root, string fileName)
        {
            var files = new ConcurrentBag<string>();
            var parallelOptions = new ParallelOptions
            {
                MaxDegreeOfParallelism = Environment.ProcessorCount
            };

            try
            {
                Parallel.ForEach(Directory.EnumerateFileSystemEntries(root, "*", SearchOption.AllDirectories), parallelOptions, (entry, state) =>
                {
                    if (files.IsEmpty)
                    {
                        if (Directory.Exists(entry))
                        {
                            var foundFiles = Directory.EnumerateFiles(entry, fileName, SearchOption.TopDirectoryOnly);
                            foreach (var file in foundFiles)
                            {
                                files.Add(file);
                                state.Stop(); // stops the search if the file is found
                            }
                        }
                        else if (File.Exists(entry) && Path.GetFileName(entry).Equals(fileName, StringComparison.OrdinalIgnoreCase))
                        {
                            files.Add(entry);
                            state.Stop(); // stops the search if the file is found
                        }
                    }
                });
            }
            catch (UnauthorizedAccessException) { }
            catch (DirectoryNotFoundException) { }
            catch (IOException) { }

            return files.FirstOrDefault();
        }


        //==================================================================================================================
        /// <summary>
        /// Counts the words in a text file that begin with a capital letter and have more than 5 letters.
        /// </summary>
        /// <remarks>
        /// Implementation: Creates tasks, with each task assigned to count words in a different paragraph of the text file.
        /// </remarks>
        /// <param name="filePath">The path of the file to analyze.</param>
        /// <returns>A string representing the total count of words meeting the specified criteria in the file.</returns>
        //===================================================================================================================
        private async Task<string> CountCapitalWordsAsync(string filePath)
        {
            Stopwatch swWordCount = Stopwatch.StartNew();

            int totalCount = 0;

            try
            {
                using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                {
                    string paragraph = string.Empty;
                    List<Task<int>> tasks = new List<Task<int>>();

                    while (!streamReader.EndOfStream)
                    {
                        string line = await streamReader.ReadLineAsync();

                        if (string.IsNullOrWhiteSpace(line))
                        {
                            if (!string.IsNullOrWhiteSpace(paragraph))
                            {
                                tasks.Add(ProcessParagraphAsync(paragraph));
                            }
                            paragraph = string.Empty;
                        }
                        else
                        {
                            paragraph += line + " ";
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(paragraph))
                    {// processes the last paragraph if not empty
                        tasks.Add(ProcessParagraphAsync(paragraph));
                    }

                    await Task.WhenAll(tasks);

                    totalCount = tasks.Sum(t => t.Result);
                    tasks.Clear();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  x Error reading file: {ex.Message}");
                return null;
            }

            swWordCount.Stop();
            Console.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] : Counting done in {swWordCount.ElapsedMilliseconds}ms for [{Path.GetFileName(filePath)}] --> Word Count: {totalCount}");

            return totalCount.ToString();
        }


        private Task<int> ProcessParagraphAsync(string paragraph)
        {
            return Task.Run(() =>
            {
                int localCount = 0;
                string[] words = paragraph.Split(new char[] { ' ', '\t', '.', ',', ';', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (string word in words)
                {
                    if (char.IsUpper(word[0]) && word.Length > 5)
                    {
                        localCount++;
                    }
                }
                return localCount;
            });
        }
        //
        //
        //====================================================================================================================================
        // *** END *** //
        //====================================================================================================================================
    }
}
