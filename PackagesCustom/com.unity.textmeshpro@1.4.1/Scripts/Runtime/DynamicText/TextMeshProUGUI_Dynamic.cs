using UnityEngine;

namespace TMPro
{
    [AddComponentMenu("UI/TextMeshPro - Text Dynamic (UI)", 12)]
    public class TextMeshProUGUI_Dynamic : TextMeshProUGUI
    {
        protected override void OnEnable()
        {
            base.OnEnable();
            DoDynamicUpdate();
        }

        protected override void OnDisable()
        {
            base.OnDisable();
            DoDynamicUpdate();
        }

        public override void SetVerticesDirty()
        {
            base.SetVerticesDirty();
            DoDynamicUpdate();
        }

        private void DoDynamicUpdate()
        {
            TMP_DynamicManager.TextObjectForUpdate(this,
                !(!m_isAwake || (this.IsActive() == false && m_ignoreActiveState == false)));
        }
    }
}