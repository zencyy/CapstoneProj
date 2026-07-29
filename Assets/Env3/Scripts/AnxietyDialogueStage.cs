using System;
using UnityEngine;

namespace Env3.Anxiety
{
    /// <summary>One beat of the conversation: what MainDude says, and what the player can blurt back.</summary>
    [Serializable]
    public class AnxietyDialogueStage
    {
        [Tooltip("Editor-only label so the stage list is readable in the inspector.")]
        public string label = "Stage";

        [Tooltip("Name shown before the line, as 'Name: line'. Leave empty and the line is treated as the player's own thought: no name, wrapped in brackets.")]
        public string speakerName = "Sean Tay";

        [TextArea(2, 4)]
        [Tooltip("What is said to open this stage.")]
        public string npcLine = "";

        [Tooltip("Answers offered to the player. On a panic stage these are cycled through as the screen floods.")]
        public string[] choices = new string[0];

        [Tooltip("Seconds before the player is forced to answer. 0 = no timer.")]
        public float answerTimeLimit = 0f;

        [Range(0f, 1f)]
        [Tooltip("Anxiety this stage settles at. Drives vignette, drone, head-lock, tremble and heartbeat rate.")]
        public float anxiety = 0f;

        [Tooltip("Seconds between each choice appearing.")]
        public float choiceRevealInterval = 0.18f;

        [Tooltip("Panic stage: choices flood the whole panel at accelerating speed and the stage ends on a timer, not a click.")]
        public bool panicFlood = false;
    }
}
