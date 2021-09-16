using System;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text;
using Sirenix.OdinInspector;

public class SceneGameobjectsReferenceScriptGenerator : MonoBehaviour
{
    [Button]
    public void GenerateSciptWithNames()
    {
        var AllChildren = gameObject.transform.root.GetComponentsInChildren<Transform>(true);


        StringBuilder pp = new StringBuilder();
        pp.Append(@" 
using System;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using UnityEngine;
public class MainSceneGameObjectReferences : MonoBehaviour
{");


        foreach (var child in AllChildren)
        {
            string ChildName = child.name;

            var tttt = AllChildren.Where(N => N.name == ChildName).ToList();
            if (tttt.Count > 1)
            {
                for (var Index = 0; Index < tttt.Count; Index++)
                {
                    var smaeName = tttt[Index];
                    smaeName.name = smaeName.name + Index;
                }
            }

            string trimmed = String.Concat(child.name.Where(c => !Char.IsWhiteSpace(c)));

            pp.Append("public GameObject " + trimmed + ";");
        }

        pp.Append(@"
    public static MainSceneGameObjectReferences Singleton;

   private void Start()
    {
        CreateSingleton();

    }



    private void CreateSingleton()
    {
        if (Singleton == null)
        {
            Singleton = this;
        }
        else
        {
            Destroy(this);
        }
    }

    [Button]
    public void FillReferences()
    {
        var AllChildren = transform.root.GetComponentsInChildren<Transform>(true);

        foreach (var Child in AllChildren)
        {
            string trimmed = String.Concat(Child.name.Where(c => !Char.IsWhiteSpace(c)));
            Child.name = trimmed;
        }

        foreach (var FieldInfo in this.GetType().GetFields())
        {

     if(FieldInfo.IsStatic)return; // can't use bindingFlag.Instance as its not yet created. I mean its not in playmode this code runs in editor mode
            string FieldInfoName = FieldInfo.Name;

            var obj = AllChildren.FirstOrDefault(n => { return n.name == FieldInfoName; });
            if (obj == null)
            {
                Debug.LogError(""Maybe you forgot to compile.eroor caused by-Fieldname    "" + FieldInfoName);
                Debug.Break();
                return;
            }

            FieldInfo.SetValue(this, obj.gameObject);
        }
    }
}
");
        string Contents = pp.ToString();

        File.WriteAllText(Application.dataPath + "\\Real project\\MainSceneGameObjectReferences.cs",
            Contents);

        Debug.Log("script is sucessfully generated");
    }
}