using Misc.EditorHelpers;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Misc
{

    public class SingleObject : MonoBehaviour
    {
        protected static List<SingleObject> all;

        [SerializeField] private string _uniqueID;
        public string uniqueID
        {
            get => _uniqueID;
            set => _uniqueID = value;
        }

        [SerializeField] private bool _prioritizePrevious;
        public bool prioritizePrevious
        {
            get => _prioritizePrevious;
            set
            {
                if (_prioritizePrevious != value)
                {
                    Validate.UpdateField(this, nameof(_prioritizePrevious), _prioritizePrevious = value);
                    CheckUnique();
                }
            }
        }

        [SerializeField] private DisableMode _disableMode;
        public DisableMode disableMode
        {
            get => _disableMode;
            set => _disableMode = value;
        }

        public enum DisableMode
        {
            DestroyGameObject = 0,
            DeactivateGameObject = 1,
            DestroyComponent = 2,
            DisableComponent = 3
        }

        protected virtual void OnValidate()
        {
            Validate.FieldWithProperty(this, nameof(_uniqueID), nameof(uniqueID));
        }

        protected virtual void OnEnable()
        {
            CheckUnique();

            all.Add(this);
        }

        protected virtual void OnDisable()
        {
            all.Remove(this);
        }

        protected virtual void CheckUnique()
        {
            foreach (SingleObject other in all)
            {
                if (other && other.uniqueID == uniqueID)
                {
                    if (_prioritizePrevious)
                    {
                        Disable();
                        return;
                    }
                    else
                        other.Disable();
                }
            }
        }

        protected virtual void Disable()
        {
            switch (disableMode)
            {
                case DisableMode.DeactivateGameObject:
                    gameObject.SetActive(false);
                    break;

                case DisableMode.DestroyComponent:
                    Destroy(this);
                    break;

                case DisableMode.DisableComponent:
                    enabled = false;
                    break;

                default:
                    Destroy(gameObject);
                    break;
            }
        }
    }
}
