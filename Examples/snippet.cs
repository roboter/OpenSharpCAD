var rootUnion = new MatterHackers.Csg.Operations.Union("root");



rootUnion.Add(new Translate(new Cylinder(10, 40), 5, 10, 5));



rootUnion.Add(new BoxPrimitive(8, 20, 10));



for(int i=0;i!=10;i++)

rootUnion.Add(new Translate(new BoxPrimitive(18, i, 14),10*i,10,10));

return rootUnion;