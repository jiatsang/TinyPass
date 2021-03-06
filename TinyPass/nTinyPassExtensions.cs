﻿// ------------------------------------------------------------------------
// Chiats Common&Data Library V3.5 Beta#1 (2017/03)
// Chiats@Studio(http://www.chiats.com/Common)
// Design&Coding By Chia Tsang Tsai
// Copyright(C) 2005-2017 Chiats@Studio All rights reserved.
// ------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Reflection;

namespace Chiats.nTinyPass
{

    public static class nTinyPassExtensions
    {
        public static bool QueryFill(this IDataReader reader, object obj, nTinyPassMode TinyPassMode = nTinyPassMode.CheckAndException )
        {
            if (obj != null)
            {
                Type TinyPassGeneric = typeof(nTinyPass<>);
                Type TinyPassConstructed = TinyPassGeneric.MakeGenericType(obj.GetType());
                MethodInfo GetMethodInfo = TinyPassConstructed.GetMethod("QueryFill");
                try
                {
                    GetMethodInfo.Invoke(null, new object[] { obj, reader, TinyPassMode });
                }
                catch (TargetInvocationException ex)
                {
                    throw ex.InnerException;
                }
                return true;
            }
            return false;
        }
    }
}
