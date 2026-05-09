using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace Island
{

    namespace Builder
    {

        namespace HUD
        {

            public class PainterModeButton : MonoBehaviour
            {

                [SerializeField] private PainterMode _painterMode;
                public bool Active = false;

                public void OnClick()
                {
                    BuilderManager.Instance.UpdatePainterMode(_painterMode);
                }

            }


        }

    }
}
