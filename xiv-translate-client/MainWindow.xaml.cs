using System;
using System.Diagnostics;
using System.Windows;
using System.Windows.Threading;
using System.Linq;
using System.Net.Http;
using System.Text;

using Sharlayan;
using Sharlayan.Models;

namespace xivTranslate
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.Loaded += this.MainWindow_Loaded;
            this.chatLogSubscriber.Tick += (_, __) => this.GetChatLogs();
            this.MouseLeftButtonDown += (_, __) => this.DragMove();
        }

        private readonly DispatcherTimer chatLogSubscriber = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromMilliseconds(1000),
        };

        private int _previousArrayIndex = 0;
        private int _previousOffset = 0;

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Process[] processes = Process.GetProcessesByName("ffxiv_dx11");

            if (processes.Length > 0)
            {
                // TODO: Language Switcher
                // supported: English, Chinese, Japanese, French, German, Korean
                string gameLanguage = "Japanese";

                // whether to always hit API on start to get the latest sigs based on patchVersion, or use the local json cache (if the file doesn't exist, API will be hit)
                bool useLocalCache = false;
                string patchVersion = "latest";

                Process process = processes[0];
                ProcessModel processModel = new ProcessModel
                {
                    Process = process,
                    IsWin64 = true,
                };

                MemoryHandler.Instance.SetProcess(
                    processModel,
                    gameLanguage,
                    patchVersion,
                    useLocalCache);

                this.chatLogSubscriber.Start();
            }
        }

        private void ScrollWindowToBottom()
        {
            this.LogTextBox.Focus();
            this.LogTextBox.CaretIndex = this.LogTextBox.Text.Length;
            this.LogTextBox.ScrollToEnd();
        }

        private async void GetTranslate(string text)
        {
            var content = new StringContent(text, Encoding.UTF8);

            using (var client = new HttpClient())
            {
                var response = await client.PostAsync("https://ru0nebe0o2.execute-api.ap-northeast-1.amazonaws.com/Prod/", content);
                this.LogTextBox.Text += (await response.Content.ReadAsStringAsync());
            }
            ScrollWindowToBottom();
        }

        private void GetChatLogs()
        {
            var readResult = Reader.GetChatLog(this._previousArrayIndex, this._previousOffset);

            var chatLogEntries = readResult.ChatLogItems;

            this._previousArrayIndex = readResult.PreviousArrayIndex;
            this._previousOffset = readResult.PreviousOffset;

            if (chatLogEntries.Count < 1)
            {
                return;
            }

            foreach (var chatLog in chatLogEntries)
            {
                string[] allowChatMode = {"003D","000A","001E","000B","000D","000E","000F","0018","0010","0011","0012","0013","0014","0015","0016","0017","0025","0065","0066","0067","0068","0069","006A","006B","001B"};

                if (!allowChatMode.Contains(chatLog.Code))
                {
                    continue;
                }

                GetTranslate(chatLog.Line);
                this.LogTextBox.Text += ($"{chatLog.Line}\n");
            }
            ScrollWindowToBottom();
        }
    }
}