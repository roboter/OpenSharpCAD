using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MatterHackers.Csg;
using MatterHackers.Csg.Solids;
using MatterHackers.Csg.Transform;
using MatterHackers.Agg.UI;
using MatterHackers.VectorMath;

namespace MatterHackers.MatterCadGui.CsgEditors
{
    public class CsgEditorTranslate : FlowLayoutWidget
    {
        Translate target;
        public CsgEditorTranslate(Translate target)
            : base(FlowDirection.TopToBottom)
        {
            this.target = target;

            FlowLayoutWidget row = new FlowLayoutWidget();
            row.AddChild(new TextWidget("Translate"));
            AddVectorEdit(row, target, 0);
            AddVectorEdit(row, target, 1);
            AddVectorEdit(row, target, 2);
            AddChild(row);

            row = new FlowLayoutWidget();
            row.AddChild(new GuiWidget(10, 3));
            row.AddChild(CsgEditorBase.CreateEditorForCsg(target.ObjectToTransform));
            AddChild(row);
        }

        private void AddVectorEdit(GuiWidget parent, Translate target, int index)
        {
            NumberEdit editControl = new NumberEdit(target.Size[index], pixelWidth: 35, allowNegatives: true, allowDecimals: true);
            editControl.TabIndex = index;
            editControl.InternalNumberEdit.TextChanged += (sender, e) =>
            {
                Vector3 newSize = target.Translation;
                newSize[index] = editControl.Value;
                target.Translation = newSize;
            };
            parent.AddChild(editControl);
        }
    }
}
