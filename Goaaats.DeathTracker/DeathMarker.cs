using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace Goaaats.DeathTracker
{
    class DeathMarker : MonoBehaviour
    {

        private GameObject FaceSprite;
        private GameObject Text;

        private Transform target;

        private Quaternion initialRotation;

        private void Start()
        {
            initialRotation = transform.localRotation;

            FaceSprite = GetComponentInChildren<SpriteRenderer>().gameObject;
            Text = GetComponentInChildren<Text>().gameObject;

            target = Locator.GetPlayerTransform();
        }

        private void LateUpdate()
        {
            //transform.forward = target.forward;
            //transform.Rotate(Vector3.right, 90);
            Text.transform.rotation = target.rotation;
        }

        public void SetupData(string info)
        {
            var text = GetComponentInChildren<Text>();
            text.text = info;
            text.font = Resources.Load<Font>(@"fonts/english - latin/SpaceMono-Regular_Dynamic");
        }
    }
}
