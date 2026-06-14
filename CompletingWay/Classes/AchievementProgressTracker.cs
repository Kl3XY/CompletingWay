using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.Game.UI;
using FFXIVClientStructs.Havok.Common.Serialize.Util;

namespace SamplePlugin.Classes;

public unsafe class AchievementProgressTracker
{
    private Queue<ushort> idQueue = new Queue<ushort>();
    private Dictionary<ushort, AchievementProgress> results = new();
    private bool loading = true;
    private ushort currentRequest = 0;

    /*
     * accepts a list achievement ids and creates placeholder AchievementProgresses with it.
     */
    public void fillList(List<ushort> achievements)
    {
        results.Clear();
        idQueue.Clear();
        foreach (var a in achievements)
        {
            results.Add(a, new AchievementProgress(0, 0));
            idQueue.Enqueue(a);
        }

        getNextEntry();
    }

    /*
     * Gets the results
     */
    public int GetResultCount()
    {
        return results.Count;
    }
    
    /*
     * Starts a refreshCycle
     */
    public void Refresh()
    {
        getNextEntry();
    }

    
    /*
     * Get all entries.
     */
    public Dictionary<ushort, AchievementProgress> get()
    {
        return results;
    }

    /*
     * Prepare getting the next entry.
     *
     * This is also the proper starting point.
     */
    public void getNextEntry()
    {

        // Get the Achievement Getter instance.
        /*
         * The Reason why this is so much more difficult than it suppose to be is that this piece of shit can only load
         * One Achievement at a time.
         */
        var achievement = Achievement.Instance();
        if (achievement == null) return;
        if (idQueue.Count == 0) return;
        
        currentRequest = idQueue.Dequeue();

        Plugin.Log.Information($"Getting Achievement Progress for: {currentRequest}");
        
        // Request the Achievement Progress.
        achievement->RequestAchievementProgress(currentRequest);
        loading = false;
    }
    
    /*
     * Run this in a Framework update.
     *
     * This is where we retrieve the values from either "GetNextEntry" or whatever
     */
    public void onUpdate()
    {
        // Don't get the next entry yet fam.
        if (loading) return;
        
        var achievement = Achievement.Instance();
        if (achievement == null) return;

        // Achievement is loaded and we are now on the currently requested Achievement.
        if (achievement->ProgressAchievementId == currentRequest &&
            achievement->State == Achievement.AchievementState.Loaded)
        {
            loading = true;
            results[currentRequest] = new AchievementProgress(achievement->ProgressCurrent, achievement->ProgressMax, false);
            
            getNextEntry();
        }
    }
}
