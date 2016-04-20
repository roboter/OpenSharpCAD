using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatterHackers.Csg;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Operations;
using MatterHackers.Agg.UI;

namespace MatterHackers.MatterCadGui.CsgEditors
{
    public class CsgEditorUnion : FlowLayoutWidget
    {
        Union target;
        public CsgEditorUnion(Union target)
            : base(FlowDirection.TopToBottom)
        {
            this.target = target;

            AddChild(new TextWidget("Union"));
            foreach (CsgObject part in target.AllObjects)
            {
                FlowLayoutWidget row = new FlowLayoutWidget();
                row.AddChild(new GuiWidget(10, 2));
                row.AddChild(CsgEditorBase.CreateEditorForCsg(part));
                AddChild(row);
            }
        }
    }
}
