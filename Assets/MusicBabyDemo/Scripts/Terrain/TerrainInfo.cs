
#if UNITY_EDITOR
using MidiPlayerTK;
using UnityEditor;
using UnityEngine;
namespace MusicRun
{
    class TerrainInfo : Editor
    {
        [MenuItem("Tools/Display all terrains", false, 71)]
        static void DisplayAllTerrains(MenuCommand menuCommand)
        {
            var tg = Object.FindFirstObjectByType<TerrainGenerator>();
            Display(tg);
        }

        [MenuItem("Tools/Display active terrains", false, 72)]
        static void DisplayEnabledTerrains(MenuCommand menuCommand)
        {
            var tg = Object.FindFirstObjectByType<TerrainGenerator>();
            Display(tg, false);
        }

        [MenuItem("Tools/Display detailed terrains", false, 72)]
        static void DisplayDetailedTerrains(MenuCommand menuCommand)
        {
            var tg = Object.FindFirstObjectByType<TerrainGenerator>();
            Display(tg, false, true);
        }

        private static void Display(TerrainGenerator tg, bool all = true, bool displayChunk = false)
        {
            ImBank bnk = null;
            if (tg == null)
            {
                Debug.LogWarning("Aucun TerrainGenerator trouvé dans la scène.");
                return;
            }

            if (tg.levels == null || tg.levels.Length == 0)
            {
                Debug.Log("TerrainGenerator.levels est vide.", tg);
                return;
            }
            if (!Application.isPlaying)
            {
                // Load description of available soundfont
                if (MidiPlayerGlobal.CurrentMidiSet == null || MidiPlayerGlobal.CurrentMidiSet.ActiveSounFontInfo == null)
                {
                    MidiPlayerGlobal.InitPath();
                    ToolsEditor.LoadMidiSet();
                    ToolsEditor.CheckMidiSet();
                }
                // Exec after Refresh, either cause errror
                if (MidiPlayerGlobal.ImSFCurrent == null)
                    MidiPlayerGlobal.LoadCurrentSF();
                bnk = MidiPlayerGlobal.ImSFCurrent.Banks[0];
            }

            for (int i = 0; i < tg.levels.Length; i++)
            {
                var lvl = tg.levels[i];
                string info = "";
                if (all || lvl.enabled)
                {
                    info += $"{i} ";
                    if (all)
                    {
                        info += lvl.enabled ? "ENABLED " : "DISABLE ";
                    }
                    info += $"name:{Fixed(lvl.name, 15)} ";

                    string substInstr = lvl.SearchForInstrument ? Fixed(bnk.defpresets[lvl.SubstitutionInstrument].Name, 18) : "'No instr. search'";
                    string midiFile = Fixed(MidiPlayerGlobal.CurrentMidiSet.MidiFiles[lvl.indexMIDI], 15);
                    info +=
                        $"MIDI:{lvl.indexMIDI,2} " + $"{midiFile} " +
                        $"loop:{lvl.LoopsToGoal} {lvl.SubstitutionInstrument} {substInstr} " +
                        $"speed:{lvl.MinSpeedMusic:F2}-{lvl.MaxSpeedMusic:F2} ratio:{lvl.RatioSpeedMusic:F2} " +
                        $"Goal:{lvl.deltaGoalChunk} Sky:{Fixed(lvl.Skybox.name, 20)} ";
                    Debug.Log(info);
                    if (displayChunk)
                    {
                        foreach (GameObject chunk in lvl.runChunks)
                            Debug.Log($"   {chunk.name}");
                    }
                }
            }
        }
        static string Fixed(string s, int length)
        {
            s ??= string.Empty;
            string f = s.Length >= (length - 2) ? s.Substring(0, length - 2) : s;
            f = $"'{f}'";
            if (s.Length < length) f = f.PadRight(length);
            return f;
        }
    }
}
#endif
