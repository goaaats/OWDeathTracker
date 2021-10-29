using UnityEngine;
using UnityEngine.UI;

namespace Goaaats.DeathTracker
{
    class DeathMarker : MonoBehaviour
    {
        private Transform canvasTransform;

        private Text infoText;
        private Text nameText;

        private Transform target;

        public string InfoLabelContent;
        public string NameLabelContent;

        private void Start()
        {
            canvasTransform = transform.GetComponentInChildren<Canvas>().transform;

            var texts = transform.GetComponentsInChildren<Text>();
            infoText = texts[0];
            nameText = texts[1];

            target = Locator.GetPlayerTransform();

            var font = Resources.Load<Font>(@"fonts/english - latin/SpaceMono-Regular_Dynamic");

            infoText.text = InfoLabelContent;
            infoText.font = font;

            nameText.text = NameLabelContent;
            nameText.font = font;
        }

        private void LateUpdate()
        {
            //transform.forward = target.forward;
            //transform.Rotate(Vector3.right, 90);
            canvasTransform.rotation = target.rotation;
        }
    }
}
