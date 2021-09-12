using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace HDDL.IO.Settings
{
    /// <summary>
    /// Base class for ini file content classes
    /// </summary>
    public abstract class IniItemBase : ICloneable
    {
        /// <summary>
        /// The subsection's name
        /// </summary>
        public string Label { get; private set; }

        /// <summary>
        /// The item's root
        /// </summary>
        public string  RootKey { get; internal set; }

        /// <summary>
        /// The containing section
        /// </summary>
        public IniSubsection SubSection { get; internal set; }

        /// <summary>
        /// Creates an item with the given parent section
        /// </summary>
        /// <param name="parent">The parent subsection</param>
        /// <param name="label">The item's name</param>
        public IniItemBase(string label, IniSubsection parent = null)
        {
            RootKey = null;
            SubSection = parent;
            Label = label;
        }

        /// <summary>
        /// Assigns the IniItemBase to be part of the given parent's contents
        /// </summary>
        /// <param name="parent">The new parent</param>
        public void SetParent(IniSubsection parent)
        {
            if (parent != null)
            {
                if (!parent.Contents.Contains(this))
                {
                    parent.Add(this);
                }
            }
            else
            {
                if (SubSection != null  &&
                    SubSection.Contents.Contains(this))
                {
                    parent.Remove(this);
                }
            }
        }

        /// <summary>
        /// Returns the item's complete path to the root
        /// </summary>
        /// <param name="includeSelf">If true, incluse the calling instance.  Otherwise, omits it</param>
        /// <returns></returns>
        public virtual string GetPath(bool includeSelf = true)
        {
            var sb = new StringBuilder();

            Action<IniItemBase, StringBuilder> recursor = null;
            recursor = (item, sb) =>
            {
                if (item.SubSection != null)
                {
                    recursor(item.SubSection, sb);
                }

                if (sb.Length > 0)
                {
                    sb.Append(IniFileManager.SubSection_Content_Designation);
                }
                sb.Append(item.GetDecoratedString());
            };

            if (includeSelf)
            {
                recursor(this, sb);
            }
            else
            {
                if (SubSection != null)
                {
                    recursor(SubSection, sb);
                }
            }
            return sb.ToString();
        }

        /// <summary>
        /// Verifies path and then attempts to interpret the given line of text
        /// </summary>
        /// <param name="path">The current path</param>
        /// <param name="line">The line of text to interpret</param>
        /// <returns>True if successful, false otherwise</returns>
        public virtual bool Read(string path, string line)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Converts the line into the appropriate ini node
        /// </summary>
        /// <param name="path">The current path</param>
        /// <param name="line">The line of text to interpret</param>
        /// <returns>The resulting node, or null</returns>
        public static IniItemBase Discover(string line)
        {
            IniItemBase result = null;
            Match match = null;
            if (!string.IsNullOrWhiteSpace(line))
            {
                // Check for a subsection first
                match = Regex.Match(line, IniSubsection.Section_Match_Regex);
                if (match.Groups.Count == 2 &&
                    !string.IsNullOrWhiteSpace(match.Groups[1].Value))
                {
                    result = new IniSubsection(match.Groups[1].Value.Substring(1, match.Groups[1].Value.Length - 2), null);
                }

                // if it's not a subsection then check for a value
                if (result == null)
                {
                    match = Regex.Match(line, IniValue.Value_Match_Regex);
                    if (match.Groups.Count == 3 &&
                        !string.IsNullOrWhiteSpace(match.Groups[1].Value) &&
                        !string.IsNullOrWhiteSpace(match.Groups[2].Value))
                    {
                        result = new IniValue(match.Groups[1].Value, match.Groups[2].Value);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// In derivations, performs a deep clone of the IniItemBase
        /// </summary>
        /// <returns></returns>
        public virtual object Clone()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns the IniItemBase as a qualified string
        /// </summary>
        /// <returns></returns>
        public virtual string GetDecoratedString()
        {
            throw new NotImplementedException();
        }
    }
}
