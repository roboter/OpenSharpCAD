using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatterHackers.Csg;
using MatterHackers.Csg.Solids;
using MatterHackers.Agg.UI;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterCadGui.CsgEditors
{
    public class CsgEditorBox : FlowLayoutWidget
    {
        BoxPrimitive target;
        public CsgEditorBox(BoxPrimitive target)
        {
            this.target = target;

            AddChild(new TextWidget("Box"));
            AddVectorEdit(target, 0);
            AddVectorEdit(target, 1);
            AddVectorEdit(target, 2);
        }

        private void AddVectorEdit(BoxPrimitive target, int index)
        {
            NumberEdit editControl = new NumberEdit(target.Size[index], pixelWidth: 35, allowDecimals: true);
            editControl.TabIndex = index;
            editControl.InternalNumberEdit.TextChanged += (sender, e) =>
            {
                Vector3 newSize = target.Size;
                newSize[index] = editControl.Value;
                target.Size = newSize;
            };
            AddChild(editControl);
        }
    }
}
