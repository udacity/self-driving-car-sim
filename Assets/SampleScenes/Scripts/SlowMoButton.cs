using System;
using UnityEngine;
using UnityEngine.UI;

namespace UnityStandardAssets.SceneUtils
{
    public class SlowMoButton : MonoBehaviour
    {
        public Sprite FullSpeedTex;     // the ui texture for full speed
        public Sprite SlowSpeedTex;     // the ui texture for slow motion mode
        public float fullSpeed = 1;
        public float slowSpeed = 0.3f;
        public Button button;           // reference to the ui texture that will be changed


        private bool m_SlowMo;


       	void Start()
        {
			m_SlowMo = false;
        }

		void OnDestroy()
		{
			Time.timeScale = 1;
		}

        public void ChangeSpeed()
        {
            // toggle slow motion state
            m_SlowMo = !m_SlowMo;

            // update button texture
            var image = button.targetGraphic as Image;
            if (image != null)
            {
                image.sprite = m_SlowMo ? SlowSpeedTex : FullSpeedTex;
            }

            button.targetGraphic = image;

			Time.timeScale = m_SlowMo ? slowSpeed : fullSpeed;
        }
    }
}
