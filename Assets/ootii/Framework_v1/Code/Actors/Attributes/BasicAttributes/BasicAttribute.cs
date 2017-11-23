using System;
using UnityEngine;
using com.ootii.Data.Serializers;
using com.ootii.Helpers;

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Very basic inventory item
    /// </summary>
    [Serializable]
    public class BasicAttribute : ISerializationCallbackReceiver
    {
        /// <summary>
        /// Unique identifier
        /// </summary>
        public string _ID = "";
        public string ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        /// <summary>
        /// Stored value of the attribute;
        /// </summary>
        public object _Value = null;
        public object Value
        {
            get { return _Value; }
        }

        /// <summary>
        /// Stores the type of value we're expecting for the attribute
        /// </summary>
        public Type _ValueType = typeof(float);
        public Type ValueType
        {
            get { return _ValueType; }

            set
            {
                _ValueType = value;

                if (_ValueType == null)
                {
                    _Value = null;
                }
                else
                {
                    try
                    {
                        _Value = Convert.ChangeType(_Value, _ValueType);
                    }
                    catch
                    {
                        //if (_ValueType.IsValueType)
                        if (ReflectionHelper.IsValueType(_ValueType))
                        {
                            _Value = Activator.CreateInstance(_ValueType);
                        }
                        else
                        {
                            _Value = null;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Float value of the attribute
        /// </summary>
        public float FloatValue
        {
            get
            {
                float lValue = 0f;

                if (_ValueType == typeof(float))
                {
                    try
                    {
                        lValue = Convert.ToSingle(_Value);
                    }
                    catch { }
                }

                return lValue;
            }

            set
            {
                if (_ValueType == typeof(float))
                {
                    _Value = value;
                }
            }
        }

        /// <summary>
        /// Unity can't serialize 'Types'. So, we have to use the assembly qualified name.
        /// </summary>
        [SerializeField]
        protected string mSerializedType = "System.Single, mscorlib, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";

        /// <summary>
        /// Unity can't serialize 'objects'. So, we have to serialize the object value.
        /// </summary>
        [SerializeField]
        protected string mSerializedValue = "";

        /// <summary>
        /// Returns the value as the specified type
        /// </summary>
        /// <typeparam name="T">Type of value we are expecting to get back</typeparam>
        /// <returns></returns>
        public T GetValue<T>()
        {
            if (_Value != null)
            {
                if (ReflectionHelper.IsAssignableFrom(_ValueType, typeof(T)))
                {
                    return (T)_Value;
                }
            }

            return default(T);
        }

        /// <summary>
        /// Sets the value and potentially the type
        /// </summary>
        /// <typeparam name="T">Type of value we're setting</typeparam>
        /// <param name="rValue">Value that we'll set (if the types match or we change type).</param>
        /// <param name="rOverrideType">Determines if we can change the attribute type.</param>
        public void SetValue<T>(T rValue, bool rOverrideType = false)
        {
            Type lValueType = typeof(T);

            if (!rOverrideType)
            {
                if (lValueType != _ValueType) { return; }
            }

            _Value = (T)rValue;
            _ValueType = lValueType;
        }

        /// <summary>
        /// Sets key values before we serialize
        /// </summary>
        public void OnBeforeSerialize()
        {
            mSerializedType = (_ValueType == null ? "" : _ValueType.AssemblyQualifiedName);
            mSerializedValue = JSONSerializer.Serialize(_Value, false);
        }

        /// <summary>
        /// Sets key values after we serialize
        /// </summary>
        public void OnAfterDeserialize()
        {
            _ValueType = (mSerializedType.Length == 0 ? null : Type.GetType(mSerializedType));
            _Value = (mSerializedValue.Length == 0 ? null : JSONSerializer.Deserialize(mSerializedValue)); 
        }
    }
}
