using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;

namespace Docker
{
    class Program
    {
        static async Task Main(string[] args)
        {
            if (File.Exists("output2.mp4"))
            {
                File.Delete("output2.mp4");
            }

            var z3 = await "killall Xvfb".Bash(true);
            //export DISPLAY=\":99\"
            var x2 = await "Xvfb :99 -screen 0 1920x1080x24 &".Bash(true);
            var tt = await "xdpyinfo -display :99 >/dev/null 2>&1 && echo \"In use\" || echo \"Free\"".Bash(true);
            var x = new CancellationTokenSource();

             await "killall chrome".Bash(true);
            var z4 = await ProcessAsyncHelper.ExecuteShellCommand("google-chrome", new[]
                {
                    "--remote-debugging-port=9222",
                    "--no-first-run",
                    "--start-fullscreen",
                    "--disable-gpu",
                    "--enable-logging --v=1",
                    "--no-sandbox",
                    "--headless",
                    "--remote-debugging-address=0.0.0.0",
                    "https://www.spitball.co/learn?Country=US"
                }
                , CancellationToken.None);



            var z = await ProcessAsyncHelper.ExecuteShellCommand("ffmpeg", new[]
              {
                "-y -nostdin",
                "-video_size 1920x1080 -framerate 30 -f x11grab",
                "-i :99",
                "-c:v libx264 -crf 0 -preset ultrafast",
                "output2.mkv",
            }, x.Token);

            await Task.Delay(TimeSpan.FromSeconds(30));

            x.Cancel();


            var z22222 = await ProcessAsyncHelper.ExecuteShellCommand("ffmpeg", new[]
              {
                "-y -nostdin",
               // "-video_size 1920x1080 -framerate 30 -f x11grab",
                "-i output2.mkv",
                "-c:v libx264 -crf 23 -pix_fmt yuv420p",
                "output2.mp4",
            }, true, CancellationToken.None);


            //Create a unique name for the container
            //string containerName = "quickstartblobs" + Guid.NewGuid().ToString();

            // Create the container and return a container client object
            //BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient("spitball-user");
            //var blobClient = containerClient.GetBlobClient("test.mp4");
            if (File.Exists("output2.mp4"))
            {
                await using FileStream uploadFileStream = File.OpenRead("output2.mp4");
                Debug.WriteLine(uploadFileStream.Length);
                //await blobClient.UploadAsync(uploadFileStream, true);
                uploadFileStream.Close();
            }

            // Process.Start(command);
            //foreach (var process in Process.GetProcesses())
            //{

            //    Console.WriteLine($"Counter: {process.ProcessName}");
            //}
            var counter = 0;
            var max = args.Length != 0 ? Convert.ToInt32(args[0]) : -1;
            while (max == -1 || counter < max)
            {
                Console.WriteLine($"Counter: {++counter}");
                await Task.Delay(1000);
            }
        }
    }

    public static class ShellHelper
    {
        public static Task<ProcessAsyncHelper.ProcessResult> Bash(this string cmd, bool shouldWaitToExit = false)
        {
            var escapedArgs = cmd.Replace("\"", "\\\"");

            return ProcessAsyncHelper.ExecuteShellCommand("/bin/bash", new[]
             {
                $"-c \"{escapedArgs}\""
            }, shouldWaitToExit, CancellationToken.None);

        
        }

       
    }
}
