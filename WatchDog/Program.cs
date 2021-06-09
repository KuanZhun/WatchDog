using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace WatchDog
{
    public class Program
    {
        private static readonly string settingsFilePath = Path.Combine(Directory.GetCurrentDirectory(), "WatchDog_Settings.json");
        public static void Main(string[] args)
        {
            try
            {
                Directory.SetCurrentDirectory(args[Array.IndexOf(args, "--WorkDirectory") + 1]);
                if (!File.Exists(settingsFilePath))
                    File.WriteAllText(settingsFilePath, JsonConvert.SerializeObject(new Setting[]
                    {
                    new Setting()
                    {
                        FileName = Path.Combine(Directory.GetCurrentDirectory(), "TestApp.exe"),
                        WorkingDirectory = Path.Combine(Directory.GetCurrentDirectory()),
                        RestartDelay = TimeSpan.FromSeconds(3),
                        RestartTimer = TimeSpan.FromHours(12),
                        Retry = 3
                    }
                    }, Formatting.Indented), Encoding.UTF8);
                Setting[] settings = JsonConvert.DeserializeObject<Setting[]>(File.ReadAllText(settingsFilePath, Encoding.UTF8));
                foreach (Setting item in settings)
                {
                    item.Start();
                }
                Host.CreateDefaultBuilder(args)
                    .UseWindowsService().Build().Run();
                    //.ConfigureServices((hostContext, services) =>
                    //{
                    //    services.AddHostedService<Worker>();
                    //})
            }
            catch (Exception)
            {
                Console.WriteLine("未設定正確的啟動參數。");
            }
        }
    }
    public class Setting : IDisposable
    {
        public string FileName { get; set; }
        public string WorkingDirectory { get; set; }
        public TimeSpan RestartTimer { get; set; }
        public TimeSpan RestartDelay { get; set; }
        public int Retry { get; set; }
        private CancellationTokenSource _mainCancellation;
        private Task _mainTask;
        private Process process;
        private bool _hasExitedBuffer = false;
        private int _retryCount = 0;
        public void Start()
        {
            try
            {
                process = Process.Start(new ProcessStartInfo()
                {
                    FileName = FileName,
                    WorkingDirectory = WorkingDirectory
                });
                Process[] processes = Process.GetProcessesByName(process.ProcessName);
                foreach (Process item in processes)
                {
                    if (item.Id != process.Id)
                        item.Kill();
                }
                _mainCancellation = new CancellationTokenSource();
                _mainTask = Task.Run(Main, _mainCancellation.Token);
            }
            catch (Exception)
            {
                Console.WriteLine(process.StartInfo.FileName + "啟動失敗。");
            }
        }
        private async Task Main()
        {
            while (!_mainCancellation.IsCancellationRequested)
            {
                try
                {
                    if (process.HasExited && !_hasExitedBuffer)
                    {
                        _hasExitedBuffer = true;
                        StartProcess();
                    }
                    if (!process.HasExited && DateTime.Now - process.StartTime >= RestartTimer)
                    {
                        process.Kill();
                    }
                    await Task.Delay(TimeSpan.FromMilliseconds(200));
                }
                catch (Exception ex)
                {
                    KzDev.ExceptionLogWriter.WriteLog(ex);
                    await Task.Delay(TimeSpan.FromSeconds(5));
                }
            }
        }
        private async void StartProcess()
        {
            await Task.Delay(RestartDelay);
            try
            {
                process = Process.Start(new ProcessStartInfo()
                {
                    FileName = FileName,
                    WorkingDirectory = WorkingDirectory
                });
                _hasExitedBuffer = false;
                _retryCount = 0;
            }
            catch (Exception)
            {
                Console.WriteLine(process.StartInfo.FileName + "啟動失敗。");
                if (_retryCount < Retry)
                {
                    _retryCount++;
                    _hasExitedBuffer = false;
                }
                else
                {
                    Console.WriteLine("重試次數超過" + Retry.ToString() + "次。");
                    _mainCancellation.Cancel();
                }
            }
        }
        public void Dispose()
        {
            _mainCancellation.Cancel();
        }
    }
}