using UnityEngine;

namespace TMPro
{
    [AddComponentMenu("Mesh/TextMeshPro - Text Dynamic")]
    public class TextMeshPro_Dynamic : TextMeshPro
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