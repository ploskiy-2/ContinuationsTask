using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace ContinuationsTask
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            // Create token which will report about abolition
            
            CancellationTokenSource showSplashcancelTokenSource = new CancellationTokenSource();
            CancellationToken showSplashToken = showSplashcancelTokenSource.Token;

            ///created dependent tokens 
            ///(if showSplashToken is canceled, then the child tokens will also
            ///be canceled immediately, without calling the task)
            CancellationTokenSource licenseTokenSource = CancellationTokenSource.CreateLinkedTokenSource(showSplashToken);
            CancellationToken licenseToken = licenseTokenSource.Token;

            CancellationTokenSource updateTokenSource = CancellationTokenSource.CreateLinkedTokenSource(showSplashToken);
            CancellationToken checkForUpdateToken = updateTokenSource.Token;

            ///this is child token for licenseToken
            CancellationTokenSource setupMenuTokenSource = CancellationTokenSource.CreateLinkedTokenSource(licenseToken);
            CancellationToken setupMenuToken = setupMenuTokenSource.Token;

            ///this is child token for checkforupdateToken
            CancellationTokenSource downloadUpdateTokenSource = CancellationTokenSource.CreateLinkedTokenSource(checkForUpdateToken);
            CancellationToken downloadUpdateToken = downloadUpdateTokenSource.Token;

            ///this is linked token for downloadUpdate and setupMenu
            CancellationTokenSource displayWelcomeScreenTokenSource = CancellationTokenSource.CreateLinkedTokenSource(downloadUpdateToken, setupMenuToken);
            CancellationToken displayWelcomeScreenToken = displayWelcomeScreenTokenSource.Token;

            ///this is last token 
            CancellationTokenSource hideSplashTokenSource = CancellationTokenSource.CreateLinkedTokenSource(displayWelcomeScreenToken);
            CancellationToken hideSplashToken = hideSplashTokenSource.Token;

            
            /// Create start process
            Task showSplashTask = new Task(() =>
            {
                Console.WriteLine("ShowSplash process has started...");
                Thread.Sleep(2000);
                Console.WriteLine("ShowSplash process has finished.");
            });

            /// Create second process. it is continuation of ShowSplash 
            Task licenseTask = showSplashTask.ContinueWith((showSplashResult,t) =>
            {               
                Console.WriteLine("Requesting license...");
                Thread.Sleep(2000); // Simulate license verification process
                if (licenseToken.IsCancellationRequested)
                {
                    Console.WriteLine("License verification canceled.");
                    Console.WriteLine("No license.");
                    licenseToken.ThrowIfCancellationRequested();
                }
                else
                {
                    Console.WriteLine("License verified.");
                }
            }, licenseToken);

            /// Create third process which works async with licenseTask
            Task checkForUpdateTask = showSplashTask.ContinueWith((showSplashResult,t) =>
            {


                Console.WriteLine("Checking updates...");
                Thread.Sleep(2000); ; // Simulate checking updates process
                if (checkForUpdateToken.IsCancellationRequested)
                {
                    Console.WriteLine("Check for update canceled.");
                    checkForUpdateToken.ThrowIfCancellationRequested();

                }
                else
                {
                    Console.WriteLine("Updates were found");
                }
            }, checkForUpdateToken);

            ///create new task which will be child task for licenseTask
            Task setupMenuTask = licenseTask.ContinueWith((licenseResult,t) =>
            {


                Console.WriteLine("Setup menus...");
                Thread.Sleep(2000);
                if (setupMenuToken.IsCancellationRequested)
                {
                    Console.WriteLine("Setup menus canceled.");
                    Console.WriteLine("Server error");
                    setupMenuToken.ThrowIfCancellationRequested();

                }
                else
                {
                    Console.WriteLine("Setup menus finished");
                }
            }, TaskContinuationOptions.NotOnCanceled, setupMenuToken); 

            ///create new task which will be child task for checkforupdate
            Task downloadUpdateTask = checkForUpdateTask.ContinueWith((checkForUpdateResult,t) =>
            {
                Console.WriteLine("Downloading updates....");
                Thread.Sleep(2000);
                if (downloadUpdateToken.IsCancellationRequested)
                {
                    Console.WriteLine("Download updates canceled");
                    Console.WriteLine("No rights for this operation");
                    downloadUpdateToken.ThrowIfCancellationRequested();

                }
                else
                {
                    Console.WriteLine("Download updates finished");
                }
            }, TaskContinuationOptions.NotOnCanceled, downloadUpdateToken);
            var multiTasks = new List<Task>();
            multiTasks.Add(setupMenuTask);
            multiTasks.Add(downloadUpdateTask);
            ///create second to last linked task 
            Task displayWelcomeScreenTask = Task.WhenAll(multiTasks).ContinueWith((checkForUpdateResult) =>
            {
                Thread.Sleep(1000);
                Console.WriteLine("Display welcome screen....");
                Thread.Sleep(1000);
                Console.WriteLine("Welcome screen was displayed");
                
            });

            ///create last linked task 
            Task hideSplashTask = displayWelcomeScreenTask.ContinueWith((displayWelcomeScreenResult,t) =>
            {
                Thread.Sleep(1000);
                Console.WriteLine("Hide splash....");
                Thread.Sleep(1000);
                Console.WriteLine("Splash was hided");

            }, TaskContinuationOptions.NotOnFaulted, hideSplashToken);

            try
            {
                /// Start showsplash and it will be interrupted  with a 20% chance 
                showSplashTask.Start();
                
                Thread.Sleep(10);
                if (new Random().Next(10) < 2)
                {
                    ///showSplashcancelTokenSource.Cancel();
                }
                showSplashTask.Wait();

                Thread.Sleep(10);
                if (new Random().Next(10) < 2)
                {
                    licenseTokenSource.Cancel();
                }

                Thread.Sleep(10);
                if (new Random().Next(10) < 2)
                {
                    updateTokenSource.Cancel();
                }

                
                Task.WaitAll(checkForUpdateTask, licenseTask);

                Thread.Sleep(10);
                if (new Random().Next(10) < 2)
                {
                    setupMenuTokenSource.Cancel();
                }

                Thread.Sleep(10);
                if (new Random().Next(10) < 2)
                {
                    downloadUpdateTokenSource.Cancel();
                }

                Task.WaitAll(setupMenuTask, downloadUpdateTask);



                if (!displayWelcomeScreenToken.IsCancellationRequested)
                {
                   
                    displayWelcomeScreenTask.Start();
                    displayWelcomeScreenTask.Wait();
                }

                hideSplashTask.Wait();
                
            }
            catch (AggregateException ae)
            {
                foreach (Exception e in ae.InnerExceptions)
                {
                    if (e is TaskCanceledException)
                        Console.WriteLine("This crashed. All next processes canceled");
                }
            }
            finally
            {
                showSplashcancelTokenSource.Dispose();
                licenseTokenSource.Dispose();
                updateTokenSource.Dispose();
                setupMenuTokenSource.Dispose();
                downloadUpdateTokenSource.Dispose();
                displayWelcomeScreenTokenSource.Dispose();
                hideSplashTokenSource.Dispose();
            }
            if (!hideSplashToken.IsCancellationRequested)
            {
                Console.WriteLine();
                Console.WriteLine("The program was successfully launched");
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine("An error was detected when starting the program");
            }
        }
    }
}