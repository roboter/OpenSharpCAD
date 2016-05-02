using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatterHackers.Csg;
using MatterHackers.Csg.Operations;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Processors;
using MatterHackers.Csg.Transform;
using MatterHackers.Agg.UI;
using System.Diagnostics;

namespace MatterHackers.MatterCadGui.CsgEditors
{
    public static class CsgEditorBase
    {
        public static GuiWidget CreateEditorForCsg(CsgObject csgObject)
        {
            if (csgObject.GetType() == typeof(BoxPrimitive))
            {
                return new CsgEditorBox((BoxPrimitive)csgObject);
            }
            else if (csgObject.GetType() == typeof(Union))
            {
                return new CsgEditorUnion((Union)csgObject);
            }
            else if (csgObject.GetType() == typeof(Translate))
            {
                return new CsgEditorTranslate((Translate)csgObject);
            }
            else
            {
                Debug.WriteLine(csgObject.GetType());
                return new CsgEditorBox((BoxPrimitive)csgObject);
                //   throw new NotImplementedException();
            }
        }
    }
}
