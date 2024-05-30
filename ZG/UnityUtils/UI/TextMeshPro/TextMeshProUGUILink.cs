using System;
using UnityEngine;
using UnityEngine.Events;
using TMPro;
using UnityEngine.EventSystems;

namespace ZG
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextMeshProUGUILink : MonoBehaviour, IPointerClickHandler
    {
        public event Action<string, PointerEventData> clickEvent;
        
        public StringEvent onClick;
        private TextMeshProUGUI __text;

        public void SetText(string text)
        {
            __text.SetText(text);
        }

        public void SetText(System.Text.StringBuilder text)
        {
            __text.SetText(text);
        }

        protected void Awake()
        {
            __text = GetComponent<TextMeshProUGUI>();
        }

        void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
        {
            if (onClick == null)
                return;

            int linkIndex = TMP_TextUtilities.FindIntersectingLink(__text, eventData.position, eventData.enterEventCamera);
            if (linkIndex == -1)
                return;

            // was a link clicked?
            TMP_LinkInfo linkInfo = __text.textInfo.linkInfo[linkIndex];

            var id = linkInfo.GetLinkID();
            // open the link id as a url, which is the metadata we added in the text field
            onClick.Invoke(id);

            if (clickEvent != null)
                clickEvent(id, eventData);
        }
    }
}