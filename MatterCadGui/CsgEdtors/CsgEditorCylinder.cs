using System;
using MatterHackers.Csg.Solids;
using MatterHackers.Agg.UI;

namespace MatterHackers.MatterCadGui.CsgEditors
{
    public class CsgEditorCylinder : FlowLayoutWidget
    {
        Cylinder.CylinderPrimitive target;
        public CsgEditorCylinder(Cylinder.CylinderPrimitive target)
        {
            this.target = target;

            AddChild(new TextWidget("Cylinder R1,R2,H,S"));

            AddNumberEdit(target.Radius1, (val) => target.Radius1 = val, 0);
            AddNumberEdit(target.Radius2, (val) => target.Radius2 = val, 1);
            AddNumberEdit(target.Height, (val) => target.Height = val, 2);
            AddNumberEdit(target.Sides, (val) => target.Sides = (int)val, 3);
        }

        private void AddNumberEdit(double initialValue, Action<double> setter, int index)
        {
            NumberEdit editControl = new NumberEdit(initialValue, pixelWidth: 35, allowDecimals: true)
            {
                TabIndex = index
            };
            editControl.InternalNumberEdit.TextChanged += (sender, e) =>
            {
                setter(editControl.Value);
            };
            AddChild(editControl);
        }
    }
}
