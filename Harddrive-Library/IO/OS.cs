// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using System.Runtime.InteropServices;

namespace HDDL.IO
{
    /// <summary>
    /// Static class to allow inspection and exploration of the operating system
    /// </summary>
    public static class OS
    {
        /// <summary>
        /// Returns a value indicating if this is running on Windows
        /// </summary>
        /// <returns></returns>
        public static bool IsWindows
        {
            get
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            }
        }

        /// <summary>
        /// Returns a value indicating if this is running on Linux
        /// </summary>
        /// <returns></returns>
        public static bool IsLinux
        {
            get
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.Linux);
            }
        }

        /// <summary>
        /// Returns a value indicating if this is running on Mac OSX
        /// </summary>
        /// <returns></returns>
        public static bool IsOSX
        {
            get
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
            }
        }

        /// <summary>
        /// Returns a value indicating if this is running on FreeBSD
        /// </summary>
        /// <returns></returns>
        public static bool IsFreeBSD
        {
            get
            {
                return RuntimeInformation.IsOSPlatform(OSPlatform.FreeBSD);
            }
        }
    }
}
