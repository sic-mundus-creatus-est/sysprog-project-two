using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace CapitalWordCounter
{
    public struct CacheItem
    {
        public string WordCount { get; set; }
        public string FilePath { get; set; }

        public CacheItem(string value, string filePath)
        {
            WordCount = value;
            FilePath = filePath;
        }
    }

    public class CWC_Cache
    {
        //=============================================================================
        // *** CWC CACHE ATTRIBUTES *** //
        //=============================================================================
        private readonly int _capacity;
        private readonly Dictionary<string, LinkedListNode<CacheItem>> _cacheMap;
        private readonly LinkedList<CacheItem> _lruList;
        private readonly SemaphoreSlim _cacheLock = new SemaphoreSlim(1, 1);

        //==================================================================
        // *** CWC CACHE CONSTRUCTOR *** //
        //==================================================================
        /// <summary>
        /// CapitalWordCounter Cache Constructor.
        /// </summary>
        //====================-----------------------=======================
        public CWC_Cache(int capacity)
        {
            try
            {
                if (capacity <= 0)
                    throw new ArgumentException("  !!! The capacity of cache must be set to a value greater than zero.");

                _capacity = capacity;
                _cacheMap = new Dictionary<string, LinkedListNode<CacheItem>>();
                _lruList = new LinkedList<CacheItem>();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                Console.WriteLine("=> The capacity of cache was set to a default value of 1024.");
                _capacity = 1024;
            }
        }

        //====================================================================================================================================
        // *** CWC CACHE MANAGEMENT FUNCTIONS *** //
        //====================================================================================================================================
        //
        //
        //===============================================================================================================================
        /// <summary>
        /// Attempts to retrieve the word count value for the specified file from the cache using the <see cref="GetAsync"/> method.<br />
        /// If the file is not found in the cache, it creates a new cache item for the file using the <see cref="CreateAsync"/> method.
        /// </summary>
        /// <param name="fileName">The name of the file to retrieve or create the cache item for.</param>
        /// <param name="root">The root directory of the server files.</param>
        /// <param name="createItem">The function used to generate the word count value for the file if it needs to be created.</param>
        /// <returns>The word count value for the specified file.</returns>
        //===============================================================================================================================
        public async Task<string> GetOrCreateAsync(string fileName, string root, Func<string, Task<string>> createItem)
        {
            await _cacheLock.WaitAsync();
            try
            {
                if (_cacheMap.ContainsKey(fileName))
                {
                    return await GetAsync(fileName);
                }
                else
                {
                    return await CreateAsync(fileName, root, createItem);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  x Error in GetOrCreateAsync for file [{fileName}]: {ex.Message}");
                throw;
            }
            finally
            {
                _cacheLock.Release();
            }
        }

        //=======================================================================================================================
        /// <summary>
        /// Attempts to retrieve the cached word count value for the specified file.
        /// </summary>
        /// <param name="fileName">The name of the file to retrieve the word count for.</param>
        /// <returns>The word count value if the file is found in the cache, otherwise throws a KeyNotFoundException.</returns>
        //=======================================================================================================================
        private Task<string> GetAsync(string fileName)
        {
            try
            {
                if (_cacheMap.TryGetValue(fileName, out LinkedListNode<CacheItem> curnode))
                {
                    SetToMostRecent(curnode);
                    Console.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] : Response retrieved from cache for: [{fileName}]");
                    return Task.FromResult(curnode.Value.WordCount);
                }
                else
                {
                    throw new KeyNotFoundException($"  x Value for key [{fileName}] not found!");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  x Error in GetAsync for file [{fileName}]: {ex.Message}");
                throw;
            }
        }

        //========================================================================================================================
        /// <summary>
        /// Creates a new cache item for the specified file if it doesn't exist in the cache, and retrieves its word count value.
        /// </summary>
        /// <param name="fileName">The name of the file to create the cache item for.</param>
        /// <param name="root">The root directory of the server files.</param>
        /// <param name="createItem">The function used to generate the word count value for the file.</param>
        /// <returns>The word count value for the specified file.</returns>
        //========================================================================================================================
        private async Task<string> CreateAsync(string fileName, string root, Func<string, Task<string>> createItem)
        {
            try
            {
                if (_cacheMap.Count >= _capacity)
                {
                    EvictItem();
                }

                string filePath = await CWC_Server.SearchFileAsync(root, fileName);

                if (filePath == null)
                {
                    throw new FileNotFoundException($"  # File [{fileName}] not found in root directory [{root}].");
                }

                string wordCount = await createItem(filePath);

                LinkedListNode<CacheItem> newNode = new LinkedListNode<CacheItem>(new CacheItem(wordCount, filePath));
                _lruList.AddFirst(newNode);
                _cacheMap[fileName] = newNode;

                Console.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] : Added new item to cache > [File Path: {filePath}]");
                Console.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] : New number of items in cache map: {_cacheMap.Count}/{_capacity}; New number of items in LRU list: {_lruList.Count}");

                return wordCount;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  x Error creating cache item for file [{fileName}]: {ex.Message}");
                throw;
            }
        }

        //===========================================================================
        /// <summary>
        /// Moves the specified node to the most recent position in the LRU list.
        /// </summary>
        /// <param name="node">The node to be moved.</param>
        //===========================================================================
        private void SetToMostRecent(LinkedListNode<CacheItem> node)
        {
            if (_lruList.First != node)
            {
                _lruList.Remove(node);
                _lruList.AddFirst(node);
            }
        }

        //===========================================================================
        /// <summary>
        /// Evicts the least recently used item from the cache.
        /// </summary>
        //===========================================================================
        private void EvictItem()
        {
            LinkedListNode<CacheItem>? nodeToRemove = _lruList.Last;

            if (nodeToRemove != null)
            {
                _cacheMap.Remove(Path.GetFileName(nodeToRemove.Value.FilePath));
                _lruList.RemoveLast();
                Console.WriteLine($"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] : Evicted an item from cache > [[File Path: {nodeToRemove.Value.FilePath}]");
            }
        }

        //===========================================================================
        /// <summary>
        /// Checks if the specified key exists in the cache.
        /// </summary>
        /// <param name="key">The key to check for existence.</param>
        /// <returns>True if the key exists in the cache, otherwise false.</returns>
        //===========================================================================
        public bool ExistsInCache(string key)
        {
            return _cacheMap.ContainsKey(key);
        }

        //====================================================================================================================================
        // *** END *** //
        //====================================================================================================================================
    }
}
