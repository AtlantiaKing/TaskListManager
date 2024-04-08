
using ClickableTransparentOverlay;
using ImGuiNET;
using System.Numerics;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace TaskListManager
{
    public class Program
    {
        public static void Main()
        {
            var taskList = new TaskListController();
            var trayIcon = new TrayIcon
            {
                OnClick = taskList.ShowView,
                OnClose = () =>
                {
                    Application.Exit();
                    taskList.Dispose();
                }
            };

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
    public enum TaskType
    {
        Daily,
        Permanent
    }

    public class TaskListController : IDisposable
    {
        private TaskListView _view;

        public TaskListController()
        {
            // TODO: Load from file
            Dictionary<TaskType, List<string>> tasks = new()
            {
                { TaskType.Daily, new List<string>() },
                { TaskType.Permanent, new List<string>() }
            };

            _view = new()
            {
                Vm = new()
                {
                    OnCreateTask = CreateTask,
                    OnTaskComplete = FinishTask,
                    Tasks = tasks,
                }
            };

            _view.Start();
        }

        public void Dispose()
        {
            _view.Close();
        }

        public void ShowView()
        {
            _view.Show();
        }

        public void CreateTask(string taskName, TaskType taskType)
        {
            // TODO: Save to file

            _view.Vm.Tasks[taskType].Add(taskName);
        }

        public void FinishTask(string taskName, TaskType taskType)
        {
            // TODO: Save to file

            _view.Vm.Tasks[taskType].Remove(taskName);
        }
    }

    public class TaskListView : Overlay
    {
        public class TaskListVM
        {
            public Action<string, TaskType>? OnCreateTask { get; set; }
            public Action<string, TaskType>? OnTaskComplete { get; set; }
            public Dictionary<TaskType, List<string>> Tasks { get; set; } = new();
        }
        public TaskListVM Vm { get; set; } = new();

        private bool _isOpen = true;
        private const int _width = 500;
        private const int _height = 250;
        private const int _heightPerTask = 25;

        private int _currentSelectedTaskType = 0;
        private string _currentTaskName = string.Empty;

        public void Show()
        {
            _isOpen = true;
        }

        protected override void Render()
        {
            if (!_isOpen) return;

            ImGui.SetNextWindowSize(new Vector2(_width, _height));
            ImGui.Begin("Your task list", ref _isOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

            foreach((TaskType task, List<string> tasks) in Vm.Tasks)
            {
                PrintTasks(task, tasks, Vm.OnTaskComplete);
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Spacing();

            var taskTypesAsStrings = Enum.GetNames(typeof(TaskType));
            ImGui.Combo("Task type", ref _currentSelectedTaskType, taskTypesAsStrings, taskTypesAsStrings.Length);

            ImGui.InputText("Task name", ref _currentTaskName, 100);

            if(ImGui.Button("Create new task"))
            {
                Vm.OnCreateTask?.Invoke(_currentTaskName, ((TaskType[])Enum.GetValues(typeof(TaskType)))[_currentSelectedTaskType]);
            }

            ImGui.End();
        }

        private void PrintTasks(TaskType type, List<string> tasks, Action<string, TaskType>? onTaskFinished)
        {
            string taskTypeText = type.ToString().Replace('_', ' ');

            ImGui.Text(taskTypeText);
            ImGui.BeginListBox(taskTypeText, new Vector2(_width, tasks.Count == 0 ? _heightPerTask : _heightPerTask * tasks.Count));
            for (int taskIdx = tasks.Count - 1; taskIdx >= 0; --taskIdx)
            {
                string curTask = tasks[taskIdx];

                bool tempBool = false;
                if (ImGui.Checkbox(curTask, ref tempBool))
                    onTaskFinished?.Invoke(curTask, type);
            }
            ImGui.EndListBox();
        }
    }
}