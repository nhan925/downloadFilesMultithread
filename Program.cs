using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;

namespace DownloadingFileMiltithreading;

public class DownloadFileMultithreading
{
    public int chunkCount;
    public string url = String.Empty;
    public string filePath = String.Empty;
    static HttpClient client = new HttpClient();

    public DownloadFileMultithreading(int chunkCount, string url, string filePath)
    {
        this.chunkCount = chunkCount;
        this.url = url;
        this.filePath = filePath;
    }

    private void DownloadChunk(long start, long end, int chunkIndex)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, url);
        request.Headers.Range = new System.Net.Http.Headers.RangeHeaderValue(start, end);

        using (var response = client.SendAsync(request).Result)
        using (var stream = response.Content.ReadAsStreamAsync().Result)
        {
            string chunkPath = $"{filePath}.part{chunkIndex}";
            using (var fileStream = new FileStream(chunkPath, FileMode.Create, FileAccess.Write))
            {
                stream.CopyTo(fileStream);
            }
        }

        Console.WriteLine($"Chunk {chunkIndex} downloaded.");
    }

    public void DownloadFileInChunks()
    {
        var request = new HttpRequestMessage(HttpMethod.Head, url);

        using (var response = client.SendAsync(request).Result)
        {
            long fileSize = response.Content.Headers.ContentLength ?? 0;
            long chunkSize = fileSize / chunkCount;
            Console.WriteLine($"File size: {Math.Round(fileSize/Math.Pow(2, 20), 2)} MB");
            Thread[] threads = new Thread[chunkCount];

            // Start timer to track download time
            Stopwatch stopwatch = Stopwatch.StartNew();

            for (int i = 0; i < chunkCount; i++)
            {
                long start = i * chunkSize;
                long end = (i == chunkCount - 1) ? fileSize - 1 : (start + chunkSize - 1);
                int chunkIndex = i;

                threads[i] = new Thread(() => DownloadChunk(start, end, chunkIndex));
                threads[i].Start();
            }

            foreach (var thread in threads)
            {
                thread.Join();
            }

            // Combine chunks
            using (var output = new FileStream(filePath, FileMode.Create))
            {
                for (int i = 0; i < chunkCount; i++)
                {
                    string chunkPath = $"{filePath}.part{i}";
                    using (var chunkStream = new FileStream(chunkPath, FileMode.Open))
                    {
                        chunkStream.CopyTo(output);
                    }
                    File.Delete(chunkPath);
                }
            }

            stopwatch.Stop();
            Console.WriteLine($"Download completed in {stopwatch.Elapsed.TotalSeconds} seconds.");
            Console.WriteLine("File downloaded and chunks merged successfully!");
        }
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        string url, filePath;
        int chunkCount;

        Console.WriteLine("Enter the URL of the file to download: ");
        url = Console.ReadLine() ?? string.Empty;
        Console.WriteLine("Enter the path to save the file: ");
        filePath = Console.ReadLine() ?? string.Empty;
        Console.WriteLine("Enter the number of threads: ");
        chunkCount = int.Parse(Console.ReadLine() ?? "1");

        DownloadFileMultithreading downloader = new (chunkCount, url, filePath);
        downloader.DownloadFileInChunks();
    }
}
