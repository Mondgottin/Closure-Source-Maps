using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ClosureSourceMaps
{
    interface IBuilder
    {
        OriginalMapping build();
        IBuilder setOriginalFile(string fileName);
        IBuilder setLineNumber(int lineNumber);
        IBuilder setColumnPosition(int column);
        IBuilder setIdentifier(string identifier);
    }
}
