using System;
using UnityEngine;

namespace CupkekGames.Character
{
    public enum AnimationDatabaseClipType
    {
        None = -1,

        // NPC
        Walking = 0,
        WalkingFemale = 1,
        Waving = 2,
        Talking01 = 3,
        Talking02 = 4,
        JumpForJoy = 5,
        Yay = 6,
        Cry = 7,
        MotivatedPose = 8,
        Cheer1 = 9,
        Cheer2 = 10,
        Cheer3 = 11,
        Bored = 12,
        Horror01 = 13,
        Exclamation01 = 14,

        // Scout
        Harvesting = 1000,
        Mining = 1001,
        GatherStanding = 1002,
        GatherKneeling = 1003,
        ChestOpen = 1004,
    }
}