
using ClickableTransparentOverlay;
using ImGuiNET;
using Newtonsoft.Json;
using System.Globalization;
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
        private const string _fileName = "taskdata.json";

        public TaskListController()
        {
            Dictionary<TaskType, List<string>> tasks;
            if (File.Exists(_fileName))
                tasks = JsonConvert.DeserializeObject<Dictionary<TaskType, List<string>>>(File.ReadAllText(_fileName));
            else
                tasks = new()
                {
                    { TaskType.Daily, new List<string>() },
                    { TaskType.Permanent, new List<string>() }
                };

            _view = new();

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

        private void Save()
        {
            //File.WriteAllText(_fileName, JsonConvert.SerializeObject(_view.Vm.Tasks));
        }
    }

    public enum OtherWindowStates
    {
        None,
        ForceWorkDay,
        Backlog,
        Logs,
        Editor
    }

    public enum ClaimResults
    {
        None,
        Claimed
    }

    [Serializable]
    public class Project
    {
        public string Name = "";
        public int DaysPerWeek;
        public bool Active;
        public int DaysWorkedThisWeek;
        public int DaysBehind;
        public int TotalDaysWorked;
    }

    [Serializable]
    public class SaveFile
    {
        public List<Project> Projects;
        public DateTime LastUsage;
        public Project? LastProject;
    }

    public class TaskListView : Overlay
    {
        private bool _isOpen = true;
        private const int _width = 500;
        private const int _height = 250;
        private const int _otherWidth = 1000;
        private const int _otherHeight = 250;
        private OtherWindowStates _otherWindowState = OtherWindowStates.None;
        private ClaimResults _claimResults = ClaimResults.None;

        public List<Project> _projects = new();

        private string _nextProjectName = "";

        private Project? _todaysProject = null;

        private DateTime _lastCheckedDate = DateTime.Now;

        private int _daysToWorkPerWeek = 5;

        public TaskListView()
        {
            if (!File.Exists("savefile.json"))
                return;

            var text = File.ReadAllText("savefile.json");

            SaveFile save = JsonConvert.DeserializeObject<SaveFile>(text);

            _projects = save.Projects;
            _lastCheckedDate = save.LastUsage;

            var now = DateTime.Now;

            if (now.DayOfYear == _lastCheckedDate.DayOfYear)
            {
                _todaysProject = save.LastProject;
            }

            Calendar calendar = CultureInfo.CurrentCulture.Calendar;
            var previousWeek = calendar.GetWeekOfYear(save.LastUsage, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
            var currentWeek = calendar.GetWeekOfYear(now, CalendarWeekRule.FirstDay, DayOfWeek.Monday);

            if (previousWeek != currentWeek)
            {
                foreach (var project in _projects)
                {
                    project.DaysWorkedThisWeek = 0;

                    int workDifference = project.DaysPerWeek - project.DaysWorkedThisWeek;

                    project.DaysBehind += workDifference;

                    if (project.DaysBehind < 0)
                        project.DaysBehind = 0;
                }
            }
        }

        public void Save()
        {
            SaveFile file = new();
            file.Projects = _projects;
            file.LastUsage = DateTime.Now;
            file.LastProject = _todaysProject;

            var text = JsonConvert.SerializeObject(file);

            File.WriteAllText("savefile.json", text);
        }

        public void Show()
        {
            _isOpen = true;
        }

        private void ClaimProject(Project project)
        {
            project.DaysWorkedThisWeek++;
            project.TotalDaysWorked++;
            _todaysProject = project;
            _claimResults = ClaimResults.Claimed;

            Save();
        }

        protected override void Render()
        {
            if (!_isOpen) return;

            ImGui.SetNextWindowSize(new Vector2(_width, _height));
            ImGui.Begin("Your task list", ref _isOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

            if(ImGui.Button("Claim day"))
            {
                if(_claimResults == ClaimResults.None)
                {
                    int daysWorked = 0;
                    foreach(var project in _projects)
                    {
                        daysWorked += project.DaysWorkedThisWeek;
                    }

                    if(daysWorked < _daysToWorkPerWeek)
                    {
                        List<Project> weightedProjectList = new();

                        foreach(var project in _projects)
                        {
                            for(int i = 0; i < project.DaysPerWeek - project.DaysWorkedThisWeek; ++i)
                            {
                                weightedProjectList.Add(project);
                            }
                        }

                        Project projectForToday = weightedProjectList[(new Random()).Next() % weightedProjectList.Count];

                        ClaimProject(projectForToday);
                    }
                }
            }
            if(ImGui.Button("Force work day"))
            {
                _otherWindowState = OtherWindowStates.ForceWorkDay;
            }
            if(ImGui.Button("Backlog"))
            {
                _otherWindowState = OtherWindowStates.Backlog;
            }
            if (ImGui.Button("Logs"))
            {
                _otherWindowState = OtherWindowStates.Logs;
            }
            if (ImGui.Button("Edit"))
            {
                _otherWindowState = OtherWindowStates.Editor;
            }

            if (_claimResults == ClaimResults.Claimed || _todaysProject != null)
            {
                ImGui.Text($"Today's item is {_todaysProject?.Name}");
            }

            ImGui.End();

            if(_otherWindowState != OtherWindowStates.None)
            {
                string windowName = "other";
                switch (_otherWindowState)
                {
                    case OtherWindowStates.ForceWorkDay:
                        windowName = "Force a work day";
                        break;
                    case OtherWindowStates.Backlog:
                        windowName = "Backlog";
                        break;
                    case OtherWindowStates.Logs:
                        windowName = "Logs";
                        break;
                    case OtherWindowStates.Editor:
                        windowName = "Editor";
                        break;
                }

                ImGui.SetNextWindowSize(new Vector2(_otherWidth, _otherHeight));
                bool isOtherWindowOpen = true;
                ImGui.Begin(windowName, ref isOtherWindowOpen, ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);

                switch (_otherWindowState)
                {
                    case OtherWindowStates.ForceWorkDay:
                        {
                            foreach(var project in _projects)
                            {
                                if (ImGui.Button(project.Name))
                                {
                                    ClaimProject(project);
                                    _otherWindowState = OtherWindowStates.None;
                                }
                            }
                        }
                        break;
                    case OtherWindowStates.Backlog:
                        bool empty = true;
                        foreach (var project in _projects)
                        {
                            if (project.DaysBehind == 0)
                                continue;

                            empty = false;

                            ImGui.Text($"{project.Name}: {project.DaysBehind} days");
                        }

                        if(empty)
                        {
                            ImGui.Text("No backlog, keep it up! ;)");
                        }
                        break;
                    case OtherWindowStates.Logs:
                        foreach (var project in _projects)
                        {
                            ImGui.Text($"{project.Name}: {project.TotalDaysWorked} day(s) worked ({project.DaysWorkedThisWeek} day(s) this week)");
                        }
                        break;
                    case OtherWindowStates.Editor:
                        {
                            ImGui.InputTextWithHint("", "Project name here", ref _nextProjectName, 100);
                            ImGui.SameLine();
                            if(ImGui.Button("Add"))
                            {
                                _projects.Add(new Project() { Name = _nextProjectName, Active = true });
                                Save();
                            }

                            ImGui.Spacing();

                            List<Project> toRemove = new();

                            for (int i = 0; i < _projects.Count; i++)
                            {
                                Project project = _projects[i];
                                var prevDays = project.DaysPerWeek;
                                var prevState = project.Active;
                                ImGui.Text(project.Name);
                                //ImGui.SameLine();
                                ImGui.InputInt($"days {i}", ref project.DaysPerWeek);
                                //ImGui.SameLine();
                                ImGui.Checkbox($"Enabled {i}", ref project.Active);
                                //ImGui.SameLine();
                                if(ImGui.Button($"X {i}", new Vector2(20,20)))
                                {
                                    toRemove.Add(project);
                                }
                                if (prevDays != project.DaysPerWeek)
                                    Save();
                                if (prevState != project.Active)
                                    Save();
                            }

                            foreach (var project in toRemove)
                                _projects.Remove(project);

                            if(toRemove.Count > 0)
                            {
                                Save();
                            }
                        }
                        break;
                }

                ImGui.End();

                if(!isOtherWindowOpen)
                {
                    _otherWindowState = OtherWindowStates.None;
                }
            }
        }
    }
}