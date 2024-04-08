
using ClickableTransparentOverlay;
using ImGuiNET;
using System.Numerics;

namespace TaskListManager
{
    public class Program
    {
        public static void Main()
        {
            var taskListView = new TaskListView();
            taskListView.Start().Wait();
        }
    }

    public class TaskListView : Overlay
    {
        protected override void Render()
        {
            ImGui.SetNextWindowSize(new Vector2(500, 250));
            ImGui.Begin("Your task list");

            ImGui.End();
        }
    }
}