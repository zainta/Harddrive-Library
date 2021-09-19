// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace HDDL.IO.Settings
{
    /// <summary>
    /// Represents a subsection in an ini file
    /// </summary>
    public class IniSubsection : IniItemBase
    {
        /// <summary>
        /// The regular expression used to match sections and extract their names
        /// </summary>
        internal const string Section_Match_Regex = @"^(\[.+\])";

        private List<IniItemBase> _contents;
        /// <summary>
        /// The Subsection's contents
        /// </summary>
        public IReadOnlyCollection<IniItemBase> Contents
        {
            get
            {
                return _contents.AsReadOnly();
            }
        }

        /// <summary>
        /// Creates a value item with the given parent section
        /// </summary>
        /// <param name="parent">The parent subsection</param>
        /// <param name="label">The item's name</param>
        public IniSubsection(string label, IniSubsection parent = null, params IniItemBase[] contents) : base(label, parent)
        {
            _contents = new List<IniItemBase>();
            foreach (var item in contents)
            {
                item.SetParent(this);
            }
        }

        /// <summary>
        /// Adds the item to the sub section.
        /// Prevents duplicate item names
        /// </summary>
        /// <param name="item">The item to add</param>
        /// <returns>True if successful, False upon failure</returns>
        public bool Add(IniItemBase item)
        {
            var anyDupes = (from c in _contents where c.Label == item.Label select c).Any();
            if (!anyDupes)
            {
                if (item.SubSection != null)
                {
                    item.SubSection.Remove(item);
                }

                item.SubSection = this;
                _contents.Add(item);
                return true;
            }

            return false;
        }

        public void Remove(IniItemBase item)
        {
            var found = (from c in _contents where c.Label == item.Label select c).Any();
            if (found)
            {
                item.SubSection = null;
                _contents.Remove(item);
            }
        }

        /// <summary>
        /// Verifies path and then attempts to interpret the given line of text
        /// </summary>
        /// <param name="path">The current path</param>
        /// <param name="line">The line of text to interpret</param>
        /// <returns>True if successful, false otherwise</returns>
        public override bool Read(string path, string line)
        {
            if (path != GetPath(false)) return false;

            var match = Regex.Match(line, Section_Match_Regex);
            if (match.Groups.Count == 2 && match.Groups[1].Value.Substring(1, match.Groups[1].Value.Length - 2) == Label)
            {
                // nothing to really do here other than report that we found it.
                return true;
            }

            return false;
        }

        /// <summary>
        /// Converts the ini subsection to a line of text to be written to disk
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return $"[{Label}]";
        }

        /// <summary>
        /// Returns a deep clone of the IniSubsection (omitting parenting structure)
        /// </summary>
        /// <returns>The cloned instance</returns>
        public override object Clone()
        {
            return new IniSubsection(Label, null, (from c in Contents select c.Clone() as IniItemBase).ToArray());
        }
    }
}
