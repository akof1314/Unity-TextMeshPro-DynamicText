using UnityEngine;
using System.Collections.Generic;
using System.Text;

namespace TMPro
{
    public class TMP_DynamicManager
    {
        private static TMP_DynamicManager s_Instance;

        private static TMP_DynamicManager instance
        {
            get
            {
                if (s_Instance == null)
                    s_Instance = new TMP_DynamicManager();
                return s_Instance;
            }
        }

        private Dictionary<TMP_FontAsset, Dictionary<uint, int>> m_FontCharacterLookupDictionary = new Dictionary<TMP_FontAsset, Dictionary<uint, int>>();
        private Dictionary<TMP_Text, string> m_TextOldCharacterDictionary = new Dictionary<TMP_Text, string>();
        private HashSet<char> m_CharMissingCharacters = new HashSet<char>();
        private StringBuilder m_StringBuilder = new StringBuilder();

        internal static void TextObjectForUpdate(TMP_Text textObject, bool isActive)
        {
            if (isActive)
            {
                try
                {
                    instance.InternalRegisterTextObjectForUpdate(textObject);
                }
                catch
                {
                    // ignored
                }
            }
            else
            {
                instance.InternalUnRegisterTextObjectForUpdate(textObject);
            }
        }

        private void InternalUnRegisterTextObjectForUpdate(TMP_Text textObject)
        {
            RemoveFontCharacterLookup(textObject);
        }

        private void InternalRegisterTextObjectForUpdate(TMP_Text textObject)
        {
            string text = textObject.text;
            string oldText;
            m_TextOldCharacterDictionary.TryGetValue(textObject, out oldText);
            if (text == oldText)
            {
                return;
            }

            RemoveFontCharacterLookup(textObject);
            m_TextOldCharacterDictionary.Add(textObject, text);

            m_CharMissingCharacters.Clear();
            var font = textObject.font;
            for (int i = 0; i < text.Length; i++)
            {
                if (!m_CharMissingCharacters.Contains(text[i]) && !HasCharacter(font, text[i]))
                {
                    m_CharMissingCharacters.Add(text[i]);
                }
            }

            if (m_CharMissingCharacters.Count == 0)
            {
                return;
            }

            font = GetDynamicFontAsset(font);
            if (font == null)
            {
                return;
            }

            Dictionary<uint, int> charRefDictionary;
            if (!m_FontCharacterLookupDictionary.TryGetValue(font, out charRefDictionary))
            {
                charRefDictionary = new Dictionary<uint, int>();
                m_FontCharacterLookupDictionary.Add(font, charRefDictionary);
            }

            m_StringBuilder.Length = 0;
            foreach (var charMissingCharacter in m_CharMissingCharacters)
            {
                int count;
                if (charRefDictionary.TryGetValue((uint)charMissingCharacter, out count))
                {
                    count++;
                    charRefDictionary[charMissingCharacter] = count;
                    continue;
                }

                count = 1;
                charRefDictionary.Add(charMissingCharacter, count);

                // 优化，如果此字符已经动态生成，就不再放到生成串里
                if (!(font.characterLookupTable != null && font.HasCharacter(charMissingCharacter)))
                {
                    m_StringBuilder.Append(charMissingCharacter);
                }
            }

            if (m_StringBuilder.Length == 0)
            {
                return;
            }

            if (font.characterLookupTable != null && !font.TryAddCharacters(m_StringBuilder.ToString()))
            {
                ResetFontAssetData(font);
            }
        }

        private void ResetFontAssetData(TMP_FontAsset font)
        {
            Dictionary<uint, int> charRefDictionary;
            if (!m_FontCharacterLookupDictionary.TryGetValue(font, out charRefDictionary))
            {
                return;
            }

            m_StringBuilder.Length = 0;
            foreach (var kv in charRefDictionary)
            {
                m_StringBuilder.Append((char)kv.Key);
            }

            font.ClearFontAssetData();
            if (!font.TryAddCharacters(m_StringBuilder.ToString()))
            {
                Debug.LogWarningFormat("{0} addCharacters fail", font.name);
            }
        }

        private void RemoveFontCharacterLookup(TMP_Text textObject)
        {
            string oldText;
            if (!m_TextOldCharacterDictionary.TryGetValue(textObject, out oldText))
            {
                return;
            }
            m_TextOldCharacterDictionary.Remove(textObject);

            m_CharMissingCharacters.Clear();
            for (int i = 0; i < oldText.Length; i++)
            {
                if (!m_CharMissingCharacters.Contains(oldText[i]))
                {
                    m_CharMissingCharacters.Add(oldText[i]);
                }
            }

            var font = GetDynamicFontAsset(textObject.font);
            if (font == null)
            {
                return;
            }

            Dictionary<uint, int> charRefDictionary;
            if (!m_FontCharacterLookupDictionary.TryGetValue(font, out charRefDictionary))
            {
                return;
            }

            foreach (var charMissingCharacter in m_CharMissingCharacters)
            {
                int count;
                if (charRefDictionary.TryGetValue(charMissingCharacter, out count))
                {
                    count--;
                    if (count <= 0)
                    {
                        charRefDictionary.Remove(charMissingCharacter);
                    }
                    else
                    {
                        charRefDictionary[charMissingCharacter] = count;
                    }
                }
            }
            m_CharMissingCharacters.Clear();
        }

        private bool HasCharacter(TMP_FontAsset font, char character)
        {
            if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                return false;
            }

            if (font.characterLookupTable.ContainsKey(character))
            {
                return true;
            }

            if (font.fallbackFontAssetTable != null && font.fallbackFontAssetTable.Count > 0)
            {
                for (int i = 0; i < font.fallbackFontAssetTable.Count && font.fallbackFontAssetTable[i] != null; i++)
                {
                    if (HasCharacter(font.fallbackFontAssetTable[i], character))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private TMP_FontAsset GetDynamicFontAsset(TMP_FontAsset font)
        {
            if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic)
            {
                return font;
            }

            if (font.fallbackFontAssetTable != null && font.fallbackFontAssetTable.Count > 0)
            {
                for (int i = 0; i < font.fallbackFontAssetTable.Count && font.fallbackFontAssetTable[i] != null; i++)
                {
                    if (font.fallbackFontAssetTable[i].atlasPopulationMode == AtlasPopulationMode.Dynamic)
                    {
                        return font.fallbackFontAssetTable[i];
                    }
                }
            }

            return null;
        }
    }
}
