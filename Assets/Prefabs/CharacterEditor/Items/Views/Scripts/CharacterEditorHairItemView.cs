using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Catalog;
using UnityEngine.UI;

namespace CharacterEditor
{
    public class CharacterEditorHairItemView : ItemView
    {
        [SerializeField] private Sprite _baseDefault;
        [SerializeField] private Sprite _baseActive;
        [SerializeField] private GameObject _baseObject;
        [SerializeField] private GameObject _backGlow;
        [SerializeField] private GameObject _selector;
        [SerializeField] private GameObject _thumbnail;
	
        private Image _baseImage;

        public override void Init(Item item)
        {
            base.Init(item);


            _thumbnail.GetComponent<Image>().sprite = item.Image;
            _baseImage = _baseObject?.GetComponent<Image>();

            if (_baseImage != null)
                _baseImage.sprite = _baseDefault;

            _backGlow?.SetActive(false);
            _selector?.SetActive(false);
        }

        public override void OnSelect()
        {
            if (_baseImage != null)
                _baseImage.sprite = _baseActive;

            _backGlow?.SetActive(true);
            _selector?.SetActive(true);
        }

        public override void OnDeselect()
        {
            if (_baseImage != null)
                _baseImage.sprite = _baseDefault;

            _backGlow?.SetActive(false);
            _selector?.SetActive(false);
        }
    }
}
