using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace HDDL.UI.WPF
{
    /// <summary>
    /// Adds syntax highlighting functionality to the RTB control
    /// </summary>
    public class KeywordHighlighterTextBox : RichTextBox
    {
        List<Task> _recolorJobs;

        public KeywordHighlighterTextBox() : base()
        {
            _recolorJobs = new List<Task>();

            BindTextChange();
        }

        #region Utility

        /// <summary>
        /// Queues a recolor job for the given changes
        /// </summary>
        /// <param name="e">The changes that triggered the recolor</param>
        private void QueueJob(TextChangedEventArgs e)
        {
            // fire off a tokenization job

        }

        /// <summary>
        /// Binds the text changed event
        /// </summary>
        private void BindTextChange()
        {
            TextChanged += KeywordHighlighterTextBox_TextChanged;
        }

        /// <summary>
        /// Unbinds the text changed event
        /// </summary>
        private void UnbindTextChanged()
        {
            TextChanged -= KeywordHighlighterTextBox_TextChanged;
        }

        /// <summary>
        /// Takes a start and an end point and works outward until it finds whitespace
        /// </summary>
        /// <param name="beginning">The starting beginning of the range</param>
        /// <param name="ending">The starting ending of the range</param>
        /// <returns>The resulting text range</returns>
        private TextRange GetBoundaryRange(TextPointer beginning, TextPointer ending)
        {
            bool startDone = false;
            bool endDone = false;
            TextPointer start = null;
            TextPointer end = null;

            // we want a minimum length of characters to start out from
            if ((start = beginning.GetNextInsertionPosition(LogicalDirection.Backward)) == null)
            {
                start = beginning;
                startDone = true;
            }
            if ((end = ending.GetNextInsertionPosition(LogicalDirection.Forward)) == null)
            {
                end = ending;
                endDone = true;
            }

            // to speed this already dreadfully slow process up,
            // we loop through start and end at the same time
            TextPointer next = null;
            while (!startDone || !endDone)
            {
                if (!startDone)
                {
                    if (Regex.IsMatch(GetPointString(start, LogicalDirection.Backward), @"\W"))
                    {
                        startDone = true;
                    }
                    else
                    {
                        if ((next = start.GetNextInsertionPosition(LogicalDirection.Backward)) == null)
                        {
                            startDone = true;
                        }
                        else
                        {
                            start = next;
                        }
                    }
                }

                if (!endDone)
                {
                    if (Regex.IsMatch(GetPointString(end, LogicalDirection.Forward), @"\W"))
                    {
                        endDone = true;
                    }
                    else
                    {
                        if ((next = end.GetNextInsertionPosition(LogicalDirection.Forward)) == null)
                        {
                            endDone = true;
                        }
                        else
                        {
                            end = next;
                        }
                    }
                }
            }

            return new TextRange(start, end);
        }

        /// <summary>
        /// Takes a TextPointer and returns a single character string from its location in the direction provided
        /// </summary>
        /// <param name="location"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        private string GetPointString(TextPointer location, LogicalDirection direction)
        {
            TextPointer target;
            if ((target = location.GetNextInsertionPosition(direction)) != null)
            {
                TextRange result = null;
                switch (direction)
                {
                    case LogicalDirection.Forward:
                        result = new TextRange(location, target);
                        break;
                    case LogicalDirection.Backward:
                        result = new TextRange(target, location);
                        break;
                }

                return result.Text;
            }

            return null;
        }

        /// <summary>
        /// Takes a starting point and moves until it is as close to the given offset from it as possible
        /// </summary>
        /// <param name="start">The starting point</param>
        /// <param name="offset">The offset value</param>
        /// <returns>The new location</returns>
        private TextPointer GetPointer(TextPointer start, int offset)
        {
            var result = start;
            var distance = start.GetOffsetToPosition(Document.ContentStart);
            var currentOffset = Math.Abs(start.GetOffsetToPosition(result));
            if (distance < offset)
            {
                while (currentOffset < offset)
                {
                    var next = result.GetNextInsertionPosition(LogicalDirection.Forward);
                    if (next == null)
                    {
                        break;
                    }
                    else
                    {
                        currentOffset = Math.Abs(start.GetOffsetToPosition(next));
                    }

                    if (currentOffset >= offset)
                    {
                        break;
                    }
                    else
                    {
                        result = next;
                    }
                }
            }
            else if (distance > offset)
            {
                while (currentOffset < offset)
                {
                    var next = result.GetNextInsertionPosition(LogicalDirection.Backward);
                    if (next == null)
                    {
                        break;
                    }
                    else
                    {
                        currentOffset = Math.Abs(start.GetOffsetToPosition(next));
                    }

                    if (currentOffset >= offset)
                    {
                        break;
                    }
                    else
                    {
                        result = next;
                    }
                }
            }

            return result;
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Used for highlighting
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void KeywordHighlighterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            //UnbindTextChanged();
            //Document.LineHeight = 1;

            //foreach (var change in e.Changes)
            //{
            //    var start = GetPointer(Document.ContentStart, change.Offset);

            //    TextPointer end = null;
            //    if (change.AddedLength > 0)
            //    {
            //        end = GetPointer(Document.ContentStart, change.Offset + change.AddedLength);
            //    }
            //    else
            //    {
            //        end = GetPointer(Document.ContentStart, change.Offset + change.RemovedLength);
            //    }

            //    var modded = GetBoundaryRange(start, end);
            //    var moddedStr = modded.Text;

            //    ////var startP = GetPointer(start, 1, LogicalDirection.Backward);
            //    ////var endP = GetPointer(end, 1, LogicalDirection.Forward);

            //    //var iStart = start.GetOffsetToPosition(Document.ContentStart);
            //    //var iEnd = end.GetOffsetToPosition(Document.ContentStart);
            //    //var iLength = iEnd - iStart;

            //    //// get the bordering range
            //    //var t = new TextRange(start, end);
            //    ////var t2 = new TextRange(startP, endP);

            //    //if (change.AddedLength > 0)
            //    //{
            //    //    t.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Red));
            //    //}
            //    //else
            //    //{
            //    //    start.InsertTextInRun("New Text");
            //    //    t.ApplyPropertyValue(TextElement.ForegroundProperty, new SolidColorBrush(Colors.Blue));
            //    //}
            //}

            //BindTextChange();
        }

        #endregion
    }
}
