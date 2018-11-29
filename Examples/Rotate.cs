var rootUnion = new MatterHackers.Csg.Operations.Union("root");


for (int i = 0; i != 10; i++)

{
   rootUnion.Add(new Rotate(new BoxPrimitive(18, i, 14), 10 * i, 10, 10));
}


return rootUnion;

