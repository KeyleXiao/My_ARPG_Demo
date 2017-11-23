using System;
using System.Collections.Generic;
using UnityEngine;
using com.ootii.Helpers;

namespace com.ootii.Actors.Attributes
{
    /// <summary>
    /// Creates a simple attribute system. That is both the "AttributeSource" and the
    /// character attributes.
    /// 
    /// If you use a more advanced attribute system, simply create an "AttributeSource" 
    /// that represents a bridge for your system.
    /// </summary>
    public class BasicAttributes : MonoBehaviour, IAttributeSource
    {
        /// <summary>
        /// List of inventory items
        /// </summary>
        public List<BasicAttribute> Items = new List<BasicAttribute>();

        /// <summary>
        /// Determines if the attribute exists.
        /// </summary>
        /// <param name="rItemID">String representing the name or ID of the attribute we're checking</param>
        /// <returns></returns>
        public virtual bool AttributeExists(string rAttributeID)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Compare(Items[i].ID, rAttributeID, true) == 0)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Determines if all the attributes in the comma delimited value exist.
        /// </summary>
        /// <param name="rAttributes">Comma delimited list of tags to test for</param>
        /// <param name="rRequireAll">Determines if all must exist or just one</param>
        /// <returns>True or false</returns>
        public virtual bool AttributesExist(string rAttributes, bool rRequireAll = true)
        {
            if (rAttributes == null || rAttributes.Length == 0) { return false; }

            int lCount = StringHelper.Split(rAttributes, ',');
            for (int i = lCount - 1; i >= 0; i--)
            {
                StringHelper.SharedStrings[i] = StringHelper.SharedStrings[i].Trim();

                bool lFound = false;
                for (int j = Items.Count - 1; j >= 0; j--)
                {
                    if (string.Compare(Items[j].ID, StringHelper.SharedStrings[i], true) == 0)
                    {
                        lFound = true;
                        break;
                    }
                }

                // If it's found, we may be done
                if (lFound)
                {
                    if (!rRequireAll)
                    {
                        return true;
                    }
                }
                // If it's not found, we may be done
                else if (rRequireAll)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Adds the attribute if it doesn't exist and returns it.
        /// </summary>
        /// <param name="rAttributeID">Attribute ID to add (if it doesn't exist)</param>
        /// <param name="rType">Type of attribute we'll add. It must be one of the supported types and null means tag.</param>
        /// <returns>Attribute that matches the ID</returns>
        public virtual BasicAttribute AddAttribute(string rAttributeID, Type rType = null)
        {
            // Return the existing attribute
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Compare(Items[i].ID, rAttributeID, true) == 0)
                {
                    return Items[i];
                }
            }

            // Add the new attribute
            BasicAttribute lAttribute = new BasicAttribute();
            lAttribute.ID = rAttributeID;
            lAttribute.ValueType = rType;

            Items.Add(lAttribute);

            return lAttribute;
        }

        /// <summary>
        /// Removes the specified attribute from the list of items
        /// </summary>
        /// <param name="rAttribute"></param>
        public virtual void RemoveAttribute(BasicAttribute rAttribute)
        {
            Items.Remove(rAttribute);
        }

        /// <summary>
        /// Removes the specified attribute from the list of items
        /// </summary>
        /// <param name="rItemID">String representing the name or ID of the attribute we're removing</param>
        /// <param name="rAttribute"></param>
        public virtual void RemoveAttribute(string rAttributeID)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Compare(Items[i].ID, rAttributeID, true) == 0)
                {
                    Items.Remove(Items[i]);
                    return;
                }
            }
        }

        /// <summary>
        /// Returns the BasicAttribute associated with the name or ID
        /// </summary>
        /// <param name="rItemID">String representing the name or ID of the attribute we're checking</param>
        /// <returns>BasicAttribute if one is found or NULL if not</returns>
        public virtual BasicAttribute GetAttribute(string rAttributeID)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Compare(Items[i].ID, rAttributeID, true) == 0)
                {
                    return Items[i];
                }
            }

            return null;
        }

        /// <summary>
        /// Gets the type of the attribute
        /// </summary>
        /// <param name="rAttributeID"></param>
        /// <returns></returns>
        public virtual Type GetAttributeType(string rAttributeID)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Compare(Items[i].ID, rAttributeID, true) == 0)
                {
                    return Items[i].ValueType;
                }
            }

            return null;
        }

        /// <summary>
        /// Returns the attribute value as the specified type
        /// </summary>
        /// <typeparam name="T">Type that we're expecting to get back</typeparam>
        /// <param name="rAttributeID">Attribute that we're looking for</param>
        /// <returns>Attribute value as the specified type</returns>
        public virtual T GetAttributeValue<T>(string rAttributeID)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Compare(Items[i].ID, rAttributeID, true) == 0)
                {
                    return Items[i].GetValue<T>();
                }
            }

            return default(T);
        }

        /// <summary>
        /// Returns the attribute value as the specified type
        /// </summary>
        /// <typeparam name="T">Type that we're expecting to get back</typeparam>
        /// <param name="rAttributeID">Attribute that we're looking for</param>
        /// <param name="rDefault">Default value if the attribute isn't found</param>
        /// <returns>Attribute value as the specified type</returns>
        public virtual T GetAttributeValue<T>(string rAttributeID, T rDefault)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Compare(Items[i].ID, rAttributeID, true) == 0)
                {
                    return Items[i].GetValue<T>();
                }
            }

            return rDefault;
        }

        /// <summary>
        /// Given the specified attribute, grab the float value
        /// </summary>
        /// <param name="rAttribute">string representing the attribute type we want</param>
        /// <param name="rDefault">Default value if the attribute isn't found</param>
        /// <returns>Float representing the value of the attribute or default if not found.</returns>
        public virtual float GetAttributeValue(string rAttributeID, float rDefault = 0f)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Compare(Items[i].ID, rAttributeID, true) == 0)
                {
                    return Items[i].GetValue<float>();
                }
            }

            return rDefault;
        }

        /// <summary>
        /// Given the specified attribute, set the value associated with the attribute
        /// </summary>
        /// <typeparam name="T">Type of attribute to set</typeparam>
        /// <param name="rAttributeID">String representing the name or ID of the item we want</param>
        /// <param name="rValue">value to set on the attribute</param>
        public virtual void SetAttributeValue<T>(string rAttributeID, T rValue)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Compare(Items[i].ID, rAttributeID, true) == 0)
                {
                    Items[i].SetValue<T>(rValue);
                }
            }
        }

        /// <summary>
        /// Given the specified attribute, set the value associated with the attribute
        /// </summary>
        /// <param name="rAttribute">String representing the name or ID of the item we want</param>
        /// <param name="rValue">value to set on the attribute</param>
        public virtual void SetAttributeValue(string rAttributeID, float rValue)
        {
            for (int i = Items.Count - 1; i >= 0; i--)
            {
                if (string.Compare(Items[i].ID, rAttributeID, true) == 0)
                {
                    Items[i].SetValue<float>(rValue);
                }
            }
        }

        // **************************************************************************************************
        // Following properties and function only valid while editing
        // **************************************************************************************************

#if UNITY_EDITOR

        /// <summary>
        /// Allows us to re-open the last selected motor
        /// </summary>
        public int EditorItemIndex = 0;

#endif
    }
}
