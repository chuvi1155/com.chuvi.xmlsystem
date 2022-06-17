using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEditor;
using UnityEngine;

public class AutoSettingsCreator : EditorWindow
{
#if CHUVI_EXTENSIONS
    [MenuItem("Chuvi/AutoSettingsCreator")]
    static void Init()
    {
        AutoSettingsCreator.GetWindow<AutoSettingsCreator>();
    }

    MonoBehaviour[] scripts;
    bool[] checks;
    Vector2 scrollPos;
    bool createValidated = false;

    private void OnGUI()
    {
        if (GUILayout.Button("Update"))
            scripts = null;

        if (scripts == null || checks == null || checks.Length != scripts.Length)
        {
            scripts = FindObjectsOfType<MonoBehaviour>();
            List<MonoBehaviour> uniq = new List<MonoBehaviour>();
            for (int i = 0; i < scripts.Length; i++)
            {
                if (!uniq.Exists(u => u.GetType() == scripts[i].GetType()))
                    uniq.Add(scripts[i]);
            }
            scripts = uniq.ToArray();
            checks = new bool[scripts.Length];
        }

        EditorGUILayout.BeginVertical("box");
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
        for (int i = 0; i < scripts.Length; i++)
        {
            checks[i] = EditorGUILayout.ToggleLeft($"{scripts[i].GetType().Name} ({scripts[i].name})", checks[i]);
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();

        createValidated = EditorGUILayout.ToggleLeft("Create validate method", createValidated);

        if (GUILayout.Button("Create settings (XML)"))
        {
            System.Globalization.CultureInfo ci = new System.Globalization.CultureInfo("en-US");
            Thread.CurrentThread.CurrentCulture = ci;
            Thread.CurrentThread.CurrentUICulture = ci;
            for (int i = 0; i < scripts.Length; i++)
                if (checks[i])
                    CreateSettingsXML(scripts[i]);
        }
    }

    void CreateSettingsXML(MonoBehaviour script)
    {
        string path = AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(script));
        string scriptText = File.ReadAllText(path);

        var type = script.GetType();
        var fileds = type.GetFields(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);

        string[] lines = new string[fileds.Length];
        for (int i = 0; i < fileds.Length; i++)
        {
            var field = fileds[i];

            lines[i] = $"\t\t{field.Name} = XMLSystem.Settings.XMLSettings.GetValueWithAdd(\"{type.Name}\", \"{field.Name}\", {field.Name}{(createValidated ? ", OnSettingsValidate" : "")});";

            if (scriptText.Contains(lines[i]))
                lines[i] = "";
        }

        lines = System.Array.FindAll(lines, ln => !string.IsNullOrEmpty(ln) && !string.IsNullOrWhiteSpace(ln));

        string bodyStart = scriptText.After("Start()");

        if (string.IsNullOrEmpty(bodyStart))
            bodyStart = scriptText.After("Start ()");

        if (string.IsNullOrEmpty(bodyStart))
        {
            var bodyStartIndex = scriptText.IndexOf("}");
            scriptText = scriptText.Insert(bodyStartIndex + 1, "\n\tvoid Start()\n\t{\n\t// automatic generated code\n\t}\n\n");
        }

        bodyStart = scriptText.After("Start()").After("{").Before("}");
        int insertIndex = -1;
        if (string.IsNullOrWhiteSpace(bodyStart))
        {
            bodyStart = "\n\t\t// automatic generated code\n";
            insertIndex = scriptText.IndexOf("Start()");
            insertIndex = scriptText.IndexOf("{", insertIndex) + 1;
        }
        var settings = string.Join("\n", lines);
        if (string.IsNullOrEmpty(settings) || string.IsNullOrWhiteSpace(settings))
            return;
        settings = "\n\t\t// automatic generated code\n\n" + settings;
        var newbodyStart = bodyStart.Insert(0, settings);
        if (insertIndex == -1)
            scriptText = scriptText.Replace(bodyStart, newbodyStart);
        else
            scriptText = scriptText.Insert(insertIndex, newbodyStart);

        if (createValidated)
        {
            insertIndex = scriptText.IndexOf("}", insertIndex) + 1;
            string onValidateMethod = "\n\tprivate void OnValidate(string group, string key, string typeName, bool isValid)\n" +
                                      "\t{\n" +
                                      "\n" +
                                      "\t}\n";

            scriptText = scriptText.Insert(insertIndex, onValidateMethod);
        }

        File.WriteAllText(path, scriptText);
        AssetDatabase.WriteImportSettingsIfDirty(path);
    } 
#endif
}
