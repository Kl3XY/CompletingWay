using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Dalamud.Bindings.ImGui;
using Dalamud.Interface.Textures;
using Dalamud.Interface.Utility;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Client.UI.Misc;
using Lumina.Excel.Sheets;
using SamplePlugin.Classes;

namespace SamplePlugin.Windows;

public unsafe class MainWindow : Window, IDisposable
{
    public double RefreshTick = 0;
    public double ReloadProgressTick = 20;
    
    private readonly string goatImagePath;
    private readonly Plugin plugin;
    private double refreshTick = 1;
    private double reloadTick = 1;
    private AchievementProgressTracker tracker = new();

    // We give this window a hidden ID using ##.
    // The user will see "My Amazing Window" as window title,
    // but for ImGui the ID is "My Amazing Window##With a hidden ID"
    public MainWindow(Plugin plugin, string goatImagePath)
        : base("Tracked Achievements##With a hidden ID", ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse)
    {
        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(375, 330),
            MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Plugin.Framework.Update += OnUpdate;
        
        var achievementListModule = AchievementListModule.Instance();
        var list = achievementListModule->WatchList.ToArray().Where(x => x != 0);
        
        tracker.fillList(list.ToList());
        
        this.goatImagePath = goatImagePath;
        this.plugin = plugin;
    }

    
    public override void OnOpen()
    {
        var achievementListModule = AchievementListModule.Instance();
        var list = achievementListModule->WatchList.ToArray().Where(x => x != 0);
        
        tracker.fillList(list.ToList());
    }
    
    private void OnUpdate(IFramework framework)
    {
        try
        {
            double delta = (Convert.ToDouble(framework.UpdateDelta.Milliseconds) / 1000);
            refreshTick -= delta;
            reloadTick -= delta;

            if (refreshTick >= 0) return;
            refreshTick = RefreshTick;
            
            /*
             * Check if the user has added any Achievements, if so rerun the loop.
             */
            var achievementListModule = AchievementListModule.Instance();
            var list = achievementListModule->WatchList.ToArray().Where(x => x != 0);
            if (tracker.GetResultCount() != list.Count())
            {
                Plugin.Log.Information("Achievements have been added, rerun the check!");
                tracker.fillList(list.ToList());
            }
            
            tracker.onUpdate();

            if (reloadTick < 0)
            {
                reloadTick = ReloadProgressTick;
                tracker.Refresh();
            }
        }
        catch (Exception ex)
        {
            Plugin.Log.Information(ex.ToString());
        }
    }
    
    public void Dispose() { }

    public unsafe override void Draw()
    {
        using (var child = ImRaii.Child("child", Vector2.Zero, true))
        {
            // Check if this child is drawing
            if (child.Success)
            {
                var achievementListModule = AchievementListModule.Instance();
                var list = achievementListModule->WatchList.ToArray().Where(x => x != 0);
                var trackerResults = tracker.get();
                
                ImGui.Text($"Refreshes in: {reloadTick}");
                
                foreach (var id in list)
                {
                    if (trackerResults.ContainsKey(id)) {
                        var dict = trackerResults[id];
                        var achievement = Plugin.DataManager.GetExcelSheet<Achievement>().GetRow(id);
                        if (dict.isLoading)
                        {
                            ImGui.Text($"{achievement.Name.ExtractText()}: LOADING...");
                        }
                        else
                        {
                            ImGui.Text($"{achievement.Name.ExtractText()}:");
                            ImGui.SameLine();
                            var min = (float)dict.Min;
                            var max = (float)dict.Max;
                            ImGui.ProgressBar(min / max, new Vector2(128, 16));
                        }
                        
                    }
                }
            }
        }
    }
}
