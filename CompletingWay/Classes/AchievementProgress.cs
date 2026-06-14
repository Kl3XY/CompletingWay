using Lumina.Excel.Sheets;

namespace SamplePlugin.Classes;

public unsafe class AchievementProgress
{
    public bool isLoading = true;
    public uint Min = 999;
    public uint Max = 999;

    public AchievementProgress(uint min, uint max, bool loading = true)
    {
        Min = min;
        Max = max;
        isLoading = loading;
    }
}
