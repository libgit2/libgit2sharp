using System;
using System.Diagnostics;
using CodeGeneration.Roslyn;

namespace LibGit2Sharp
{
    /// <summary>
    /// Causes generation of an overload of a P/Invoke method that has a more friendly signature.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    [CodeGenerationAttribute("CodeGeneration.OfferFriendlyInteropOverloadsGenerator, CodeGeneration, Version=" + ThisAssembly.AssemblyVersion + ", Culture=neutral, PublicKeyToken=" + ThisAssembly.PublicKeyToken)]
    [Conditional("CodeGeneration")]
    public class OfferFriendlyInteropOverloadsAttribute : Attribute
    {
    }
}
