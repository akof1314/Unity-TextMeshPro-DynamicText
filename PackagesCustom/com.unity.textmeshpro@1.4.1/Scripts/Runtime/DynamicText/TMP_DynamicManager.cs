using System.Collections.Generic;

namespace TMPro
{
    public static class TMP_DynamicManager
    {
        private static readonly List<TMP_FontAsset> sDynamicFontAssets = new List<TMP_FontAsset>();

        public static void RegisterFontAssetDynamic(TMP_Text textObject)
        {
            if (!textObject)
            {
                return;
            }
            var font = textObject.font;
            if (!font)
            {
                return;
            }

            if (font.fallbackFontAssetTable != null && font.fallbackFontAssetTable.Count > 0)
            {
                for (int i = 0; i < font.fallbackFontAssetTable.Count && font.fallbackFontAssetTable[i] != null; i++)
                {
                    if (font.fallbackFontAssetTable[i].atlasPopulationMode == AtlasPopulationMode.Dynamic &&
                        font.fallbackFontAssetTable[i].isMultiAtlasTexturesEnabled)
                    {
                        if (!sDynamicFontAssets.Contains(font.fallbackFontAssetTable[i]))
                        {
                            sDynamicFontAssets.Add(font.fallbackFontAssetTable[i]);
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 在切场景的时候，进行检查，多少张进行清除根据实际情况修改
        /// </summary>
        public static void CheckFontAssetDynamic()
        {
            foreach (var fontAsset in sDynamicFontAssets)
            {
                if (fontAsset.atlasTextureCount > 2)
                {
                    fontAsset.ClearFontAssetData();
                }
            }
        }
    }
}
