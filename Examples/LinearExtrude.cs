var rootUnion = new MatterHackers.Csg.Operations.Union("root");
rootUnion.Add(new LinearExtrude(new double[] {0, 10, 10, 10, 0, 0}, 10,new Alignment(), 0 ));
return rootUnion;
