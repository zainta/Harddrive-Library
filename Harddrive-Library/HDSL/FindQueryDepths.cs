using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HDDL.HDSL
{
    /// <summary>
    /// The types of find starting points
    /// </summary>
    enum FindQueryDepths
    {
        /// <summary>
        /// Immediately inside of the target path
        /// </summary>
        In,
        /// <summary>
        /// Immediately inside of the target path and under all subdirectories, to an infinite depth
        /// </summary>
        Within,
        /// <summary>
        /// Immediately inside of all subdirectories, to an infinite depth
        /// </summary>
        Under
    }
}
