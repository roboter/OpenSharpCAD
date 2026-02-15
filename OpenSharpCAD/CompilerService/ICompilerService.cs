using System.Collections.Generic;

namespace CSharpCAD
{
    public interface ICompilerService
    {
        object Compile(string scriptSource, out List<string> errors);
    }
}
