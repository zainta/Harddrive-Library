// Copyright (c) Zain Al-Ahmary.  All rights reserved.
// Licensed under the MIT License, (the "License"); you may not use this file except in compliance with the License. 
// You may obtain a copy of the License at https://mit-license.org/

using HDDL.Data;
using HDDL.IO.Settings;
using Microsoft.AspNetCore.Mvc;

namespace HDDL.Web
{
    /// <summary>
    /// Base class for ini / datahandler based controllers
    /// </summary>
    public class HDSLControllerBase : Controller
    {
        protected IDataHandler _dh;
        protected IInitializationFileManager _ini;
        protected bool _useTypeAnnotation;


        public HDSLControllerBase(IDataHandler dh, IInitializationFileManager ini)
        {
            _dh = dh;
            _ini = ini;

            if (_ini["HDSL_Web>UseTypeAnnotationProperty"] != null)
            {
                if (!bool.TryParse(_ini["HDSL_Web>UseTypeAnnotationProperty"].Value, out _useTypeAnnotation))
                {
                    _useTypeAnnotation = false;
                }
            }
        }
    }
}
