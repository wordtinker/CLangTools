using System;
using System.Reflection;

namespace LangTools.ViewModels
{
    /// <summary>
    /// A sample assembly reference class that would exist in the `Core` project. 
    /// </summary>
    public static class CoreAssembly
    {
        public static readonly Assembly Reference = typeof(CoreAssembly).Assembly;
        public static readonly Version Version = Reference.GetName().Version;
    }
}
