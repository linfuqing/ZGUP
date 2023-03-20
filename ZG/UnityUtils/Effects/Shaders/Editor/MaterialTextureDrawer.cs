using UnityEngine;
using UnityEditor;

public class MaterialTextureDrawer : MaterialPropertyDrawer
{
    private readonly string __keyword;

    public MaterialTextureDrawer(string keyword)
    {
        __keyword = keyword;
    }

    public override float GetPropertyHeight(MaterialProperty prop, string label, MaterialEditor editor)
    {
        if (prop.type == MaterialProperty.PropType.Texture)
            return MaterialEditor.GetDefaultPropertyHeight(prop);

        return base.GetPropertyHeight(prop, label, editor);
    }

    public override void OnGUI(Rect position, MaterialProperty prop, string label, MaterialEditor editor)
    {
        if(prop.type == MaterialProperty.PropType.Texture)
        {
            //EditorGUI.BeginChangeCheck();
            Texture texture = editor.TextureProperty(position, prop, label);
            //if (EditorGUI.EndChangeCheck())
            {
                Material material;
                Object[] targets = prop.targets;
                int numTargets = targets.Length, i;
                if (texture == null)
                {
                    for (i = 0; i < numTargets; i++)
                    {
                        material = (Material)targets[i];

                        material.DisableKeyword(__keyword);

                        EditorUtility.SetDirty(material);
                    }
                }
                else
                {
                    for (i = 0; i < numTargets; i++)
                    {
                        material = (Material)targets[i];

                        material.EnableKeyword(__keyword);

                        EditorUtility.SetDirty(material);
                    }
                }
            }
        }
        else
            base.OnGUI(position, prop, label, editor);
    }
}
