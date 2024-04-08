
using ClickableTransparentOverlay;
using ImGuiNET;
using System.Numerics;
using System.Windows.Forms;

namespace TaskListManager
{
    public class Program
    {
        public static void Main()
        {
            var taskListView = new TaskListView();
            var trayIcon = new TrayIcon
            {
                OnClick = taskListView.Show,
                OnClose = () =>
                {
                    Application.Exit();
                    taskListView.Close();
                }
            };
            taskListView.Start();

            Application.Run();
        }
    }

    public class TrayIcon
    {
        private NotifyIcon _notifyIcon;

        public Action? OnClick { get; set; }
        public Action? OnClose { get; set; }

        public TrayIcon()
        {
            _notifyIcon = new NotifyIcon();
            _notifyIcon.Icon = new Icon("Resources/icon.ico");
            _notifyIcon.Click += (sender, e) => { OnClick?.Invoke(); };

            _notifyIcon.ContextMenuStrip = new();
            _notifyIcon.ContextMenuStrip.Items.Add("Exit", null, (sender, e) => { OnClose?.Invoke(); });

            _notifyIcon.Visible = true;
        }
    }

    public class TaskListView : Overlay
    {
        private bool _isOpen = true;

        public void Show()
        {
            _isOpen = true;
        }

        protected override void Render()
        {
            if (!_isOpen) return;

            ImGui.SetNextWindowSize(new Vector2(500,250));
            ImGui.Begin("Your task list", ref _isOpen);



            ImGui.End();
        }
    }
}