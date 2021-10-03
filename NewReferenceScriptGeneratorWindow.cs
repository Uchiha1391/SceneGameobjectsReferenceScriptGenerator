﻿using System;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ES3Types;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using UnityEditor;
using UnityEngine.SceneManagement;
using static System.String;

public class NewReferenceScriptGeneratorWindow : OdinEditorWindow
{




    [Required]
    public GameObject RootGameObject;

    [TabGroup("Generate References Script")] [Space(15.0f)] [Required]
    public List<(string ClassName,GameObject parentGameObject, List<GameObject> ObjectsList)> _ScriptNameAndObjectsList =
        new List<(string ClassName, GameObject parentGameObject, List<GameObject> ObjectsList)>();

    [TabGroup("Extend References Script")]
    [Space(15.0f)]
    [Required]
    [SerializeField]
    private GameObject OriginalScriptObject;
    [TabGroup("Extend References Script")] [Space(15.0f)] [Required]
    public List<GameObject> ExtendScriptObjectList = new List<GameObject>();



    [HideInInspector] public List<(string, GameObject parentGameObject)> _refreshedClassesNames =
        new List<(string, GameObject parentGameObject)>();


    [MenuItem("My Ui Commands/Open_NewReferenceScriptGeneratorWindow")]
    private static void ShowWindow()
    {
        GetWindow<NewReferenceScriptGeneratorWindow>().Show();
    }


    //[Button]
    //void testing()
    //{
    //    var tt = Selection.activeGameObject;

    //    var tts = tt.GetComponent<Animation>();

    //    foreach (AnimationState o in tts)
    //    {

    //        var pp = AnimationUtility.GetCurveBindings(o.clip);
    //        foreach (var editorCurveBinding in pp)
    //        {
    //            var goname = AnimationUtility.GetAnimatedObject(tt, editorCurveBinding);
    //            Debug.Log(goname.name);
    //        }
    //    }

    //}

    [Button, TabGroup("Generate References Script")]
    public void GenerateScript()
    {
        // adding ssuffix to the names of scripts. damn tuples
        if (_ScriptNameAndObjectsList == null)

        {
            Debug.LogError("fill _ScriptNameAndObjectsList First");
            return;
        }

        for (var index = 0; index < _ScriptNameAndObjectsList.Count; index++)
        {
            var valueTuple = _ScriptNameAndObjectsList[index];

            if (IsNullOrEmpty(valueTuple.ClassName))
            {
                Debug.LogError(
                    "empty string is not a valid name for class, So not generating it");
                continue;
            }

            valueTuple.ClassName += "ReferenceSingleton";
            _ScriptNameAndObjectsList[index] = valueTuple;
        }

        foreach (var (className, parentGameObject, objectsList) in _ScriptNameAndObjectsList)
        {
            GenerateScriptInternal(className, objectsList, false);
        }
    }

    [Button, TabGroup("Extend References Script")]
    public void ExtendScript()
    {
        var referenceSingleton = OriginalScriptObject.GetComponent<IReferenceSingleton>();
        if (referenceSingleton == null)
        {
            Debug.LogError(
                "The original gameobject doesn't have any existing script that derive from IReferenceSingleton");
            return;
        }

        var exisitinGiveAllObjectReferenes = referenceSingleton.GiveAllObjectReferenes();
        var ExistingScriptName = referenceSingleton.getScriptName();
        List<GameObject> NewobjectsList = new List<GameObject>();
        NewobjectsList.AddRange(exisitinGiveAllObjectReferenes);
        foreach (var o in ExtendScriptObjectList)
        {
            var b=NewobjectsList.Contains(o);
            if(b)
            {
                Debug.LogWarning($"{o.name}  was already present in original script references.");
                continue;
            }

            NewobjectsList.Add(o);
        }
    
        GenerateScriptInternal(ExistingScriptName, NewobjectsList, true);
    }

    [Button,TabGroup("Extend References Script")]
    public void FillReferencesForExtendscript()
    {
        var referenceSingleton = OriginalScriptObject.GetComponent<IReferenceSingleton>();
        if (referenceSingleton == null)
        {
            Debug.LogError(
                "The original gameobject doesn't have any existing script that derive from IReferenceSingleton");
            return;
        }
        var ExistingScriptName = referenceSingleton.getScriptName();
        FillrereferneceForExtendAndRefreshInternal((ExistingScriptName, OriginalScriptObject));
    }

    private void GenerateScriptInternal(string ClassName,
        List<GameObject> gameObjectList,bool overwirteExistingScript)
    {
        var newclassFilePath = Application.dataPath + $"\\Real project\\{ClassName}.cs";

        if (!overwirteExistingScript)
        {
            if (File.Exists(newclassFilePath))
            {
                Debug.LogWarning("File already exist and overwrite is off, so returning...");
                return;
            }
        }



        StringBuilder pp = new StringBuilder();
        pp.Append($@" 
using System;
using System.Linq;
using System.Collections.Generic;

using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
public class {ClassName} : MonoBehaviour,IReferenceSingleton
{{");

        List<GameObject> fileteredGameObjects = new List<GameObject>();
        foreach (var child in gameObjectList)
        {
            var b = CheckIfGameobjectpresentInOthereReferenceSignletonScript(child, RootGameObject);

            if (b)
            {
                continue;
            }
            fileteredGameObjects.Add(child);
        }

        if(fileteredGameObjects.Count==0)return;
        
        foreach (var child in fileteredGameObjects)
        {

           

            string childName = child.name;

            var tttt = gameObjectList.Where(N => N.name == childName).ToList();
            if (tttt.Count > 1)
            {
                for (var Index = 0; Index < tttt.Count; Index++)
                {
                    var smaeName = tttt[Index];
                    smaeName.name = smaeName.name + Index;
                }
            }

            var processedChildName = ProcessGameObjectsNames(child.name);
            child.name = processedChildName;
            pp.Append("public GameObject " + processedChildName + ";");
        }

        pp.Append($@"
    public static {ClassName} Singleton;

   private void Awake()
    {{
        
        CreateSingleton();

    }}



    private void CreateSingleton()
    {{
        if (Singleton == null)
        {{
            Singleton = this;
        }}
        else
        {{
            Destroy(this);
        }}
    }}

   /// <inheritdoc />
    public List<GameObject> GiveAllObjectReferenes()
    {{
        List < FieldInfo > fieldInfosList = GetType().GetFields().ToList();
        List<FieldInfo> FilteredfieldInfosList = new List<FieldInfo>();

        foreach (var fieldInfoInsatance in fieldInfosList)
        {{
            if (fieldInfoInsatance.IsStatic)
            {{
                continue;
            }}
            FilteredfieldInfosList.Add(fieldInfoInsatance);
        }}

        List<GameObject> gameObjectsReferencesList = new List<GameObject>();
        foreach (var fieldInfoInstace in FilteredfieldInfosList)
        {{
            object o = fieldInfoInstace.GetValue(this);

            if (o.Equals(null))
            {{
                continue;
            }}
            if (o is GameObject o1)
            {{
                gameObjectsReferencesList.Add(o1);
            }}
        }}

        return gameObjectsReferencesList;
    }}

    /// <inheritdoc />
    public string getScriptName()
    {{
        return GetType().Name;
    }}
}}


");


        string Contents = pp.ToString();

        File.Delete(newclassFilePath);

        File.WriteAllText(newclassFilePath, Contents);

        AssetDatabase.Refresh();
    }

    private string ProcessGameObjectsNames(string childName)
    {
        StringBuilder sb = new StringBuilder();
        foreach (char c in childName)
        {
            if ((c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') ||
                c == '_')
            {
                sb.Append(c);
            }
        }

        string trimmed = Concat(sb.ToString().Where(c => !Char.IsWhiteSpace(c)));

        return trimmed;
    }


    [Button, TabGroup("Generate References Script")]
    private void FillReferences()
    {
        foreach (var (NewClassName, parentGameObject, objectsList) in _ScriptNameAndObjectsList)
        {
            Debug.Log("FillReferences called sucessful");
            var newClassType = GetNewlyGeneratedScriptType(NewClassName);
            if (newClassType == null) return;

            var componentInstance = parentGameObject.AddComponent(newClassType);


            foreach (var fieldInfo in newClassType.GetFields())
            {
                if (fieldInfo.IsStatic)
                    continue; // can't use bindingFlag.Instance as its not yet created. I mean its not in playmode this code runs in editor mode
                string fieldInfoName = fieldInfo.Name;

                var obj = objectsList.FirstOrDefault(n => n.name == fieldInfoName);
                if (obj == null)
                {
                    Debug.LogError("Maybe you forgot to compile.eroor caused by - Fieldname    " +
                                   fieldInfoName);
                    Debug.Break();
                    return;
                }

                fieldInfo.SetValue(componentInstance, obj.gameObject);
            }


            Debug.Log("script is sucessfully generated");
        }
    }

    private Type GetNewlyGeneratedScriptType(string classname)
    {
        var ActiveAssemblies = AppDomain.CurrentDomain.GetAssemblies();


        Assembly assemblyWhichContainType =
            ActiveAssemblies.FirstOrDefault(n => n.GetType(classname) != null);

        if (assemblyWhichContainType == null)
        {
            Debug.LogError(" cannot find the assembly for the newly generated type");
            return null;
        }

        var newClassType = assemblyWhichContainType.GetType(classname);

        return newClassType;
    }

    [Button, TabGroup("Refresh References Script")]
    public void RefreshScriptsForNameChangesInHierircy()
    {
        List<GameObject> goReferences = new List<GameObject>();
        var rootGameObject = RootGameObject;
        if (rootGameObject == null)
        {
            Debug.LogError("Fill the root gamoject field");
            return;
        }

        var generatedSciptInstances =
            rootGameObject.GetComponentsInChildren<IReferenceSingleton>(true);
        if (generatedSciptInstances.Length == 0)
        {
            Debug.Log("No script to refresh :)");
        }

        foreach (var generatedSciptInstance in generatedSciptInstances)
        {
            goReferences.Clear();

            goReferences.AddRange(generatedSciptInstance.GiveAllObjectReferenes());

            var className = generatedSciptInstance.GetType().Name;
            _refreshedClassesNames.Add((className, goReferences[0]));
            GenerateScriptInternal(className, goReferences, true);
        }
    }

    [Button, TabGroup("Refresh References Script")]
    public void FillRererencesForRefreshedList()
    {
        foreach ((string classname, GameObject parentGameObject) valueTuple in
            _refreshedClassesNames)
        {
            FillrereferneceForExtendAndRefreshInternal(valueTuple);
        }

        Debug.Log("References are filled sucessfully");
    }

    private void FillrereferneceForExtendAndRefreshInternal((string classname, GameObject parentGameObject) valueTuple)
    {
        var newClassType = GetNewlyGeneratedScriptType(valueTuple.classname);


        DestroyImmediate(valueTuple.parentGameObject.GetComponent(newClassType));

        var componentInstance = valueTuple.parentGameObject.AddComponent(newClassType);

        foreach (var fieldInfo in newClassType.GetFields())
        {
            if (fieldInfo.IsStatic)
                continue; // can't use bindingFlag.Instance as its not yet created. I mean its not in playmode this code runs in editor mode
            string fieldInfoName = fieldInfo.Name;

            var obj = GameObject.Find(fieldInfoName);

            if (obj == null)
            {
                Debug.LogError(
                    "gameOobjec of this name not found.Maybe you forgot to compile.error caused by - Fieldname    " +
                    fieldInfoName);
                Debug.Break();
            }

            fieldInfo.SetValue(componentInstance, obj.gameObject);
        }
    }


    public bool CheckIfGameobjectpresentInOthereReferenceSignletonScript(GameObject obj,
        GameObject rootGameObject)
    {
        var generatedSciptInstances =
          rootGameObject.GetComponentsInChildren<IReferenceSingleton>(true);
        if (generatedSciptInstances.Length == 0)
        {
            Debug.Log("No script to refresh :)");
        }

        foreach (var generatedSciptInstance in generatedSciptInstances)
        {

            var goObjectReferenes = generatedSciptInstance.GiveAllObjectReferenes();
            if (goObjectReferenes.Contains(obj))
            {

                Debug.LogError(obj.name+" is already present in other reference singleton script. script name is"+generatedSciptInstance.getScriptName());
                return true;
            }
        }
        return false;

    }
    [Button, TabGroup("Generate References Script")]

    public void ClearReferenceSIngletionfromScriptName()
    {
        for (var index = 0; index < _ScriptNameAndObjectsList.Count; index++)
        {
            var valueTuple = _ScriptNameAndObjectsList[index];
           valueTuple.ClassName= valueTuple.ClassName.Replace("ReferenceSingleton", "");
            _ScriptNameAndObjectsList[index] = valueTuple;
        }
    }
}