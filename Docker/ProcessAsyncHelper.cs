using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

public static class ProcessAsyncHelper
{
    public static Task<ProcessResult> ExecuteShellCommand(string command, IEnumerable<string> arguments,
        CancellationToken token) =>
        ExecuteShellCommand(command, arguments, false, token);
    public static async Task<ProcessResult> ExecuteShellCommand(string command, IEnumerable<string> arguments, bool shouldWait, CancellationToken token)
    {
        var result = new ProcessResult();

        var process = new Process()
        {
            StartInfo = new ProcessStartInfo()
            {

                FileName = command,
                UseShellExecute = false,
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            }
        };
        if (arguments != null)
        {
            process.StartInfo.Arguments = string.Join(" ", arguments);
        }
        //foreach (var argument in arguments)
        //{
        //    process.StartInfo.ArgumentList.Add(argument);
        //}

        // If you run bash-script on Linux it is possible that ExitCode can be 255.
        // To fix it you can try to add '#!/bin/bash' header to the script.
        var outputBuilder = new StringBuilder();
        var outputCloseEvent = new TaskCompletionSource<bool>();

        process.OutputDataReceived += (s, e) =>
                                        {
                                            // The output stream has been closed i.e. the process has terminated
                                            if (e.Data == null)
                                            {
                                                outputCloseEvent.SetResult(true);
                                            }
                                            else
                                            {
                                                Debug.WriteLine(e.Data);
                                                outputBuilder.AppendLine(e.Data);
                                            }
                                        };

        var errorBuilder = new StringBuilder();
        var errorCloseEvent = new TaskCompletionSource<bool>();

        process.ErrorDataReceived += (s, e) =>
                                        {
                                            // The error stream has been closed i.e. the process has terminated
                                            if (e.Data == null)
                                            {
                                                errorCloseEvent.SetResult(true);
                                            }
                                            else
                                            {
                                                Debug.WriteLine(e.Data);
                                                errorBuilder.AppendLine(e.Data);
                                            }
                                        };

        bool isStarted;

        try
        {
            isStarted = process.Start();
        }
        catch (Exception error)
        {
            // Usually it occurs when an executable file is not found or is not executable

            result.Completed = true;
            result.ExitCode = -1;
            result.Output = error.Message;

            isStarted = false;
        }

        if (isStarted)
        {
            // Reads the output stream first and then waits because deadlocks are possible
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();


            token.Register(() =>
            {
                process.Kill();
                process.Dispose();
            });
            // Creates task to wait for process exit using timeout
            if (shouldWait)
            {
                var waitForExit = WaitForExitAsync(process, token);
                var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);
               // await Task.WhenAll(processTask);
                if (await Task.WhenAny(processTask,Task.Delay(TimeSpan.FromSeconds(10))) == processTask && waitForExit.Result)
                {
                    result.Completed = true;
                    result.ExitCode = process.ExitCode;

                    // Adds process output if it was completed with error
                    if (process.ExitCode != 0)
                    {
                        result.Output = $"{outputBuilder}{errorBuilder}";
                    }
                }
                else
                {
                    try
                    {
                        // Kill hung process
                       // process.Kill();
                        result.Output = $"{outputBuilder}{errorBuilder}";
                    }
                    catch
                    {
                    }
                }
            }


            // Create task to wait for process exit and closing all output streams
            //var processTask = Task.WhenAll(waitForExit, outputCloseEvent.Task, errorCloseEvent.Task);

            // Waits process completion and then checks it was not completed by timeout
            //await Task.WhenAll(processTask);
            //if (await Task.WhenAny(processTask) == processTask && waitForExit.Result)
            //{
            //    result.Completed = true;
            //    result.ExitCode = process.ExitCode;

            //    // Adds process output if it was completed with error
            //    if (process.ExitCode != 0)
            //    {
            //        result.Output = $"{outputBuilder}{errorBuilder}";
            //    }
            //}
            //else
            //{
            //    try
            //    {
            //        // Kill hung process
            //        process.Kill();
            //        result.Output = $"{outputBuilder}{errorBuilder}";
            //    }
            //    catch
            //    {
            //    }
            //}
        }


        return result;
    }


    private static Task<bool> WaitForExitAsync(Process process, CancellationToken token)
    {
        var  milliseconds = (int)TimeSpan.FromSeconds(10).TotalMilliseconds;
        //return Task.Run(() => process.WaitForExit(Milliseconds), token);
        return Task.Run(() => process.WaitForExit(milliseconds), token);
    }


    public struct ProcessResult
    {
        public bool Completed;
        public int? ExitCode;
        public string Output;
    }
}