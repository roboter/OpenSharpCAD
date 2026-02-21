using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace CSharpCAD
{
    public interface ICompilerService
    {
        object Compile(string scriptSource, out List<Diagnostic> errors);
    }
}
