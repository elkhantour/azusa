using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;


namespace Island
{

    namespace Builder
    {


        public class CatalogItem : MonoBehaviour
        {

            [SerializeField] private string _name;
            [SerializeField] private Sprite _visual;

            public void Init(string name, Sprite visual)
            {
                _name = name;
                _visual = visual;

                Image img = gameObject.transform.Find("Visual").GetComponent<Image>();

                if (img == null)
                {
                    Debug.LogError($"Could not find the Visual game object for the catalog item {_name}");
                    return;
                }

                if(_visual != null){
                    img.sprite = _visual;
                }
            }


        }


    }

}
