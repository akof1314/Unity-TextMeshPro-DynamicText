using TMPro;
using UnityEngine;

public class Demo : MonoBehaviour
{
    public TMP_Text text;

    public void DoRegisterFontAssetDynamic()
    {
        TMP_DynamicManager.RegisterFontAssetDynamic(text);
    }

    public void DoCheckFontAssetDynamic()
    {
        TMP_DynamicManager.CheckFontAssetDynamic();
    }
}
