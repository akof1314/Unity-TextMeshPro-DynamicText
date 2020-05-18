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

        /// <summary>
        /// 子集对应的字使用次数
        /// </summary>
        private Dictionary<TMP_FontAsset, Dictionary<uint, int>> m_FontCharacterLookupDictionary = new Dictionary<TMP_FontAsset, Dictionary<uint, int>>();

        /// <summary>
        /// 动态也无法生成的字符，不再进行生成，前提是图集够大，不是因为动态字满了才导致失败
        /// </summary>
        private Dictionary<TMP_FontAsset, HashSet<char>> m_CharNonexistentCharacters = new Dictionary<TMP_FontAsset, HashSet<char>>();

        /// <summary>
        /// 用来缓存文本组件旧文本内容，防止重复去设置
        /// </summary>
        private Dictionary<TMP_Text, string> m_TextOldCharacterDictionary = new Dictionary<TMP_Text, string>();

        /// <summary>
        /// 将静态字集里面不存在的字，临时放在这个列表
        /// </summary>
        private HashSet<char> m_CharMissingCharacters = new HashSet<char>();

        /// <summary>
        /// 将要进行动态添加的字符
        /// </summary>
        private List<char> m_CharTryAddCharacters = new List<char>();

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

            HashSet<char> nonexistentCharacters;
            if (!m_CharNonexistentCharacters.TryGetValue(font, out nonexistentCharacters))
            {
                nonexistentCharacters = new HashSet<char>();
                m_CharNonexistentCharacters.Add(font, nonexistentCharacters);
            }

            m_CharTryAddCharacters.Clear();
            foreach (var charMissingCharacter in m_CharMissingCharacters)
            {
                // 如果已经被使用过，则使用计数+1
                int count;
                if (charRefDictionary.TryGetValue((uint)charMissingCharacter, out count))
                {
                    count++;
                    charRefDictionary[charMissingCharacter] = count;
                    continue;
                }
                
                // 无法生成的字符，不计算
                if (nonexistentCharacters.Contains(charMissingCharacter))
                {
                    continue;
                }

                count = 1;
                charRefDictionary.Add(charMissingCharacter, count);

                // 优化，如果此字符已经动态生成，就不再放到生成串里
                if (!(font.characterLookupTable != null && font.HasCharacter(charMissingCharacter)))
                {
                    m_CharTryAddCharacters.Add(charMissingCharacter);
                }
            }

            if (m_CharTryAddCharacters.Count == 0)
            {
                return;
            }

            if (font.characterLookupTable != null)
            {
                m_CharMissingCharacters.Clear();
                bool ret = font.TryAddCharacters(m_CharTryAddCharacters, m_CharMissingCharacters);
                MissingCharactersToNonexistent(font, nonexistentCharacters, charRefDictionary);

                if (!ret)
                {
                    ResetFontAssetData(font);
                }
            }
        }

        private void ResetFontAssetData(TMP_FontAsset font)
        {
            Dictionary<uint, int> charRefDictionary;
            if (!m_FontCharacterLookupDictionary.TryGetValue(font, out charRefDictionary))
            {
                return;
            }

            m_CharTryAddCharacters.Clear();
            foreach (var kv in charRefDictionary)
            {
                m_CharTryAddCharacters.Add((char)kv.Key);
            }

            HashSet<char> nonexistentCharacters;
            if (!m_CharNonexistentCharacters.TryGetValue(font, out nonexistentCharacters))
            {
                nonexistentCharacters = new HashSet<char>();
                m_CharNonexistentCharacters.Add(font, nonexistentCharacters);
            }


            font.ClearFontAssetData();
            m_CharMissingCharacters.Clear();
            bool ret = font.TryAddCharacters(m_CharTryAddCharacters, m_CharMissingCharacters);
            MissingCharactersToNonexistent(font, nonexistentCharacters, charRefDictionary);
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

        private void MissingCharactersToNonexistent(TMP_FontAsset font, HashSet<char> nonexistentCharacters, Dictionary<uint, int> charRefDictionary)
        {
            if (m_CharMissingCharacters.Count > 0)
            {
                m_StringBuilder.Length = 0;
                foreach (var charMissingCharacter in m_CharMissingCharacters)
                {
                    if (!nonexistentCharacters.Contains(charMissingCharacter))
                    {
                        nonexistentCharacters.Add(charMissingCharacter);
                        charRefDictionary.Remove(charMissingCharacter);
                        m_StringBuilder.Append(charMissingCharacter);
                    }
                }
                Debug.LogWarningFormat("{0} addCharacters fail {1}", font.name, m_StringBuilder.ToString());
            }
        }
    }
}
